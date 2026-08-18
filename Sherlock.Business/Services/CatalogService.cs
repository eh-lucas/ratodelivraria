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

        // Já resolvido antes: nem toca na loja.
        if (!string.IsNullOrWhiteSpace(item.Isbn))
            return new ResolveIsbnResultDto { Found = true, Isbn = item.Isbn, Title = item.Name };

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

            return new ResolveIsbnResultDto { Found = true, Isbn = isbn, Title = item.Name };
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
}
