using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sherlock.Domain.Entities;

namespace Sherlock.Business.Core.Crawling;

/// <summary>
/// Varre o catálogo das lojas Cedet pelo endpoint JSON <c>product/search/infiniteScroll</c>.
///
/// Com <c>search=</c> vazio o endpoint pagina a loja inteira, o que torna a coleta barata:
/// cerca de 13 requisições por loja com <c>limit=500</c>, em vez de uma por produto.
/// O JSON não traz o ISBN — ele fica na página do produto e é resolvido sob demanda.
/// </summary>
public class CatalogCrawler
{
    private static readonly HttpClient HttpClient;

    /// <summary>
    /// Um semáforo por endereço IP de destino.
    ///
    /// As lojas são domínios distintos hospedados na mesma máquina (a plataforma Cedet
    /// serve dezenas delas do mesmo IP). Limitar por domínio não protege nada: quatro
    /// "lojas diferentes" em paralelo viram quatro varreduras de catálogo no mesmo
    /// servidor, que responde 504. O limite real precisa ser por servidor.
    /// </summary>
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> HostThrottles = new();

    private readonly ILogger<CatalogCrawler> _logger;
    private readonly CatalogCrawlSettings _settings;

    private const string ChromeUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    static CatalogCrawler()
    {
        // Compressão automática: as páginas de catálogo vêm em gzip e economizam banda das lojas.
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All,
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5,
        };

        // Timeout generoso: o custo real é a loja montar a página, não a rede.
        HttpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(180) };
    }

    public CatalogCrawler(
        IOptions<CatalogCrawlSettings> settings,
        ILogger<CatalogCrawler> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Coleta o catálogo das lojas informadas. Cada loja é varrida sequencialmente;
    /// o paralelismo acontece apenas entre lojas diferentes.
    ///
    /// <paramref name="onProviderCompleted"/> é chamado assim que cada loja termina, para
    /// que o resultado seja gravado na hora: uma falha tardia não descarta o já coletado,
    /// e o catálogo inteiro nunca precisa caber na memória de uma vez.
    /// </summary>
    public async Task<int> CrawlAsync(
        IReadOnlyList<Provider> providers,
        List<ProviderCrawlReport> reports,
        Func<List<CatalogItem>, ProviderCrawlReport, Task> onProviderCompleted,
        IReadOnlySet<string>? knownProductIds = null,
        CancellationToken cancellationToken = default)
    {
        var totalItems = 0;
        var reportsLock = new object();
        using var throttle = new SemaphoreSlim(_settings.MaxParallelProviders);

        var tasks = providers.Select(async provider =>
        {
            await throttle.WaitAsync(cancellationToken);
            try
            {
                var report = new ProviderCrawlReport
                {
                    ProviderId = provider.Id,
                    ProviderName = provider.Name,
                };
                var collected = await CrawlProviderAsync(provider, report, knownProductIds, cancellationToken);

                try
                {
                    await onProviderCompleted(collected, report);
                }
                catch (Exception ex)
                {
                    report.Error = $"Falha ao gravar: {ex.Message}";
                    report.Success = false;
                    _logger.LogError(ex, "{Provider}: erro ao gravar itens", provider.Name);
                }

                lock (reportsLock)
                {
                    reports.Add(report);
                    totalItems += collected.Count;
                }
            }
            finally
            {
                throttle.Release();
            }
        });

        await Task.WhenAll(tasks);
        return totalItems;
    }

    /// <summary>Chave de agrupamento: o IP que atende a loja, ou o host se o DNS falhar.</summary>
    private static async Task<string> ResolveHostKeyAsync(string url)
    {
        try
        {
            var host = new Uri(url).Host;
            var addresses = await Dns.GetHostAddressesAsync(host);
            return addresses.Length > 0 ? addresses[0].ToString() : host;
        }
        catch
        {
            return url;
        }
    }

    private async Task<List<CatalogItem>> CrawlProviderAsync(
        Provider provider,
        ProviderCrawlReport report,
        IReadOnlySet<string>? knownProductIds,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var items = new List<CatalogItem>();
        var seenProductIds = new HashSet<string>();
        var consecutiveErrors = 0;

        var hostKey = await ResolveHostKeyAsync(provider.Url);

        // Modo incremental: só vale se já temos catálogo para comparar.
        var incremental = knownProductIds is { Count: > 0 };
        var pagesWithoutNews = 0;

        for (var page = 1; page <= _settings.MaxPagesPerProvider; page++)
        {
            if (cancellationToken.IsCancellationRequested) break;

            // Pausa entre páginas da mesma loja — nunca dispara em rajada.
            if (page > 1)
                await Task.Delay(_settings.DelayBetweenPagesMs, cancellationToken);

            var url = BuildPageUrl(provider.Url, page, incremental);

            try
            {
                // Serializa por servidor: lojas vizinhas na mesma máquina esperam a vez.
                var throttle = HostThrottles.GetOrAdd(hostKey, _ => new SemaphoreSlim(1, 1));
                await throttle.WaitAsync(cancellationToken);

                string json;
                try
                {
                    json = await FetchAsync(url, cancellationToken);
                }
                finally
                {
                    throttle.Release();
                }

                var (pageItems, reportedPages) = ParsePage(json, provider.Id);

                report.PagesFetched++;
                if (reportedPages > 0) report.ReportedPages = reportedPages;

                if (pageItems.Count == 0) break;

                // Loja que ignora o parâmetro de página devolveria sempre o mesmo bloco.
                var novel = pageItems.Where(i => seenProductIds.Add(i.ProductId)).ToList();
                if (novel.Count == 0)
                {
                    _logger.LogWarning(
                        "{Provider}: página {Page} repetiu produtos já vistos — encerrando",
                        provider.Name, page);
                    break;
                }

                items.AddRange(novel);
                consecutiveErrors = 0;

                if (incremental)
                {
                    var newToCatalog = novel.Count(i => !knownProductIds!.Contains(i.ProductId));
                    report.NewProducts += newToCatalog;

                    if (newToCatalog == 0)
                    {
                        pagesWithoutNews++;
                        if (pagesWithoutNews >= _settings.StopAfterKnownPages)
                        {
                            report.StoppedEarly = true;
                            _logger.LogInformation(
                                "{Provider}: encerrando na página {Page} — {Pages} páginas seguidas sem novidade",
                                provider.Name, page, pagesWithoutNews);
                            break;
                        }
                    }
                    else
                    {
                        pagesWithoutNews = 0;
                    }
                }

                // pagination_total é a contagem de PÁGINAS para o limit pedido — não de produtos.
                if (report.ReportedPages > 0 && page >= report.ReportedPages) break;
            }
            catch (Exception ex)
            {
                consecutiveErrors++;
                _logger.LogWarning(ex,
                    "{Provider}: falha na página {Page} ({Errors}/{Max})",
                    provider.Name, page, consecutiveErrors, _settings.MaxConsecutiveErrors);

                if (consecutiveErrors >= _settings.MaxConsecutiveErrors)
                {
                    report.Error = ex.Message;
                    break;
                }

                // Recuo exponencial antes de tentar de novo: 1.6s, 3.2s, 6.4s...
                var backoff = _settings.DelayBetweenPagesMs * (int)Math.Pow(2, consecutiveErrors);
                await Task.Delay(backoff, cancellationToken);
            }
        }

        stopwatch.Stop();
        report.ElapsedMs = stopwatch.ElapsedMilliseconds;
        report.ItemsCollected = items.Count;
        report.Success = items.Count > 0 && report.Error is null;

        _logger.LogInformation(
            "{Provider}: {Items} itens em {Pages} páginas ({Elapsed}ms)",
            provider.Name, items.Count, report.PagesFetched, stopwatch.ElapsedMilliseconds);

        return items;
    }

    private string BuildPageUrl(string baseUrl, int page, bool newestFirst)
    {
        var root = baseUrl.TrimEnd('/');
        var url = $"{root}/index.php?route=product/search/infiniteScroll" +
                  $"&search=&page={page}&limit={_settings.PageSize}";

        // Do mais recente para o mais antigo: o que ainda não conhecemos vem primeiro.
        if (newestFirst)
            url += "&sort=p.date_added&order=DESC";

        return url;
    }

    private static async Task<string> FetchAsync(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", ChromeUserAgent);
        request.Headers.Add("Accept", "application/json, text/javascript, */*; q=0.01");
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        request.Headers.Add("Accept-Language", "pt-BR,pt;q=0.9,en-US;q=0.8");

        using var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static (List<CatalogItem> Items, int ReportedPages) ParsePage(string json, int providerId)
    {
        var items = new List<CatalogItem>();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        // pagination_total = número de páginas disponíveis para o 'limit' informado.
        var pages = 0;
        if (root.TryGetProperty("pagination_total", out var totalElement))
            pages = ReadInt(totalElement);

        if (!root.TryGetProperty("products", out var products) ||
            products.ValueKind != JsonValueKind.Array)
        {
            return (items, pages);
        }

        foreach (var product in products.EnumerateArray())
        {
            var productId = product.TryGetProperty("product_id", out var idElement)
                ? ReadString(idElement)
                : null;
            var name = product.TryGetProperty("name", out var nameElement)
                ? ReadString(nameElement)
                : null;

            if (string.IsNullOrWhiteSpace(productId) || string.IsNullOrWhiteSpace(name))
                continue;

            items.Add(new CatalogItem
            {
                ProviderId = providerId,
                ProductId = productId,
                Name = name.Trim(),
                NameNormalized = Normalize(name),
                Authors = ReadAuthors(product),
                Price = ReadPrice(product),
                Href = product.TryGetProperty("href", out var hrefElement)
                    ? ReadString(hrefElement)
                    : null,
            });
        }

        return (items, pages);
    }

    private static string? ReadAuthors(JsonElement product)
    {
        if (!product.TryGetProperty("authors", out var authors) ||
            authors.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var names = authors.EnumerateArray()
            .Select(a => a.TryGetProperty("author_name", out var n) ? ReadString(n) : null)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();

        if (names.Count == 0) return null;

        var joined = string.Join(", ", names);
        return joined.Length > 500 ? joined[..500] : joined;
    }

    /// <summary>Prefere o preço promocional; aceita "R$ 124,90" e "91,18".</summary>
    private static decimal? ReadPrice(JsonElement product)
    {
        foreach (var field in new[] { "special", "price" })
        {
            if (!product.TryGetProperty(field, out var element)) continue;

            var raw = ReadString(element);
            if (string.IsNullOrWhiteSpace(raw)) continue;

            var digits = new string(raw.Where(c => char.IsDigit(c) || c is ',' or '.').ToArray());
            if (digits.Length == 0) continue;

            // Formato brasileiro: ponto é milhar, vírgula é decimal.
            digits = digits.Replace(".", string.Empty).Replace(',', '.');

            if (decimal.TryParse(digits, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
                && value > 0)
            {
                return value;
            }
        }

        return null;
    }

    private static string? ReadString(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.GetRawText(),
        _ => null,
    };

    private static int ReadInt(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Number => element.TryGetInt32(out var n) ? n : 0,
        JsonValueKind.String => int.TryParse(element.GetString(), out var n) ? n : 0,
        _ => 0,
    };

    /// <summary>Minúsculo e sem acento, para a busca por nome não depender de digitação exata.</summary>
    public static string Normalize(string text)
    {
        var decomposed = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }

        var result = builder.ToString().Normalize(NormalizationForm.FormC);
        return result.Length > 500 ? result[..500] : result;
    }
}
