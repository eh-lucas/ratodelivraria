using Microsoft.Extensions.Logging;
using Polly;
using Sherlock.Business.Core.Scrapers;
using Sherlock.Business.Interfaces;
using Sherlock.Domain.Entities;
using System.Diagnostics;

namespace Sherlock.Business.Core.Resilience;

/// <summary>
/// Wrapper que adiciona resiliência (retry, circuit breaker) e cache aos scrapers
/// </summary>
public class ResilientScraperWrapper
{
    private readonly ICacheService _cacheService;
    private readonly IQueryHistoryService _queryHistoryService;
    private readonly ILogger<ResilientScraperWrapper> _logger;
    private readonly Dictionary<string, ResiliencePipeline<QueryResult>> _pipelines = new();
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(2);

    public ResilientScraperWrapper(
        ICacheService cacheService,
        IQueryHistoryService queryHistoryService,
        ILogger<ResilientScraperWrapper> logger)
    {
        _cacheService = cacheService;
        _queryHistoryService = queryHistoryService;
        _logger = logger;
    }

    /// <summary>
    /// Executa busca com resiliência e cache
    /// </summary>
    public async Task<QueryResult> ExecuteWithResilienceAsync(
        IScraper scraper,
        SearchParameter parameters,
        CancellationToken cancellationToken = default)
    {
        var scraperName = scraper.GetType().Name;
        var provider = parameters.Source ?? new Provider { Id = 0, Name = "Unknown", Url = string.Empty };
        var stopwatch = Stopwatch.StartNew();

        // Tenta buscar do cache primeiro
        var cacheKey = _cacheService.GenerateProviderKey(parameters.BookTitle, provider.Id);
        var cachedResult = await _cacheService.GetAsync<QueryResult>(cacheKey);

        if (cachedResult != null)
        {
            _logger.LogInformation(
                "Resultado em cache para {BookTitle} no provider {ProviderId}",
                parameters.BookTitle, provider.Id);
            return cachedResult;
        }

        // Obtém ou cria pipeline de resiliência para este scraper
        var pipeline = GetOrCreatePipeline(scraperName);

        try
        {
            var result = await pipeline.ExecuteAsync(async ct =>
            {
                return await scraper.ExecuteSearch(parameters);
            }, cancellationToken);

            stopwatch.Stop();

            // Salva no cache se teve resultado válido
            if (result.HasValidResult)
            {
                await _cacheService.SetAsync(cacheKey, result, CacheDuration);

                _logger.LogDebug(
                    "Resultado cacheado para {BookTitle} no provider {ProviderId} por {Duration}",
                    parameters.BookTitle, provider.Id, CacheDuration);
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "Falha ao executar scraper {ScraperName} para {BookTitle}",
                scraperName, parameters.BookTitle);

            return QueryResult.CreateFailure(
                provider,
                QueryErrorType.Unknown,
                ex.Message,
                stopwatch.ElapsedMilliseconds);
        }
    }

    private ResiliencePipeline<QueryResult> GetOrCreatePipeline(string scraperName)
    {
        if (!_pipelines.TryGetValue(scraperName, out var pipeline))
        {
            pipeline = ResiliencePolicies.CreateScraperPipeline<QueryResult>(
                _logger,
                scraperName,
                maxRetries: 2,
                circuitBreakerThreshold: 3,
                timeoutSeconds: 30);

            _pipelines[scraperName] = pipeline;
        }

        return pipeline;
    }
}
