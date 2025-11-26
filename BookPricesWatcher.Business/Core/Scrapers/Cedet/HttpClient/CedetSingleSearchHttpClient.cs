using HtmlAgilityPack;
using Sherlock.Domain.Entities;
using System.Globalization;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Sherlock.Domain.Enums;
using Sherlock.Business.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;
using Polly.Retry;

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
            // Permite mais conexões simultâneas
            MaxConnectionsPerServer = 100,
            // Conexões podem ser reutilizadas
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            // Timeout para estabelecer conexão
            ConnectTimeout = TimeSpan.FromSeconds(10),
            // Segue redirects automaticamente (301, 302, 307, 308)
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5
        };

        _httpClient = new System.Net.Http.HttpClient(handler)
        {
            Timeout = RequestTimeout
        };

        // Retry policy com Polly: 2 retries com exponential backoff
        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode && (int)r.StatusCode >= 500)
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: 2,
                sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    // Log será feito no método principal
                });
    }

    /// <summary>
    /// Cria um HttpRequestMessage com headers apropriados (thread-safe)
    /// </summary>
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
            if (string.IsNullOrEmpty(parameters.BookTitle))
            {
                _logger.LogDebug("[{Provider}] Título vazio, ignorando busca", provider.Name);
                return QueryResult.CreateNoResult(provider, stopwatch.ElapsedMilliseconds);
            }

            var searchTerm = SetSearchingParameter(parameters);
            // Normaliza a URL removendo www. para evitar redirects
            var baseUrl = NormalizeUrl(provider.Url);
            // Usa o template de URL do provider (permite WooCommerce, OpenCart, etc)
            var searchPath = provider.SearchUrlTemplate.Replace("{search}", searchTerm);
            var url = $"{baseUrl.TrimEnd('/')}/{searchPath.TrimStart('/')}";

            _logger.LogDebug("[{Provider}] Iniciando busca: {Url}", provider.Name, url);

            HttpResponseMessage response;
            try
            {
                response = await _retryPolicy.ExecuteAsync(async () =>
                {
                    using var request = CreateRequest(url);
                    _logger.LogDebug("[{Provider}] Enviando requisição para {Url}", provider.Name, url);
                    return await _httpClient.SendAsync(request);
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogWarning(ex, "[{Provider}] Exceção durante requisição HTTP após {ElapsedMs}ms: {Message}",
                    provider.Name, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }

            stopwatch.Stop();

            if (!response.IsSuccessStatusCode)
            {
                // Log detalhado para diagnóstico de redirects
                var locationHeader = response.Headers.Location?.ToString() ?? "N/A";
                var finalUrl = response.RequestMessage?.RequestUri?.ToString() ?? "N/A";
                _logger.LogWarning("[{Provider}] HTTP {StatusCode} em {ElapsedMs}ms - Location: {Location} - FinalUrl: {FinalUrl} - OriginalUrl: {OriginalUrl}",
                    provider.Name, (int)response.StatusCode, stopwatch.ElapsedMilliseconds, locationHeader, finalUrl, url);

                return QueryResult.CreateFailure(
                    provider,
                    QueryErrorType.HttpError,
                    $"HTTP {(int)response.StatusCode}",
                    stopwatch.ElapsedMilliseconds,
                    (int)response.StatusCode);
            }

            var html = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("[{Provider}] Resposta recebida em {ElapsedMs}ms ({Size} bytes)",
                provider.Name, stopwatch.ElapsedMilliseconds, html.Length);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var products = ExtractProducts(doc);
            if (products == null || products.Count == 0)
            {
                _logger.LogDebug("[{Provider}] Nenhum produto encontrado", provider.Name);
                return QueryResult.CreateNoResult(provider, stopwatch.ElapsedMilliseconds);
            }

            var possibleBooks = ParseProducts(products, provider);

            _logger.LogDebug("[{Provider}] {Count} produtos parseados", provider.Name, possibleBooks.Count);

            var bestBook = ChooseBestBookOption(possibleBooks, parameters.BookTitle, parameters.IsExactSearch);

            if (bestBook != null && !string.IsNullOrEmpty(bestBook.Title) && bestBook.Price > 0)
            {
                _logger.LogInformation("[{Provider}] Encontrado: \"{Title}\" - R${Price:F2} em {ElapsedMs}ms",
                    provider.Name, bestBook.Title, bestBook.Price, stopwatch.ElapsedMilliseconds);

                return QueryResult.CreateSuccess(
                    provider,
                    bestBook.Title,
                    bestBook.Author,
                    bestBook.Price,
                    bestBook.Discount,
                    stopwatch.ElapsedMilliseconds,
                    bestBook.ProductUrl);
            }

            return QueryResult.CreateNoResult(provider, stopwatch.ElapsedMilliseconds);
        }
        catch (TaskCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("[{Provider}] Timeout após {ElapsedMs}ms", provider.Name, stopwatch.ElapsedMilliseconds);

            return QueryResult.CreateFailure(
                provider,
                QueryErrorType.Timeout,
                "Request timeout",
                stopwatch.ElapsedMilliseconds);
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning("[{Provider}] Erro de rede após {ElapsedMs}ms: {Message}",
                provider.Name, stopwatch.ElapsedMilliseconds, ex.Message);

            return QueryResult.CreateFailure(
                provider,
                QueryErrorType.Network,
                ex.Message,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[{Provider}] Erro inesperado após {ElapsedMs}ms",
                provider.Name, stopwatch.ElapsedMilliseconds);

            return QueryResult.CreateFailure(
                provider,
                QueryErrorType.Unknown,
                ex.Message,
                stopwatch.ElapsedMilliseconds);
        }
    }

    private string SetSearchingParameter(SearchParameter parameters)
    {
        // Sempre usa o título do livro como termo de busca
        if (!string.IsNullOrEmpty(parameters.Isbn))
            return Uri.EscapeDataString(parameters.Isbn);

        return Uri.EscapeDataString(parameters.BookTitle);
    }

    /// <summary>
    /// Normaliza a URL removendo www. para evitar redirects HTTPS->HTTP
    /// que o HttpClient não segue por segurança
    /// </summary>
    private static string NormalizeUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return url;

        // Remove www. do host
        var uri = new Uri(url);
        if (uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            var newHost = uri.Host.Substring(4);
            var builder = new UriBuilder(uri)
            {
                Host = newHost
            };
            return builder.Uri.ToString();
        }

        return url;
    }

    /// <summary>
    /// Extrai produtos usando múltiplos seletores CSS para maior robustez
    /// </summary>
    private HtmlNodeCollection? ExtractProducts(HtmlDocument doc)
    {
        // Tenta múltiplos seletores em ordem de preferência
        string[] selectors = new[]
        {
            // Cedet - estrutura principal (item-product é a classe correta!)
            "//div[contains(@class, 'item-product')]",
            "//div[contains(@class, 'product-item')]",
            "//li[contains(@class, 'product')]//div[contains(@class, 'item-product')]",
            // WooCommerce padrão
            "//ul[contains(@class, 'products')]//li[contains(@class, 'product')]",
            "//div[@class='product']",
            "//article[contains(@class, 'product')]"
        };

        foreach (var selector in selectors)
        {
            var products = doc.DocumentNode.SelectNodes(selector);
            if (products != null && products.Count > 0)
            {
                _logger.LogDebug("Produtos encontrados com seletor: {Selector} ({Count} produtos)", selector, products.Count);
                return products;
            }
        }

        _logger.LogDebug("Nenhum produto encontrado no HTML");
        return null;
    }

    /// <summary>
    /// Parseia produtos usando seletores CSS robustos ao invés de índices fixos
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
    /// Parseia um único produto usando múltiplos seletores CSS
    /// </summary>
    private ParsedProduct? ParseSingleProduct(HtmlNode product, Provider provider)
    {
        // Extrai título - Cedet usa a.product-name ou div.name/a
        var title = ExtractText(product, new[]
        {
            // Cedet - seletores corretos identificados no HTML
            ".//a[contains(@class, 'product-name')]",
            ".//div[contains(@class, 'name')]//a",
            ".//div[contains(@class, 'name')]",
            // WooCommerce
            ".//h2[contains(@class, 'woocommerce-loop-product__title')]",
            ".//a[contains(@class, 'woocommerce-LoopProduct-link')]//h2",
            ".//h2//a",
            ".//h3//a"
        });

        if (string.IsNullOrEmpty(title))
            return null;

        // Extrai autor - Cedet usa p.author ou p.author/a
        var author = ExtractText(product, new[]
        {
            // Cedet - seletores corretos identificados no HTML
            ".//p[contains(@class, 'author')]//a",
            ".//p[contains(@class, 'author')]",
            ".//a[contains(@href, 'author')]",
            ".//span[contains(@class, 'author')]",
            ".//div[contains(@class, 'author')]"
        }) ?? "";

        // Extrai preço atual - Cedet usa span.price-new
        var newPrice = ExtractPrice(product, new[]
        {
            // Cedet - seletores corretos identificados no HTML
            ".//span[contains(@class, 'price-new')]",
            ".//span[contains(@class, 'sale-price')]",
            ".//span[contains(@class, 'special')]",
            // WooCommerce
            ".//ins//span[contains(@class, 'woocommerce-Price-amount')]//bdi",
            ".//ins//span[contains(@class, 'woocommerce-Price-amount')]",
            ".//ins//bdi"
        });

        // Se não achou preço com desconto, tenta preço normal
        if (newPrice <= 0)
        {
            newPrice = ExtractPrice(product, new[]
            {
                ".//span[contains(@class, 'woocommerce-Price-amount')]//bdi",
                ".//span[contains(@class, 'woocommerce-Price-amount')]",
                ".//span[contains(@class, 'price')]//bdi",
                ".//bdi"
            });
        }

        if (newPrice <= 0)
            return null;

        // Extrai preço antigo - Cedet usa span.price-old
        var oldPrice = ExtractPrice(product, new[]
        {
            // Cedet - seletores corretos identificados no HTML
            ".//span[contains(@class, 'price-old')]",
            ".//span[contains(@class, 'price-of')]",
            ".//span[contains(@class, 'original-price')]",
            // WooCommerce
            ".//del//span[contains(@class, 'woocommerce-Price-amount')]//bdi",
            ".//del//span[contains(@class, 'woocommerce-Price-amount')]",
            ".//del//bdi"
        });

        // Calcula desconto
        int discount = 0;
        if (oldPrice > 0 && oldPrice > newPrice)
        {
            discount = (int)Math.Round(100 * (1 - (newPrice / oldPrice)));
        }

        // Tenta extrair URL do produto
        // Cedet: a.product-name ou a.link-card
        var productUrl = ExtractHref(product, new[]
        {
            ".//a[contains(@class, 'product-name')]",
            ".//a[contains(@class, 'link-card')]",
            ".//div[contains(@class, 'name')]//a",
            ".//a[contains(@class, 'woocommerce-LoopProduct-link')]",
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

    /// <summary>
    /// Extrai texto usando múltiplos seletores XPath
    /// </summary>
    private string? ExtractText(HtmlNode node, string[] selectors)
    {
        foreach (var selector in selectors)
        {
            var element = node.SelectSingleNode(selector);
            if (element != null)
            {
                var text = element.InnerText?.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return System.Net.WebUtility.HtmlDecode(text);
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Extrai href usando múltiplos seletores XPath
    /// </summary>
    private string? ExtractHref(HtmlNode node, string[] selectors)
    {
        foreach (var selector in selectors)
        {
            var element = node.SelectSingleNode(selector);
            if (element != null)
            {
                var href = element.GetAttributeValue("href", null);
                if (!string.IsNullOrWhiteSpace(href))
                {
                    return href;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Extrai preço usando múltiplos seletores XPath
    /// </summary>
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

    /// <summary>
    /// Parseia preço de string para decimal, tratando múltiplos formatos
    /// </summary>
    private decimal ParsePrice(string priceText)
    {
        try
        {
            // Remove caracteres não numéricos exceto vírgula e ponto
            priceText = priceText.Replace("R$", "").Replace("$", "").Trim();

            // Remove espaços
            priceText = Regex.Replace(priceText, @"\s+", "");

            // Detecta formato brasileiro (1.234,56) vs americano (1,234.56)
            bool hasBrazilianFormat = priceText.Contains(",") &&
                (priceText.LastIndexOf(',') > priceText.LastIndexOf('.') || !priceText.Contains("."));

            if (hasBrazilianFormat)
            {
                // Remove pontos de milhar e troca vírgula por ponto
                priceText = priceText.Replace(".", "").Replace(",", ".");
            }
            else
            {
                // Remove vírgulas de milhar
                priceText = priceText.Replace(",", "");
            }

            // Remove qualquer caractere restante que não seja número ou ponto
            priceText = Regex.Replace(priceText, @"[^\d.]", "");

            if (decimal.TryParse(priceText, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
            {
                return price;
            }
        }
        catch
        {
            // Ignora erros de parsing
        }

        return 0;
    }

    /// <summary>
    /// Limpa texto removendo espaços extras e caracteres especiais
    /// </summary>
    private string CleanText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        // Remove espaços múltiplos
        text = Regex.Replace(text, @"\s+", " ");

        return text.Trim();
    }

    /// <summary>
    /// Escolhe o melhor livro baseado no título buscado
    /// </summary>
    private static ParsedProduct? ChooseBestBookOption(List<ParsedProduct> possibleBooks, string bookTitle, bool isExactSearch)
    {
        if (possibleBooks.Count == 0)
            return null;

        var normalizedSearch = NormalizeForComparison(bookTitle);

        if (isExactSearch)
        {
            var exactMatch = possibleBooks.FirstOrDefault(b =>
                NormalizeForComparison(b.Title) == normalizedSearch);

            return exactMatch;
        }

        // Primeiro tenta match exato
        var exact = possibleBooks.FirstOrDefault(b =>
            NormalizeForComparison(b.Title) == normalizedSearch);

        if (exact != null)
            return exact;

        // Depois tenta match que contém o termo buscado
        var contains = possibleBooks
            .Where(b => NormalizeForComparison(b.Title).Contains(normalizedSearch) ||
                       normalizedSearch.Contains(NormalizeForComparison(b.Title)))
            .OrderBy(b => b.Price)
            .FirstOrDefault();

        if (contains != null)
            return contains;

        // Por último, retorna o de menor preço
        return possibleBooks
            .Where(b => b.Price > 0)
            .OrderBy(b => b.Price)
            .FirstOrDefault();
    }

    /// <summary>
    /// Normaliza texto para comparação (remove acentos, lowercase, etc)
    /// </summary>
    private static string NormalizeForComparison(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        text = text.ToLowerInvariant().Trim();

        // Remove acentos
        var normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
        var stringBuilder = new System.Text.StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        text = stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC);

        // Remove caracteres especiais
        text = Regex.Replace(text, @"[^\w\s]", " ");

        // Remove espaços múltiplos
        text = Regex.Replace(text, @"\s+", " ");

        return text.Trim();
    }

    /// <summary>
    /// Classe interna para armazenar dados parseados antes de criar QueryResult
    /// </summary>
    private class ParsedProduct
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Discount { get; set; }
        public string? ProductUrl { get; set; }
    }
}
