using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Sherlock.Business.Core.Scrapers.Cedet.Json;
using Sherlock.Business.Core.Scrapers.Common;
using Sherlock.Business.Interfaces;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Enums;
using System.Diagnostics;

namespace Sherlock.Business.Core.Scrapers.Cedet.HttpClient;

public class CedetSingleSearchHttpClient : IScraper
{
    // Medido em 2026-08-18 nas 67 lojas pelo endpoint JSON: p50 2,65s, p95 3,23s,
    // max 4,28s. O timeout de 30s com 2 retries dava um teto de 91,2s — número que
    // aparecia cru no banco como max(response_time_ms) = 91205, sempre nosso e nunca
    // da loja. Retry contra servidor saturado é a carga que ele não tem como absorver.
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);
    // Buscamos pelo ISBN, que casa com um produto só; 20 é folga para kits homônimos.
    private const int JsonSearchLimit = 20;

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
                retryCount: 1,
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

            // Caminho preferido: o endpoint JSON da própria loja. O HTML fica de rede de
            // segurança para as lojas que não falam esse protocolo (tema diferente,
            // WooCommerce, WAF na frente).
            var candidates = await SearchJsonCandidatesAsync(baseUrl, parameters.Isbn, provider);

            if (candidates is null)
            {
                var searchUrl = BuildSearchUrl(baseUrl, provider.SearchUrlTemplate, parameters.Isbn);
                _logger.LogDebug("[{Provider}] Sem JSON, caindo para HTML: {Url}", provider.Name, searchUrl);
                candidates = await SearchCandidatesAsync(searchUrl, provider);
            }

            if (candidates.Count == 0)
                return QueryResult.CreateNoResult(provider, stopwatch.ElapsedMilliseconds);

            // Buscamos pelo próprio ISBN: confiamos no buscador do provider e usamos o primeiro candidato sem revalidar
            var best = candidates[0];
            var productUrl = BuildProductUrl(best.ProductUrl, baseUrl);

            stopwatch.Stop();
            _logger.LogInformation("[{Provider}] \"{Title}\" - R${Price:F2} em {ElapsedMs}ms ({Count} candidatos)",
                provider.Name, best.Title, best.Price, stopwatch.ElapsedMilliseconds, candidates.Count);

            return QueryResult.CreateSuccess(
                provider,
                best.Title,
                best.Author,
                best.Price,
                best.Discount,
                stopwatch.ElapsedMilliseconds,
                productUrl,
                best.ImageUrl);
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

    /// <summary>
    /// Busca no endpoint JSON da loja. Devolve <c>null</c> quando a loja não respondeu
    /// nesse formato — aí o chamador tenta o HTML.
    ///
    /// Falha de transporte (timeout, DNS, conexão) sobe: se a loja está fora do ar,
    /// tentar o HTML só gastaria o dobro do tempo para falhar igual.
    /// </summary>
    private async Task<List<BookCandidate>?> SearchJsonCandidatesAsync(
        string baseUrl, string isbn, Provider provider)
    {
        var searchUrl = BuildJsonSearchUrl(baseUrl, isbn);
        _logger.LogDebug("[{Provider}] Iniciando busca JSON: {Url}", provider.Name, searchUrl);

        var response = await SendAsync(searchUrl, provider, asJson: true);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogDebug("[{Provider}] Endpoint JSON respondeu HTTP {StatusCode}",
                provider.Name, (int)response.StatusCode);
            return null;
        }

        var payload = await response.Content.ReadAsStringAsync();
        return CedetJsonSearchParser.TryParse(payload);
    }

    private static string BuildJsonSearchUrl(string baseUrl, string isbn)
    {
        var searchTerm = Uri.EscapeDataString(isbn);
        return $"{baseUrl.TrimEnd('/')}/index.php?route=product/search/infiniteScroll" +
               $"&search={searchTerm}&page=1&limit={JsonSearchLimit}";
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

    private static string BuildProductUrl(string productUrl, string baseUrl)
    {
        if (string.IsNullOrEmpty(productUrl)) return "";
        return productUrl.StartsWith("http")
            ? productUrl
            : $"{baseUrl.TrimEnd('/')}/{productUrl.TrimStart('/')}";
    }

    // Validação por ISBN desabilitada temporariamente: confiamos no buscador do provider (busca é feita pelo próprio ISBN)

    private async Task<HttpResponseMessage> SendAsync(string url, Provider provider, bool asJson = false)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            using var request = asJson
                ? BrowserRequestFactory.CreateJson(url)
                : BrowserRequestFactory.Create(url);
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

    /// <summary>
    /// Acima disso, a pagina nao esta respondendo a nossa busca.
    ///
    /// Busca por ISBN casa com um produto so — dois ou tres quando a loja tem
    /// kits com o mesmo codigo. Uma pagina com dezenas de produtos e vitrine, e
    /// nao resultado. Medido em 2026-08-18 na Livraria da Marcela: a busca por
    /// 9788535914849 e a busca por 0000000000000 devolviam a MESMA pagina de
    /// 145.096 bytes, com 36 produtos; o parser pegava o primeiro e gravava "O
    /// flautista de Hamelin" a R$ 34,11 preso a qualquer ISBN.
    /// </summary>
    private const int MaxHtmlSearchResults = 5;

    /// <summary>
    /// A pagina respondeu a busca, ou e uma vitrine que ignora o termo?
    ///
    /// So o caminho HTML precisa disso: no JSON, busca sem resultado devolve
    /// lista vazia e nao ha o que confundir.
    /// </summary>
    internal static bool PareceResultadoDeBusca(int produtosNaPagina) =>
        produtosNaPagina > 0 && produtosNaPagina <= MaxHtmlSearchResults;

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
            if (products == null || products.Count == 0)
                continue;

            if (!PareceResultadoDeBusca(products.Count))
            {
                // Melhor nao responder nada do que responder o livro errado: um
                // preco falso vira "menor preco" e manda a pessoa para a loja
                // comprar outro livro.
                _logger.LogWarning(
                    "Pagina com {Count} produtos para uma busca por ISBN: e vitrine, nao resultado. Ignorando.",
                    products.Count);
                return null;
            }

            _logger.LogDebug("Produtos encontrados com seletor: {Selector} ({Count})", selector, products.Count);
            return products;
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
