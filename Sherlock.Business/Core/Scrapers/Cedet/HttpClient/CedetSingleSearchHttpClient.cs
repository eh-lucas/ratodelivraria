using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;
using Polly.Retry;
using Sherlock.Business.Interfaces;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Enums;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Sherlock.Business.Core.Scrapers.Cedet.HttpClient;

public class CedetSingleSearchHttpClient : IScraper
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);
    private readonly ILogger<CedetSingleSearchHttpClient> _logger;

    // HttpClient estático para reutilização de conexões
    private static readonly System.Net.Http.HttpClient _httpClient;
    private static readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    static CedetSingleSearchHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
            MaxConnectionsPerServer = 100,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            ConnectTimeout = TimeSpan.FromSeconds(10),
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5
        };

        _httpClient = new System.Net.Http.HttpClient(handler)
        {
            Timeout = RequestTimeout
        };

        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode && (int)r.StatusCode >= 500)
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: 2,
                sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) => { });
    }

    private static HttpRequestMessage CreateRequest(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        request.Headers.Add("Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
        request.Headers.Add("Accept-Language", "pt-BR,pt;q=0.9,en-US;q=0.8,en;q=0.7");
        request.Headers.Add("Accept-Encoding", "gzip, deflate");
        return request;
    }

    public CedetSingleSearchHttpClient() : this(NullLogger<CedetSingleSearchHttpClient>.Instance)
    {
    }

    public CedetSingleSearchHttpClient(ILogger<CedetSingleSearchHttpClient> logger)
    {
        _logger = logger;
    }

    public ScraperTypeEnum ScraperType => ScraperTypeEnum.CedetSingleAgilityHttpClient;

    public async Task<QueryResult> ExecuteSearch(SearchParameter parameters)
    {
        var provider = parameters.Source ?? new Provider { Id = 0, Name = "Unknown", Url = string.Empty };
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // ISBN é obrigatório
            if (string.IsNullOrEmpty(parameters.Isbn))
            {
                _logger.LogDebug("[{Provider}] ISBN vazio, ignorando busca", provider.Name);
                return QueryResult.CreateNoResult(provider, stopwatch.ElapsedMilliseconds);
            }

            var searchTerm = Uri.EscapeDataString(parameters.Isbn);
            var baseUrl = NormalizeUrl(provider.Url);
            var searchPath = provider.SearchUrlTemplate.Replace("{search}", searchTerm);
            var searchUrl = $"{baseUrl.TrimEnd('/')}/{searchPath.TrimStart('/')}";

            _logger.LogDebug("[{Provider}] Iniciando busca: {Url}", provider.Name, searchUrl);

            // ******************************
            // ETAPA 1: Busca pelo ISBN na página de busca
            // ******************************
            var searchResponse = await SendAsync(searchUrl, provider, stopwatch);

            if (!searchResponse.IsSuccessStatusCode)
            {
                stopwatch.Stop();
                var locationHeader = searchResponse.Headers.Location?.ToString() ?? "N/A";
                _logger.LogWarning("[{Provider}] HTTP {StatusCode} em {ElapsedMs}ms - Location: {Location}",
                    provider.Name, (int)searchResponse.StatusCode, stopwatch.ElapsedMilliseconds, locationHeader);

                return QueryResult.CreateFailure(
                    provider,
                    QueryErrorType.HttpError,
                    $"HTTP {(int)searchResponse.StatusCode}",
                    stopwatch.ElapsedMilliseconds,
                    (int)searchResponse.StatusCode);
            }

            var html = await searchResponse.Content.ReadAsStringAsync();
            _logger.LogDebug("[{Provider}] Resposta da busca recebida ({Size} bytes)", provider.Name, html.Length);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var products = ExtractProducts(doc);
            if (products == null || products.Count == 0)
            {
                stopwatch.Stop();
                _logger.LogDebug("[{Provider}] Nenhum produto encontrado na busca", provider.Name);
                return QueryResult.CreateNoResult(provider, stopwatch.ElapsedMilliseconds);
            }

            var possibleBooks = ParseProducts(products, provider);
            if (!possibleBooks.Any())
            {
                stopwatch.Stop();
                _logger.LogDebug("[{Provider}] Nenhum produto parseado com sucesso", provider.Name);
                return QueryResult.CreateNoResult(provider, stopwatch.ElapsedMilliseconds);
            }

            _logger.LogDebug("[{Provider}] {Count} produtos encontrados, validando ISBN...", provider.Name, possibleBooks.Count);

            // ******************************
            // ETAPA 2: Acessa página de cada produto para validar ISBN
            // ******************************
            foreach (var possibleBook in possibleBooks)
            {
                try
                {
                    // Monta URL absoluta se necessário
                    var productUrl = possibleBook.ProductUrl;
                    if (string.IsNullOrEmpty(productUrl))
                        continue;

                    if (!productUrl.StartsWith("http"))
                    {
                        productUrl = $"{baseUrl.TrimEnd('/')}/{productUrl.TrimStart('/')}";
                    }

                    _logger.LogDebug("[{Provider}] Acessando página do produto: {Url}", provider.Name, productUrl);

                    var productResponse = await SendAsync(productUrl, provider, stopwatch);
                    if (!productResponse.IsSuccessStatusCode)
                    {
                        _logger.LogDebug("[{Provider}] Falha ao acessar produto (HTTP {StatusCode})",
                            provider.Name, (int)productResponse.StatusCode);
                        continue;
                    }

                    var productHtml = await productResponse.Content.ReadAsStringAsync();
                    var productDoc = new HtmlDocument();
                    productDoc.LoadHtml(productHtml);

                    // Extrai ISBN da página do produto
                    var extractedIsbn = ExtractProductIsbn(productDoc);

                    if (!string.IsNullOrEmpty(extractedIsbn))
                    {
                        // Compara ISBN extraído com o buscado
                        if (IsbnMatches(extractedIsbn, parameters.Isbn))
                        {
                            stopwatch.Stop();
                            _logger.LogInformation("[{Provider}] ISBN validado! \"{Title}\" - R${Price:F2} em {ElapsedMs}ms",
                                provider.Name, possibleBook.Title, possibleBook.Price, stopwatch.ElapsedMilliseconds);

                            return QueryResult.CreateSuccess(
                                provider,
                                possibleBook.Title,
                                possibleBook.Author,
                                possibleBook.Price,
                                possibleBook.Discount,
                                stopwatch.ElapsedMilliseconds,
                                productUrl);
                        }
                        else
                        {
                            _logger.LogDebug("[{Provider}] ISBN não corresponde. Esperado: {Expected}, Encontrado: {Found}",
                                provider.Name, parameters.Isbn, extractedIsbn);
                        }
                    }
                    else
                    {
                        // Verifica se é um kit (não terá ISBN único)
                        if (IsKitProduct(productDoc))
                        {
                            _logger.LogDebug("[{Provider}] Produto é um kit, ignorando: {Title}", provider.Name, possibleBook.Title);
                        }
                        else
                        {
                            _logger.LogWarning("[{Provider}] ISBN não encontrado na página do produto: {Url}",
                                provider.Name, productUrl);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("[{Provider}] Erro ao validar produto: {Message}", provider.Name, ex.Message);
                    continue;
                }
            }

            // Nenhum produto com ISBN válido encontrado
            stopwatch.Stop();
            _logger.LogDebug("[{Provider}] Nenhum produto com ISBN correspondente em {ElapsedMs}ms",
                provider.Name, stopwatch.ElapsedMilliseconds);
            return QueryResult.CreateNoResult(provider, stopwatch.ElapsedMilliseconds);
        }
        catch (TaskCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("[{Provider}] Timeout após {ElapsedMs}ms", provider.Name, stopwatch.ElapsedMilliseconds);
            return QueryResult.CreateFailure(provider, QueryErrorType.Timeout, "Request timeout", stopwatch.ElapsedMilliseconds);
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning("[{Provider}] Erro de rede: {Message}", provider.Name, ex.Message);
            return QueryResult.CreateFailure(provider, QueryErrorType.Network, ex.Message, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[{Provider}] Erro inesperado", provider.Name);
            return QueryResult.CreateFailure(provider, QueryErrorType.Unknown, ex.Message, stopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Extrai ISBN da página do produto usando Regex
    /// </summary>
    private string? ExtractProductIsbn(HtmlDocument doc)
    {
        var bodyText = doc.DocumentNode.InnerText;

        // Regex para ISBN (10 ou 13 dígitos, com ou sem hífen/espaço)
        // Padrões: "ISBN: 9788584911516", "ISBN9788584911516", "ISBN 978-85-849-1151-6"
        var isbnPattern = @"ISBN[:\s]*(\d{3}[-\s]?\d{1,5}[-\s]?\d{1,7}[-\s]?\d{1,6}[-\s]?\d{1}|\d{13}|\d{10})";
        var match = Regex.Match(bodyText, isbnPattern, RegexOptions.IgnoreCase);

        if (match.Success)
        {
            var isbn = match.Groups[1].Value;
            return NormalizeIsbn(isbn);
        }

        return null;
    }

    /// <summary>
    /// Verifica se o produto é um kit/combo (não terá ISBN único)
    /// </summary>
    private bool IsKitProduct(HtmlDocument doc)
    {
        var title = doc.DocumentNode.SelectSingleNode("//h1")?.InnerText?.ToLowerInvariant() ?? "";
        var bodyText = doc.DocumentNode.InnerText.ToLowerInvariant();

        return title.Contains("kit") ||
               title.Contains("combo") ||
               title.Contains("coleção") ||
               bodyText.Contains("kit de livros") ||
               bodyText.Contains("combo de livros");
    }

    /// <summary>
    /// Normaliza ISBN removendo hífens e espaços
    /// </summary>
    private static string NormalizeIsbn(string isbn)
    {
        return Regex.Replace(isbn, @"[\s\-]", "");
    }

    /// <summary>
    /// Compara dois ISBNs (normalizados)
    /// </summary>
    private static bool IsbnMatches(string? extracted, string searched)
    {
        if (string.IsNullOrEmpty(extracted))
            return false;

        var normalizedExtracted = NormalizeIsbn(extracted);
        var normalizedSearched = NormalizeIsbn(searched);

        return normalizedExtracted == normalizedSearched;
    }

    /// <summary>
    /// Envia requisição HTTP com retry policy
    /// </summary>
    private async Task<HttpResponseMessage> SendAsync(string url, Provider provider, Stopwatch stopwatch)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            using var request = CreateRequest(url);
            _logger.LogDebug("[{Provider}] Enviando requisição para {Url}", provider.Name, url);
            return await _httpClient.SendAsync(request);
        });
    }

    /// <summary>
    /// Normaliza a URL removendo www. para evitar redirects
    /// </summary>
    private static string NormalizeUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return url;

        var uri = new Uri(url);
        if (uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            var newHost = uri.Host.Substring(4);
            var builder = new UriBuilder(uri) { Host = newHost };
            return builder.Uri.ToString();
        }

        return url;
    }

    /// <summary>
    /// Extrai produtos da página de busca
    /// </summary>
    private HtmlNodeCollection? ExtractProducts(HtmlDocument doc)
    {
        string[] selectors = new[]
        {
            "//div[contains(@class, 'item-product')]",
            "//div[contains(@class, 'product-item')]",
            "//li[contains(@class, 'product')]//div[contains(@class, 'item-product')]",
            "//ul[contains(@class, 'products')]//li[contains(@class, 'product')]",
            "//div[@class='product']",
            "//article[contains(@class, 'product')]"
        };

        foreach (var selector in selectors)
        {
            var products = doc.DocumentNode.SelectNodes(selector);
            if (products != null && products.Count > 0)
            {
                _logger.LogDebug("Produtos encontrados com seletor: {Selector} ({Count})", selector, products.Count);
                return products;
            }
        }

        return null;
    }

    /// <summary>
    /// Parseia lista de produtos
    /// </summary>
    private List<ParsedProduct> ParseProducts(HtmlNodeCollection products, Provider provider)
    {
        var possibleBooks = new List<ParsedProduct>();

        foreach (var product in products)
        {
            try
            {
                var book = ParseSingleProduct(product, provider);
                if (book != null && !string.IsNullOrEmpty(book.Title) && book.Price > 0)
                {
                    possibleBooks.Add(book);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("[{Provider}] Erro ao parsear produto: {Message}", provider.Name, ex.Message);
            }
        }

        return possibleBooks;
    }

    /// <summary>
    /// Parseia um único produto da listagem
    /// </summary>
    private ParsedProduct? ParseSingleProduct(HtmlNode product, Provider provider)
    {
        var title = ExtractText(product, new[]
        {
            ".//a[contains(@class, 'product-name')]",
            ".//div[contains(@class, 'name')]//a",
            ".//div[contains(@class, 'name')]",
            ".//h2[contains(@class, 'woocommerce-loop-product__title')]",
            ".//h2//a",
            ".//h3//a"
        });

        if (string.IsNullOrEmpty(title))
            return null;

        var author = ExtractText(product, new[]
        {
            ".//p[contains(@class, 'author')]//a",
            ".//p[contains(@class, 'author')]",
            ".//span[contains(@class, 'author')]"
        }) ?? "";

        var newPrice = ExtractPrice(product, new[]
        {
            ".//span[contains(@class, 'price-new')]",
            ".//span[contains(@class, 'sale-price')]",
            ".//ins//span[contains(@class, 'woocommerce-Price-amount')]//bdi"
        });

        if (newPrice <= 0)
        {
            newPrice = ExtractPrice(product, new[]
            {
                ".//span[contains(@class, 'woocommerce-Price-amount')]//bdi",
                ".//span[contains(@class, 'price')]//bdi",
                ".//bdi"
            });
        }

        if (newPrice <= 0)
            return null;

        var oldPrice = ExtractPrice(product, new[]
        {
            ".//span[contains(@class, 'price-old')]",
            ".//del//span[contains(@class, 'woocommerce-Price-amount')]//bdi"
        });

        int discount = 0;
        if (oldPrice > 0 && oldPrice > newPrice)
        {
            discount = (int)Math.Round(100 * (1 - (newPrice / oldPrice)));
        }

        var productUrl = ExtractHref(product, new[]
        {
            ".//a[contains(@class, 'product-name')]",
            ".//a[contains(@class, 'link-card')]",
            ".//div[contains(@class, 'name')]//a",
            ".//h2//a",
            ".//a"
        });

        return new ParsedProduct
        {
            Title = CleanText(title),
            Author = CleanText(author),
            Price = newPrice,
            Discount = discount,
            ProductUrl = productUrl
        };
    }

    private string? ExtractText(HtmlNode node, string[] selectors)
    {
        foreach (var selector in selectors)
        {
            var element = node.SelectSingleNode(selector);
            if (element != null)
            {
                var text = element.InnerText?.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                    return System.Net.WebUtility.HtmlDecode(text);
            }
        }
        return null;
    }

    private string? ExtractHref(HtmlNode node, string[] selectors)
    {
        foreach (var selector in selectors)
        {
            var element = node.SelectSingleNode(selector);
            if (element != null)
            {
                var href = element.GetAttributeValue("href", null);
                if (!string.IsNullOrWhiteSpace(href))
                    return href;
            }
        }
        return null;
    }

    private decimal ExtractPrice(HtmlNode node, string[] selectors)
    {
        foreach (var selector in selectors)
        {
            var element = node.SelectSingleNode(selector);
            if (element != null)
            {
                var priceText = element.InnerText?.Trim();
                if (!string.IsNullOrWhiteSpace(priceText))
                {
                    var price = ParsePrice(priceText);
                    if (price > 0)
                        return price;
                }
            }
        }
        return 0;
    }

    private decimal ParsePrice(string priceText)
    {
        try
        {
            priceText = priceText.Replace("R$", "").Replace("$", "").Trim();
            priceText = Regex.Replace(priceText, @"\s+", "");

            bool hasBrazilianFormat = priceText.Contains(",") &&
                (priceText.LastIndexOf(',') > priceText.LastIndexOf('.') || !priceText.Contains("."));

            if (hasBrazilianFormat)
            {
                priceText = priceText.Replace(".", "").Replace(",", ".");
            }
            else
            {
                priceText = priceText.Replace(",", "");
            }

            priceText = Regex.Replace(priceText, @"[^\d.]", "");

            if (decimal.TryParse(priceText, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                return price;
        }
        catch { }

        return 0;
    }

    private string CleanText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        text = Regex.Replace(text, @"\s+", " ");
        return text.Trim();
    }

    private class ParsedProduct
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Discount { get; set; }
        public string? ProductUrl { get; set; }
    }
}
