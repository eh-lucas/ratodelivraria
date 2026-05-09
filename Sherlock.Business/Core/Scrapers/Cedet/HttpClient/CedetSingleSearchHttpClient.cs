using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Sherlock.Business.Core.Scrapers.Common;
using Sherlock.Business.Interfaces;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Enums;
using System.Diagnostics;

namespace Sherlock.Business.Core.Scrapers.Cedet.HttpClient;

public class CedetSingleSearchHttpClient : IScraper
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);
    private static readonly System.Net.Http.HttpClient _httpClient;
    private static readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    private readonly ILogger<CedetSingleSearchHttpClient> _logger;

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

        _httpClient = new System.Net.Http.HttpClient(handler) { Timeout = RequestTimeout };

        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode && (int)r.StatusCode >= 500)
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: 2,
                sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, retryAttempt)));
    }

    public CedetSingleSearchHttpClient(ILogger<CedetSingleSearchHttpClient> logger)
    {
        _logger = logger;
    }

    public ScraperTypeEnum ScraperType => ScraperTypeEnum.CedetScraper;

    public async Task<QueryResult> ExecuteSearch(SearchParameter parameters)
    {
        var provider = parameters.Source ?? new Provider { Id = 0, Name = "Unknown", Url = string.Empty };
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (string.IsNullOrEmpty(parameters.Isbn))
            {
                _logger.LogDebug("[{Provider}] ISBN vazio, ignorando busca", provider.Name);
                return QueryResult.CreateNoResult(provider, stopwatch.ElapsedMilliseconds);
            }

            var baseUrl = NormalizeUrl(provider.Url);
            var searchUrl = BuildSearchUrl(baseUrl, provider.SearchUrlTemplate, parameters.Isbn);

            _logger.LogDebug("[{Provider}] Iniciando busca: {Url}", provider.Name, searchUrl);

            var candidates = await SearchCandidatesAsync(searchUrl, provider);
            if (candidates.Count == 0)
                return QueryResult.CreateNoResult(provider, stopwatch.ElapsedMilliseconds);

            _logger.LogDebug("[{Provider}] {Count} candidatos, validando ISBN...", provider.Name, candidates.Count);

            foreach (var candidate in candidates)
            {
                var (matched, productUrl) = await TryValidateByIsbnAsync(candidate, parameters.Isbn, baseUrl, provider);
                if (!matched) continue;

                stopwatch.Stop();
                _logger.LogInformation("[{Provider}] ISBN validado! \"{Title}\" - R${Price:F2} em {ElapsedMs}ms",
                    provider.Name, candidate.Title, candidate.Price, stopwatch.ElapsedMilliseconds);

                return QueryResult.CreateSuccess(
                    provider,
                    candidate.Title,
                    candidate.Author,
                    candidate.Price,
                    candidate.Discount,
                    stopwatch.ElapsedMilliseconds,
                    productUrl);
            }

            stopwatch.Stop();
            _logger.LogDebug("[{Provider}] Nenhum candidato com ISBN correspondente em {ElapsedMs}ms",
                provider.Name, stopwatch.ElapsedMilliseconds);
            return QueryResult.CreateNoResult(provider, stopwatch.ElapsedMilliseconds);
        }
        catch (TaskCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("[{Provider}] Timeout após {ElapsedMs}ms", provider.Name, stopwatch.ElapsedMilliseconds);
            return QueryResult.CreateFailure(provider, QueryErrorType.Timeout, "Request timeout", stopwatch.ElapsedMilliseconds);
        }
        catch (HttpRequestException ex) when (ex.StatusCode.HasValue)
        {
            // Falha HTTP com status code preserva o código no QueryResult (HttpError)
            stopwatch.Stop();
            var statusCode = (int)ex.StatusCode.Value;
            _logger.LogWarning("[{Provider}] HTTP {StatusCode} em {ElapsedMs}ms",
                provider.Name, statusCode, stopwatch.ElapsedMilliseconds);
            return QueryResult.CreateFailure(provider, QueryErrorType.HttpError,
                $"HTTP {statusCode}", stopwatch.ElapsedMilliseconds, statusCode);
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning("[{Provider}] Erro de rede: {Message}", provider.Name, ex.Message);
            return QueryResult.CreateFailure(provider, QueryErrorType.Network, ex.Message, stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task<List<BookCandidate>> SearchCandidatesAsync(string searchUrl, Provider provider)
    {
        var response = await SendAsync(searchUrl, provider);
        // EnsureSuccessStatusCode lança HttpRequestException com StatusCode preenchido em 4xx/5xx
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var products = ExtractProducts(doc);
        if (products == null || products.Count == 0)
            return [];

        return ParseProducts(products, provider);
    }

    private async Task<(bool matched, string productUrl)> TryValidateByIsbnAsync(
        BookCandidate candidate, string expectedIsbn, string baseUrl, Provider provider)
    {
        try
        {
            if (string.IsNullOrEmpty(candidate.ProductUrl))
                return (false, "");

            var productUrl = candidate.ProductUrl.StartsWith("http")
                ? candidate.ProductUrl
                : $"{baseUrl.TrimEnd('/')}/{candidate.ProductUrl.TrimStart('/')}";

            var response = await SendAsync(productUrl, provider);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("[{Provider}] Falha ao acessar produto (HTTP {StatusCode})",
                    provider.Name, (int)response.StatusCode);
                return (false, productUrl);
            }

            var html = await response.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Renderiza o texto do body uma única vez (operação cara) e reusa nas duas verificações
            var bodyText = doc.DocumentNode.InnerText;
            var extractedIsbn = IsbnHelper.ExtractFromText(bodyText);

            if (!string.IsNullOrEmpty(extractedIsbn))
            {
                if (IsbnHelper.Matches(extractedIsbn, expectedIsbn))
                    return (true, productUrl);

                _logger.LogDebug("[{Provider}] ISBN não corresponde. Esperado: {Expected}, Encontrado: {Found}",
                    provider.Name, expectedIsbn, extractedIsbn);
                return (false, productUrl);
            }

            // Sem ISBN na página: kits/combos não têm ISBN único, ignoramos silenciosamente
            if (IsKit(doc, bodyText))
                _logger.LogDebug("[{Provider}] Produto é um kit, ignorando: {Title}", provider.Name, candidate.Title);
            else
                _logger.LogWarning("[{Provider}] ISBN não encontrado na página do produto: {Url}", provider.Name, productUrl);

            return (false, productUrl);
        }
        catch (Exception ex)
        {
            _logger.LogDebug("[{Provider}] Erro ao validar produto: {Message}", provider.Name, ex.Message);
            return (false, "");
        }
    }

    private async Task<HttpResponseMessage> SendAsync(string url, Provider provider)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            using var request = BrowserRequestFactory.Create(url);
            _logger.LogDebug("[{Provider}] Enviando requisição para {Url}", provider.Name, url);
            return await _httpClient.SendAsync(request);
        });
    }

    private static string BuildSearchUrl(string baseUrl, string searchUrlTemplate, string isbn)
    {
        var searchTerm = Uri.EscapeDataString(isbn);
        var searchPath = (searchUrlTemplate ?? "").Replace("{search}", searchTerm);
        return $"{baseUrl.TrimEnd('/')}/{searchPath.TrimStart('/')}";
    }

    // Remove www. para evitar redirects desnecessários
    private static string NormalizeUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return url;

        var uri = new Uri(url);
        if (!uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            return url;

        var builder = new UriBuilder(uri) { Host = uri.Host[4..] };
        return builder.Uri.ToString();
    }

    private static bool IsKit(HtmlDocument doc, string bodyText)
    {
        var title = doc.DocumentNode.SelectSingleNode("//h1")?.InnerText?.ToLowerInvariant() ?? "";
        var bodyLower = bodyText.ToLowerInvariant();

        return title.Contains("kit") || title.Contains("combo") || title.Contains("coleção") ||
               bodyLower.Contains("kit de livros") || bodyLower.Contains("combo de livros");
    }

    private HtmlNodeCollection? ExtractProducts(HtmlDocument doc)
    {
        // Tenta seletores comuns: OpenCart (item-product), WooCommerce (li.product), genéricos
        string[] selectors =
        [
            "//div[contains(@class, 'item-product')]",
            "//div[contains(@class, 'product-item')]",
            "//li[contains(@class, 'product')]//div[contains(@class, 'item-product')]",
            "//ul[contains(@class, 'products')]//li[contains(@class, 'product')]",
            "//div[@class='product']",
            "//article[contains(@class, 'product')]"
        ];

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

    private List<BookCandidate> ParseProducts(HtmlNodeCollection products, Provider provider)
    {
        var candidates = new List<BookCandidate>();

        foreach (var product in products)
        {
            try
            {
                var candidate = ParseSingleProduct(product);
                if (candidate != null) candidates.Add(candidate);
            }
            catch (Exception ex)
            {
                _logger.LogDebug("[{Provider}] Erro ao parsear produto: {Message}", provider.Name, ex.Message);
            }
        }

        return candidates;
    }

    private static BookCandidate? ParseSingleProduct(HtmlNode product)
    {
        var title = product.TryExtractText(
            ".//a[contains(@class, 'product-name')]",
            ".//div[contains(@class, 'name')]//a",
            ".//div[contains(@class, 'name')]",
            ".//h2[contains(@class, 'woocommerce-loop-product__title')]",
            ".//h2//a",
            ".//h3//a");

        if (string.IsNullOrEmpty(title)) return null;

        var newPrice = product.TryExtractPrice(
            ".//span[contains(@class, 'price-new')]",
            ".//span[contains(@class, 'sale-price')]",
            ".//ins//span[contains(@class, 'woocommerce-Price-amount')]//bdi");

        // Fallback: nem todo template tem price-new — usa o preço regular
        if (newPrice <= 0)
        {
            newPrice = product.TryExtractPrice(
                ".//span[contains(@class, 'woocommerce-Price-amount')]//bdi",
                ".//span[contains(@class, 'price')]//bdi",
                ".//bdi");
        }

        if (newPrice <= 0) return null;

        var oldPrice = product.TryExtractPrice(
            ".//span[contains(@class, 'price-old')]",
            ".//del//span[contains(@class, 'woocommerce-Price-amount')]//bdi");

        var discount = oldPrice > 0 && oldPrice > newPrice
            ? (int)Math.Round(100 * (1 - (newPrice / oldPrice)))
            : 0;

        var author = product.TryExtractText(
            ".//p[contains(@class, 'author')]//a",
            ".//p[contains(@class, 'author')]",
            ".//span[contains(@class, 'author')]") ?? "";

        var productUrl = product.TryExtractHref(
            ".//a[contains(@class, 'product-name')]",
            ".//a[contains(@class, 'link-card')]",
            ".//div[contains(@class, 'name')]//a",
            ".//h2//a",
            ".//a");

        return new BookCandidate
        {
            Title = HtmlNodeExtensions.CleanText(title),
            Author = HtmlNodeExtensions.CleanText(author),
            Price = newPrice,
            Discount = discount,
            ProductUrl = productUrl
        };
    }
}
