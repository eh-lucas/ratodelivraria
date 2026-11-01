using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sherlock.Business.Core.Crawling;
using Sherlock.Business.DTOs;
using Sherlock.Business.Interfaces;
using Sherlock.Domain.Interfaces;

namespace Sherlock.Business.Services;

public class CatalogService : ICatalogService
{
    private readonly ICatalogRepository _catalogRepository;
    private readonly IProviderRepository _providerRepository;
    private readonly CatalogCrawler _crawler;
    private readonly CatalogCrawlSettings _settings;
    private readonly ILogger<CatalogService> _logger;

    private static readonly HttpClient HttpClient;

    /// <summary>ISBN na página do produto: "ISBN: 9788594090782".</summary>
    private static readonly Regex IsbnPattern = new(
        @"ISBN[^0-9]{0,10}((?:97[89])[-\s]?\d[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d|\d{13}|\d{10})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Capa pelo Open Graph. É o caminho mais confiável nessas lojas: todas são
    /// OpenCart e a og:image aponta para a imagem grande do produto, enquanto o
    /// &lt;img&gt; da página costuma ser a miniatura já redimensionada.
    /// </summary>
    private static readonly Regex OgImagePattern = new(
        @"<meta[^>]+property\s*=\s*[""']og:image[""'][^>]*content\s*=\s*[""']([^""']+)[""']"
        + @"|<meta[^>]+content\s*=\s*[""']([^""']+)[""'][^>]*property\s*=\s*[""']og:image[""']",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Autor e editora na ficha do produto. O OpenCart renderiza isso como
    /// "&lt;span&gt;Autor:&lt;/span&gt; Fulano" ou numa linha de tabela; o padrão
    /// aceita as duas formas e para na primeira tag que fechar.
    /// </summary>
    private static readonly Regex AuthorPattern = new(
        @"Autor(?:es)?\s*:?\s*</[a-z]+>\s*(?:<[^>]+>\s*)?([^<]{2,120})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PublisherPattern = new(
        @"Editora\s*:?\s*</[a-z]+>\s*(?:<[^>]+>\s*)?([^<]{2,120})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private const string ChromeUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    static CatalogService()
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All,
        };
        HttpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
    }

    public CatalogService(
        ICatalogRepository catalogRepository,
        IProviderRepository providerRepository,
        CatalogCrawler crawler,
        IOptions<CatalogCrawlSettings> settings,
        ILogger<CatalogService> logger)
    {
        _catalogRepository = catalogRepository;
        _providerRepository = providerRepository;
        _crawler = crawler;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<CatalogCrawlResult> CrawlAsync(
        IReadOnlyList<int>? providerIds,
        int? maxProviders,
        bool force = false,
        bool full = false,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var providers = (await _providerRepository.GetActivesAsync())
            .OrderBy(p => p.Id)
            .ToList();

        if (providerIds is { Count: > 0 })
            providers = providers.Where(p => providerIds.Contains(p.Id)).ToList();

        // Uma loja varrida há poucos dias não muda o suficiente para justificar
        // refazer ~37 páginas dela. Em refresh periódico, isso é a maior economia.
        var skipped = 0;
        if (!force && _settings.SkipIfCrawledWithinDays > 0)
        {
            var lastCrawl = await _catalogRepository.GetLastCrawlByProviderAsync(cancellationToken);
            var cutoff = DateTime.UtcNow.AddDays(-_settings.SkipIfCrawledWithinDays);

            var before = providers.Count;
            providers = providers
                .Where(p => !lastCrawl.TryGetValue(p.Id, out var last) || last < cutoff)
                .ToList();
            skipped = before - providers.Count;

            if (skipped > 0)
                _logger.LogInformation("{Skipped} lojas puladas (varridas nos últimos {Days} dias)",
                    skipped, _settings.SkipIfCrawledWithinDays);
        }

        if (maxProviders is > 0)
            providers = providers.Take(maxProviders.Value).ToList();

        _logger.LogInformation("Crawl iniciado em {Count} lojas", providers.Count);

        var result = new CatalogCrawlResult
        {
            ProvidersAttempted = providers.Count,
            ProvidersSkipped = skipped,
        };

        var saved = 0;

        // Grava assim que cada loja termina: o progresso fica visível no banco durante
        // a execução e uma falha lá na frente não descarta o que já foi coletado.
        // Ids globais já vistos: base da parada antecipada. Em varredura completa
        // (full = true) ignoramos, para reconstruir o catálogo do zero.
        var known = full
            ? new HashSet<string>()
            : await _catalogRepository.GetKnownProductIdsAsync(cancellationToken);

        if (known.Count > 0)
            _logger.LogInformation("Modo incremental: {Count} produtos já conhecidos", known.Count);

        var collected = await _crawler.CrawlAsync(
            providers,
            result.Providers,
            async (items, report) =>
            {
                if (items.Count == 0) return;

                var written = await _catalogRepository.UpsertAsync(items, cancellationToken);
                Interlocked.Add(ref saved, written);

                _logger.LogInformation(
                    "{Provider}: {Count} itens gravados", report.ProviderName, written);
            },
            known,
            cancellationToken);

        result.ItemsCollected = collected;
        result.ItemsSaved = saved;
        result.ProvidersSucceeded = result.Providers.Count(r => r.Success);
        result.Providers = result.Providers.OrderBy(r => r.ProviderName).ToList();

        stopwatch.Stop();
        result.ElapsedMs = stopwatch.ElapsedMilliseconds;

        _logger.LogInformation(
            "Crawl concluído: {Saved} itens de {Ok}/{Total} lojas em {Elapsed}ms",
            result.ItemsSaved, result.ProvidersSucceeded, result.ProvidersAttempted, result.ElapsedMs);

        return result;
    }

    public async Task<List<CatalogSuggestionDto>> SuggestAsync(
        string query, int limit, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 3)
            return new List<CatalogSuggestionDto>();

        var normalized = CatalogCrawler.Normalize(query);

        // Busca com folga: o mesmo título aparece em várias lojas e será agrupado.
        var matches = await _catalogRepository.SearchByNameAsync(
            normalized, limit * 20, cancellationToken);

        return matches
            .GroupBy(m => m.NameNormalized)
            .Select(g =>
            {
                // Prefere a linha que já tem ISBN: poupa uma ida à loja depois.
                var best = g.FirstOrDefault(x => x.Isbn != null) ?? g.First();
                return new CatalogSuggestionDto
                {
                    Id = best.Id,
                    Title = best.Name,
                    Isbn = best.Isbn,
                };
            })
            .OrderBy(s => !s.Title.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            .ThenBy(s => s.Title.Length)
            .Take(limit)
            .ToList();
    }

    public async Task<ResolveIsbnResultDto> ResolveIsbnAsync(
        int catalogItemId, CancellationToken cancellationToken = default)
    {
        var item = await _catalogRepository.GetByIdAsync(catalogItemId, cancellationToken);

        if (item is null)
            return new ResolveIsbnResultDto { Found = false, Error = "Item não encontrado." };

        // Já resolvido antes: nem toca na loja. Sai sem capa de propósito — abrir
        // a página só por imagem é exatamente a requisição que o §3.2 evita; quem
        // precisa da capa tem `queries.image_url` como primeira parada.
        if (!string.IsNullOrWhiteSpace(item.Isbn))
            return new ResolveIsbnResultDto
            {
                Found = true,
                Isbn = item.Isbn,
                Title = item.Name,
                Author = item.Authors,
            };

        if (string.IsNullOrWhiteSpace(item.Href))
            return new ResolveIsbnResultDto { Found = false, Error = "Item sem link para a loja." };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, item.Href);
            request.Headers.Add("User-Agent", ChromeUserAgent);
            request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            request.Headers.Add("Accept-Language", "pt-BR,pt;q=0.9");

            using var response = await HttpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            var match = IsbnPattern.Match(html);

            if (!match.Success)
                return new ResolveIsbnResultDto { Found = false, Error = "ISBN não encontrado na página." };

            var isbn = new string(match.Groups[1].Value.Where(char.IsDigit).ToArray());
            if (isbn.Length is not (10 or 13))
                return new ResolveIsbnResultDto { Found = false, Error = "ISBN em formato inesperado." };

            var updated = await _catalogRepository.SetIsbnForTitleAsync(
                item.NameNormalized, isbn, cancellationToken);

            _logger.LogInformation(
                "ISBN {Isbn} resolvido para \"{Title}\" ({Count} lojas atualizadas)",
                isbn, item.Name, updated);

            return new ResolveIsbnResultDto
            {
                Found = true,
                Isbn = isbn,
                Title = item.Name,
                Author = ExtrairAutor(html) ?? item.Authors,
                Publisher = ExtrairEditora(html),
                ImageUrl = ExtrairCapa(html, item.Href),
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao resolver ISBN do item {Id}", catalogItemId);
            return new ResolveIsbnResultDto
            {
                Found = false,
                Error = "Não foi possível abrir a página do produto.",
            };
        }
    }

    internal static string? ExtrairAutor(string html) => ExtrairTexto(AuthorPattern, html);

    internal static string? ExtrairEditora(string html) => ExtrairTexto(PublisherPattern, html);

    /// <summary>Primeiro grupo não vazio do padrão, já limpo de espaços e &amp;nbsp;.</summary>
    private static string? ExtrairTexto(Regex padrao, string html)
    {
        var match = padrao.Match(html);
        if (!match.Success) return null;

        var valor = match.Groups
            .Cast<Group>()
            .Skip(1)
            .FirstOrDefault(g => g.Success && !string.IsNullOrWhiteSpace(g.Value))
            ?.Value;

        if (valor == null) return null;

        valor = System.Net.WebUtility.HtmlDecode(valor).Replace('\u00a0', ' ').Trim();
        return string.IsNullOrWhiteSpace(valor) ? null : valor;
    }

    /// <summary>
    /// Capa do produto. A og:image dessas lojas às vezes vem relativa; resolver
    /// contra o endereço da página evita guardar no banco uma URL que não abre.
    /// </summary>
    internal static string? ExtrairCapa(string html, string? paginaUrl)
    {
        var bruto = ExtrairTexto(OgImagePattern, html);
        if (bruto == null) return null;

        // O esquema precisa ser conferido: no Linux, `Uri.TryCreate` aceita
        // "/image/capa.jpg" como URI absoluta e devolve "file:///image/capa.jpg".
        // Sem esta linha, toda capa de caminho relativo iria para o banco como um
        // link para o sistema de arquivos do servidor.
        if (Uri.TryCreate(bruto, UriKind.Absolute, out var absoluta)
            && (absoluta.Scheme == Uri.UriSchemeHttp || absoluta.Scheme == Uri.UriSchemeHttps))
            return absoluta.ToString();

        if (paginaUrl != null
            && Uri.TryCreate(paginaUrl, UriKind.Absolute, out var basePagina)
            && Uri.TryCreate(basePagina, bruto, out var combinada))
            return combinada.ToString();

        return null;
    }
}
