using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace Sherlock.Business.Core.Resilience;

/// <summary>
/// Políticas de resiliência para scrapers usando Polly v8
/// </summary>
public static class ResiliencePolicies
{
    /// <summary>
    /// Cria pipeline de resiliência para scrapers com retry, circuit breaker e timeout
    /// </summary>
    public static ResiliencePipeline<T> CreateScraperPipeline<T>(
        ILogger logger,
        string scraperName,
        int maxRetries = 2,
        int circuitBreakerThreshold = 3,
        int timeoutSeconds = 30)
    {
        return new ResiliencePipelineBuilder<T>()
            // Timeout por requisição
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(timeoutSeconds),
                OnTimeout = args =>
                {
                    logger.LogWarning("Timeout no scraper {ScraperName} após {Timeout}s",
                        scraperName, timeoutSeconds);
                    return default;
                }
            })
            // Retry com backoff exponencial
            .AddRetry(new RetryStrategyOptions<T>
            {
                MaxRetryAttempts = maxRetries,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<T>()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .Handle<TimeoutRejectedException>(),
                OnRetry = args =>
                {
                    logger.LogWarning(
                        "Retry {AttemptNumber}/{MaxRetries} no scraper {ScraperName}. Erro: {Error}",
                        args.AttemptNumber, maxRetries, scraperName, args.Outcome.Exception?.Message);
                    return default;
                }
            })
            // Circuit breaker
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<T>
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = circuitBreakerThreshold,
                BreakDuration = TimeSpan.FromMinutes(1),
                ShouldHandle = new PredicateBuilder<T>()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .Handle<TimeoutRejectedException>(),
                OnOpened = args =>
                {
                    logger.LogError(
                        "Circuit breaker ABERTO para {ScraperName}. Duração: {BreakDuration}",
                        scraperName, args.BreakDuration);
                    return default;
                },
                OnClosed = args =>
                {
                    logger.LogInformation("Circuit breaker FECHADO para {ScraperName}", scraperName);
                    return default;
                },
                OnHalfOpened = args =>
                {
                    logger.LogInformation("Circuit breaker HALF-OPEN para {ScraperName}", scraperName);
                    return default;
                }
            })
            .Build();
    }

    /// <summary>
    /// Cria pipeline simplificado apenas com retry
    /// </summary>
    public static ResiliencePipeline<T> CreateRetryOnlyPipeline<T>(
        ILogger logger,
        string operationName,
        int maxRetries = 3)
    {
        return new ResiliencePipelineBuilder<T>()
            .AddRetry(new RetryStrategyOptions<T>
            {
                MaxRetryAttempts = maxRetries,
                Delay = TimeSpan.FromMilliseconds(500),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    logger.LogDebug(
                        "Retry {AttemptNumber} para {OperationName}",
                        args.AttemptNumber, operationName);
                    return default;
                }
            })
            .Build();
    }
}
