using Microsoft.Extensions.Logging;
using Polly;
using Sherlock.Business.Core.Scrapers;
using Sherlock.Business.Interfaces;

namespace Sherlock.Business.Core.Resilience;

/// <summary>
/// Wrapper que adiciona resiliência (retry, circuit breaker) e cache aos scrapers
/// </summary>
public class ResilientScraperWrapper
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<ResilientScraperWrapper> _logger;
    private readonly Dictionary<string, ResiliencePipeline<BookPriceResult?>> _pipelines = new();
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(2);

    public ResilientScraperWrapper(
        ICacheService cacheService,
        ILogger<ResilientScraperWrapper> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Executa busca com resiliência e cache
    /// </summary>
    public async Task<BookPriceResult?> ExecuteWithResilienceAsync(
        IScraper scraper,
        SearchParameter parameters,
        CancellationToken cancellationToken = default)
    {
        var scraperName = scraper.GetType().Name;
        var providerId = parameters.Source?.Id ?? 0;

        // Tenta buscar do cache primeiro
        var cacheKey = _cacheService.GenerateProviderKey(parameters.BookTitle, providerId);
        var cachedResult = await _cacheService.GetAsync<BookPriceResult>(cacheKey);

        if (cachedResult != null)
        {
            _logger.LogInformation(
                "Resultado em cache para {BookTitle} no provider {ProviderId}",
                parameters.BookTitle, providerId);
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

            // Salva no cache se teve resultado válido
            if (result != null && !string.IsNullOrEmpty(result.Title) && result.Price > 0)
            {
                await _cacheService.SetAsync(cacheKey, result, CacheDuration);

                _logger.LogDebug(
                    "Resultado cacheado para {BookTitle} no provider {ProviderId} por {Duration}",
                    parameters.BookTitle, providerId, CacheDuration);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Falha ao executar scraper {ScraperName} para {BookTitle}",
                scraperName, parameters.BookTitle);
            return null;
        }
    }

    private ResiliencePipeline<BookPriceResult?> GetOrCreatePipeline(string scraperName)
    {
        if (!_pipelines.TryGetValue(scraperName, out var pipeline))
        {
            pipeline = ResiliencePolicies.CreateScraperPipeline<BookPriceResult?>(
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
