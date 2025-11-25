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
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);
    private readonly ILogger<CedetSingleSearchHttpClient> _logger;

    // HttpClient estático para reutilização de conexões
    private static readonly System.Net.Http.HttpClient _httpClient;
    private static readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    static CedetSingleSearchHttpClient()
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        };

        _httpClient = new System.Net.Http.HttpClient(handler)
        {
            Timeout = RequestTimeout
        };

        // Headers mais completos para evitar bloqueios
        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "pt-BR,pt;q=0.9,en-US;q=0.8,en;q=0.7");
        _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");

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

    public CedetSingleSearchHttpClient() : this(NullLogger<CedetSingleSearchHttpClient>.Instance)
    {
    }

    public CedetSingleSearchHttpClient(ILogger<CedetSingleSearchHttpClient> logger)
    {
        _logger = logger;
    }

    public ScraperTypeEnum ScraperType => ScraperTypeEnum.CedetSingleAgilityHttpClient;

    public async Task<BookPriceResult> ExecuteSearch(SearchParameter parameters)
    {
        var website = parameters.Source?.Url ?? string.Empty;
        var providerName = parameters.Source?.Name ?? website;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (string.IsNullOrEmpty(parameters.BookTitle))
            {
                _logger.LogDebug("[{Provider}] Título vazio, ignorando busca", providerName);
                return new BookPriceResult();
            }

            string searchTerm = Uri.EscapeDataString(parameters.BookTitle);
            string url = $"{website.TrimEnd('/')}/?s={searchTerm}&post_type=product";

            _logger.LogDebug("[{Provider}] Iniciando busca: {Url}", providerName, url);

            var response = await _retryPolicy.ExecuteAsync(async () =>
                await _httpClient.GetAsync(url));

            stopwatch.Stop();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[{Provider}] HTTP {StatusCode} em {ElapsedMs}ms",
                    providerName, (int)response.StatusCode, stopwatch.ElapsedMilliseconds);
                return new BookPriceResult();
            }

            var html = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("[{Provider}] Resposta recebida em {ElapsedMs}ms ({Size} bytes)",
                providerName, stopwatch.ElapsedMilliseconds, html.Length);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var products = ExtractProducts(doc);
            if (products == null || products.Count == 0)
            {
                _logger.LogDebug("[{Provider}] Nenhum produto encontrado", providerName);
                return new BookPriceResult();
            }

            var possibleBooks = ParseProducts(products, website, providerName);

            _logger.LogDebug("[{Provider}] {Count} produtos parseados", providerName, possibleBooks.Count);

            var result = ChooseBestBookOption(possibleBooks, parameters.BookTitle, parameters.IsExactSearch);

            if (!string.IsNullOrEmpty(result.Title) && result.Price > 0)
            {
                _logger.LogInformation("[{Provider}] Encontrado: \"{Title}\" - R${Price:F2} em {ElapsedMs}ms",
                    providerName, result.Title, result.Price, stopwatch.ElapsedMilliseconds);
            }

            return result;
        }
        catch (TaskCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("[{Provider}] Timeout após {ElapsedMs}ms", providerName, stopwatch.ElapsedMilliseconds);
            return new BookPriceResult();
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning("[{Provider}] Erro de rede após {ElapsedMs}ms: {Message}",
                providerName, stopwatch.ElapsedMilliseconds, ex.Message);
            return new BookPriceResult();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[{Provider}] Erro inesperado após {ElapsedMs}ms",
                providerName, stopwatch.ElapsedMilliseconds);
            return new BookPriceResult();
        }
    }

    /// <summary>
    /// Extrai produtos usando múltiplos seletores CSS para maior robustez
    /// </summary>
    private HtmlNodeCollection? ExtractProducts(HtmlDocument doc)
    {
        // Tenta múltiplos seletores em ordem de preferência
        string[] selectors = new[]
        {
            "//div[contains(@class, 'item-product')]",
            "//li[contains(@class, 'product')]",
            "//div[contains(@class, 'product-item')]",
            "//article[contains(@class, 'product')]",
            "//div[contains(@class, 'product') and contains(@class, 'type-product')]"
        };

        foreach (var selector in selectors)
        {
            var products = doc.DocumentNode.SelectNodes(selector);
            if (products != null && products.Count > 0)
            {
                _logger.LogDebug("Produtos encontrados com seletor: {Selector}", selector);
                return products;
            }
        }

        return null;
    }

    /// <summary>
    /// Parseia produtos usando seletores CSS robustos ao invés de índices fixos
    /// </summary>
    private List<BookPriceResult> ParseProducts(HtmlNodeCollection products, string website, string providerName)
    {
        var possibleBooks = new List<BookPriceResult>();

        foreach (var product in products)
        {
            try
            {
                var book = ParseSingleProduct(product, website, providerName);
                if (book != null && !string.IsNullOrEmpty(book.Title) && book.Price > 0)
                {
                    possibleBooks.Add(book);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("[{Provider}] Erro ao parsear produto: {Message}", providerName, ex.Message);
            }
        }

        return possibleBooks;
    }

    /// <summary>
    /// Parseia um único produto usando múltiplos seletores CSS
    /// </summary>
    private BookPriceResult? ParseSingleProduct(HtmlNode product, string website, string providerName)
    {
        // Extrai título - tenta múltiplos seletores
        var title = ExtractText(product, new[]
        {
            ".//a[contains(@class, 'name')]",
            ".//h2[contains(@class, 'title')]//a",
            ".//h2[contains(@class, 'product-title')]//a",
            ".//h3[contains(@class, 'name')]//a",
            ".//div[contains(@class, 'name')]//a",
            ".//a[contains(@class, 'woocommerce-LoopProduct-link')]",
            ".//h2[contains(@class, 'woocommerce-loop-product__title')]"
        });

        if (string.IsNullOrEmpty(title))
            return null;

        // Extrai autor - tenta múltiplos seletores
        var author = ExtractText(product, new[]
        {
            ".//span[contains(@class, 'author')]",
            ".//div[contains(@class, 'author')]",
            ".//p[contains(@class, 'author')]",
            ".//a[contains(@class, 'author')]"
        }) ?? "";

        // Extrai preço atual (com desconto se houver)
        var newPrice = ExtractPrice(product, new[]
        {
            ".//span[contains(@class, 'price-new')]",
            ".//ins//span[contains(@class, 'amount')]",
            ".//span[contains(@class, 'woocommerce-Price-amount')]",
            ".//span[contains(@class, 'price')]//ins//bdi",
            ".//p[contains(@class, 'price')]//ins//span",
            ".//span[contains(@class, 'current-price')]"
        });

        // Se não achou preço com desconto, tenta preço normal
        if (newPrice <= 0)
        {
            newPrice = ExtractPrice(product, new[]
            {
                ".//span[contains(@class, 'woocommerce-Price-amount')]//bdi",
                ".//span[contains(@class, 'price')]//bdi",
                ".//p[contains(@class, 'price')]//span[contains(@class, 'amount')]",
                ".//span[contains(@class, 'amount')]"
            });
        }

        if (newPrice <= 0)
            return null;

        // Extrai preço antigo (sem desconto) para calcular desconto
        var oldPrice = ExtractPrice(product, new[]
        {
            ".//span[contains(@class, 'price-old')]",
            ".//del//span[contains(@class, 'amount')]",
            ".//span[contains(@class, 'price')]//del//bdi",
            ".//p[contains(@class, 'price')]//del//span"
        });

        // Calcula desconto
        int discount = 0;
        if (oldPrice > 0 && oldPrice > newPrice)
        {
            discount = (int)Math.Round(100 * (1 - (newPrice / oldPrice)));
        }

        return new BookPriceResult
        {
            Title = CleanText(title),
            Author = CleanText(author),
            Price = newPrice,
            Discount = discount,
            Website = providerName
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
    private static BookPriceResult ChooseBestBookOption(List<BookPriceResult> possibleBooks, string bookTitle, bool isExactSearch)
    {
        if (possibleBooks.Count == 0)
            return new BookPriceResult();

        var normalizedSearch = NormalizeForComparison(bookTitle);

        if (isExactSearch)
        {
            var exactMatch = possibleBooks.FirstOrDefault(b =>
                NormalizeForComparison(b.Title) == normalizedSearch);

            return exactMatch ?? new BookPriceResult();
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
            .FirstOrDefault() ?? new BookPriceResult();
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
}
