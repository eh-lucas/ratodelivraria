using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Sherlock.Business.Core.Resilience;
using Sherlock.Business.Core.Scrapers;
using Sherlock.Business.Interfaces;
using Sherlock.Domain.Entities;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Sherlock.Business.Core.Base;

/// <summary>
/// Motor principal de execução de transações de busca de preços.
/// Orquestra scrapers, cache, comparação e registro de resultados.
/// </summary>
public class W16Engine
{
    private readonly Comparator _comparator;
    private readonly ScraperFactory _scraperFactory;
    private readonly ILogger<W16Engine> _logger;
    private readonly ResilientScraperWrapper? _resilientWrapper;
    private readonly ICacheService? _cacheService;

    /// <summary>
    /// Nível de paralelismo para buscas. Ajuste este valor para encontrar o melhor desempenho.
    /// Valores sugeridos para teste: 5, 10, 15, 20, 30
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = 10;

    public W16Engine() : this(NullLogger<W16Engine>.Instance, null, null)
    {
    }

    public W16Engine(ILogger<W16Engine> logger) : this(logger, null, null)
    {
    }

    public W16Engine(
        ILogger<W16Engine> logger,
        ICacheService? cacheService,
        ResilientScraperWrapper? resilientWrapper)
    {
        _comparator = new Comparator();
        _scraperFactory = new ScraperFactory();
        _logger = logger;
        _cacheService = cacheService;
        _resilientWrapper = resilientWrapper;
    }

    public async Task<SearchResult> ExecuteTransaction(Requestor requestor, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var transactionId = GenerateTransactionId();

        LogTransactionStart(transactionId, requestor);

        var queryResults = new ConcurrentBag<QueryResult>();
        var result = CreateInitialSearchResult(requestor);
        var metrics = new ParallelismMetrics();

        try
        {
            var cachedResult = await TryGetFromCacheAsync(requestor, transactionId, stopwatch);
            if (cachedResult != null)
                return cachedResult;

            await ExecuteScrapersAsync(requestor, queryResults, metrics, cancellationToken);

            stopwatch.Stop();

            FinalizeSearchResult(result, queryResults, metrics);
            LogParallelismMetrics(transactionId, metrics, stopwatch.ElapsedMilliseconds, result);
            await TryCacheResultAsync(requestor, result);
        }
        catch (Exception ex)
        {
            HandleFatalError(result, ex, transactionId);
        }
        finally
        {
            FinalizeTransaction(result, stopwatch);
        }

        return result;
    }

    private static string GenerateTransactionId()
    {
        return Guid.NewGuid().ToString("N")[..8];
    }

    private void LogTransactionStart(string transactionId, Requestor requestor)
    {
        _logger.LogInformation(
            "========== TRANSAÇÃO {TransactionId} INICIADA ==========\n" +
            "  Livro: \"{BookTitle}\"\n" +
            "  Providers: {SourceCount}\n" +
            "  Paralelismo: {Parallelism}",
            transactionId,
            requestor.SearchParameters.BookTitle,
            requestor.SourcesToSearch.Count,
            MaxDegreeOfParallelism);
    }

    private static SearchResult CreateInitialSearchResult(Requestor requestor)
    {
        return new SearchResult
        {
            InicioConsulta = DateTime.Now,
            TotalSourcesQueried = requestor.SourcesToSearch.Count
        };
    }

    private async Task<SearchResult?> TryGetFromCacheAsync(Requestor requestor, string transactionId, Stopwatch stopwatch)
    {
        if (_cacheService == null)
            return null;

        var cacheKey = _cacheService.GenerateBookPriceKey(
            requestor.SearchParameters.BookTitle,
            requestor.SearchParameters.Isbn);

        var cachedResult = await _cacheService.GetAsync<SearchResult>(cacheKey);
        if (cachedResult != null)
        {
            _logger.LogInformation(
                "Transação {TransactionId} resolvida via cache em {Elapsed}ms",
                transactionId, stopwatch.ElapsedMilliseconds);

            cachedResult.FromCache = true;
            return cachedResult;
        }

        return null;
    }

    private async Task ExecuteScrapersAsync(
        Requestor requestor,
        ConcurrentBag<QueryResult> queryResults,
        ParallelismMetrics metrics,
        CancellationToken cancellationToken)
    {
        var sourcesByCategory = GroupSourcesByCategory(requestor.SourcesToSearch);

        foreach (var (category, sources) in sourcesByCategory)
        {
            var scraper = _scraperFactory.CreateScraper(category);
            if (scraper == null)
            {
                _logger.LogWarning("Scraper não encontrado para categoria {Category}", category);
                Interlocked.Add(ref metrics.FailedCount, sources.Count);
                continue;
            }

            await ExecuteCategoryScrapingAsync(scraper, sources, requestor.SearchParameters, queryResults, metrics, cancellationToken);
        }
    }

    private static Dictionary<ProviderCategoryEnum, List<Provider>> GroupSourcesByCategory(IEnumerable<Provider> sources)
    {
        return sources
            .GroupBy(s => s.ProviderCategoryEnum)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private async Task ExecuteCategoryScrapingAsync(
        IScraper scraper,
        List<Provider> sources,
        SearchParameter baseParameters,
        ConcurrentBag<QueryResult> queryResults,
        ParallelismMetrics metrics,
        CancellationToken cancellationToken)
    {
        var semaphore = new SemaphoreSlim(MaxDegreeOfParallelism);
        var tasks = new List<Task>();

        foreach (var source in sources)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await semaphore.WaitAsync(cancellationToken);

            tasks.Add(ExecuteSingleScrapingAsync(scraper, source, baseParameters, queryResults, metrics, semaphore, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    private Task ExecuteSingleScrapingAsync(
        IScraper scraper,
        Provider source,
        SearchParameter baseParameters,
        ConcurrentBag<QueryResult> queryResults,
        ParallelismMetrics metrics,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            TrackActiveTask(metrics, true);

            try
            {
                var parameters = CreateSearchParameter(baseParameters, source);
                var queryResult = await ExecuteScraperAsync(scraper, parameters, cancellationToken);

                queryResults.Add(queryResult);
                RecordQueryResult(metrics, queryResult);
            }
            catch (Exception ex)
            {
                var errorResult = QueryResult.CreateFailure(source, QueryErrorType.Unknown, ex.Message, 0);
                queryResults.Add(errorResult);
                HandleScraperError(metrics, source.Name, 0, ex);
            }
            finally
            {
                TrackActiveTask(metrics, false);
                semaphore.Release();
            }
        }, cancellationToken);
    }

    private static void TrackActiveTask(ParallelismMetrics metrics, bool increment)
    {
        if (increment)
        {
            Interlocked.Increment(ref metrics.ActiveTasks);
            var currentActive = metrics.ActiveTasks;

            lock (metrics)
            {
                if (currentActive > metrics.PeakActiveTasks)
                    metrics.PeakActiveTasks = currentActive;
            }
        }
        else
        {
            Interlocked.Decrement(ref metrics.ActiveTasks);
        }
    }

    private static SearchParameter CreateSearchParameter(SearchParameter baseParameters, Provider source)
    {
        return new SearchParameter
        {
            BookTitle = baseParameters.BookTitle,
            Isbn = baseParameters.Isbn,
            AuthorName = baseParameters.AuthorName,
            IsExactSearch = baseParameters.IsExactSearch,
            Source = source
        };
    }

    private async Task<QueryResult> ExecuteScraperAsync(IScraper scraper, SearchParameter parameters, CancellationToken cancellationToken)
    {
        if (_resilientWrapper != null)
        {
            return await _resilientWrapper.ExecuteWithResilienceAsync(scraper, parameters, cancellationToken);
        }

        return await scraper.ExecuteSearch(parameters);
    }

    private void RecordQueryResult(ParallelismMetrics metrics, QueryResult queryResult)
    {
        lock (metrics.ResponseTimes)
        {
            metrics.ResponseTimes.Add(new ProviderResponseTime
            {
                ProviderName = queryResult.ProviderName,
                ElapsedMs = queryResult.ResponseTimeMs,
                Success = queryResult.HasValidResult,
                Error = queryResult.ErrorMessage
            });
        }

        if (queryResult.HasValidResult)
        {
            Interlocked.Increment(ref metrics.SuccessCount);
        }
        else if (!queryResult.Success)
        {
            Interlocked.Increment(ref metrics.FailedCount);
        }
        else
        {
            Interlocked.Increment(ref metrics.NoResultCount);
        }
    }

    private void HandleScraperError(ParallelismMetrics metrics, string providerName, long elapsedMs, Exception ex)
    {
        Interlocked.Increment(ref metrics.FailedCount);

        lock (metrics.ResponseTimes)
        {
            metrics.ResponseTimes.Add(new ProviderResponseTime
            {
                ProviderName = providerName,
                ElapsedMs = elapsedMs,
                Success = false,
                Error = ex.Message
            });
        }

        _logger.LogWarning("[{Provider}] Falha: {Message}", providerName, ex.Message);
    }

    private void FinalizeSearchResult(SearchResult result, ConcurrentBag<QueryResult> queryResults, ParallelismMetrics metrics)
    {
        result.SuccessfulQueries = metrics.SuccessCount;
        result.FailedQueries = metrics.FailedCount;

        // Converte QueryResults para BookPriceResults para manter compatibilidade
        var validResults = queryResults
            .Where(q => q.HasValidResult)
            .Select(q => new BookPriceResult
            {
                Title = q.Title ?? string.Empty,
                Author = q.Author ?? string.Empty,
                Price = q.Price,
                Discount = q.Discount,
                Website = q.ProviderName
            })
            .ToList();

        result.BookPriceResult = _comparator.Compare(validResults);
        result.AllResults = validResults;

        // Armazena os QueryResults completos para acesso aos dados de erro
        result.AllQueryResults = queryResults.ToList();

        result.ResultadoTransacao = DetermineResultType(result, validResults.Count);
        result.CustoCreditos = CalculateCost(result);
    }

    private async Task TryCacheResultAsync(Requestor requestor, SearchResult result)
    {
        if (_cacheService == null || !result.ResultadoTransacao.IsSuccess)
            return;

        var cacheKey = _cacheService.GenerateBookPriceKey(
            requestor.SearchParameters.BookTitle,
            requestor.SearchParameters.Isbn);

        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromHours(2));
    }

    private void HandleFatalError(SearchResult result, Exception ex, string transactionId)
    {
        result.ResultadoTransacao = ResultType.AllFailed;
        result.Errors.Add($"Erro fatal: {ex.Message}");
        _logger.LogError(ex, "Erro fatal na transação {TransactionId}", transactionId);
    }

    private static void FinalizeTransaction(SearchResult result, Stopwatch stopwatch)
    {
        stopwatch.Stop();
        result.TempoDecorrido = stopwatch.ElapsedMilliseconds;
        result.FimConsulta = DateTime.Now;
    }

    public async Task PersistQueryInDatabase()
    {

    }
    private void LogParallelismMetrics(string transactionId, ParallelismMetrics metrics, long totalElapsedMs, SearchResult result)
    {
        var responseTimes = metrics.ResponseTimes.OrderBy(r => r.ElapsedMs).ToList();

        // Estatísticas de tempo de resposta
        var successfulTimes = responseTimes.Where(r => r.Success).Select(r => r.ElapsedMs).ToList();
        var avgTime = successfulTimes.Count > 0 ? successfulTimes.Average() : 0;
        var minTime = successfulTimes.Count > 0 ? successfulTimes.Min() : 0;
        var maxTime = successfulTimes.Count > 0 ? successfulTimes.Max() : 0;
        var p50Time = successfulTimes.Count > 0 ? Percentile(successfulTimes, 50) : 0;
        var p95Time = successfulTimes.Count > 0 ? Percentile(successfulTimes, 95) : 0;

        // Top 5 mais rápidos
        var fastest = responseTimes.Where(r => r.Success).Take(5).ToList();

        // Top 5 mais lentos
        var slowest = responseTimes.Where(r => r.Success).TakeLast(5).Reverse().ToList();

        // Providers com falha
        var failed = responseTimes.Where(r => !r.Success).ToList();

        _logger.LogInformation
        (
            "\n========== MÉTRICAS DE PARALELISMO - TRANSAÇÃO {TransactionId} ==========\n" +
            "  CONFIGURAÇÃO:\n" +
            "    MaxParallelism: {MaxParallelism}\n" +
            "    Pico de tasks simultâneas: {PeakTasks}\n" +
            "\n" +
            "  RESULTADOS:\n" +
            "    Total providers: {TotalProviders}\n" +
            "    Com resultado: {SuccessCount}\n" +
            "    Sem resultado: {NoResultCount}\n" +
            "    Falhas: {FailedCount}\n" +
            "    Melhor preço: R${BestPrice:F2} ({BestProvider})\n" +
            "\n" +
            "  TEMPO DE RESPOSTA (providers com resultado):\n" +
            "    Mínimo: {MinTime}ms\n" +
            "    Máximo: {MaxTime}ms\n" +
            "    Médio: {AvgTime:F0}ms\n" +
            "    P50: {P50Time}ms\n" +
            "    P95: {P95Time}ms\n" +
            "\n" +
            "  TEMPO TOTAL: {TotalTime}ms\n" +
            "  THROUGHPUT: {Throughput:F1} providers/segundo\n" +
            "===========================================================",
            transactionId,
            MaxDegreeOfParallelism,
            metrics.PeakActiveTasks,
            metrics.ResponseTimes.Count,
            metrics.SuccessCount,
            metrics.NoResultCount,
            metrics.FailedCount,
            result.BookPriceResult?.Price ?? 0,
            result.BookPriceResult?.Website ?? "N/A",
            minTime,
            maxTime,
            avgTime,
            p50Time,
            p95Time,
            totalElapsedMs,
            metrics.ResponseTimes.Count > 0 ? (metrics.ResponseTimes.Count * 1000.0 / totalElapsedMs) : 0);

        // Log dos mais rápidos
        if (fastest.Any())
        {
            _logger.LogInformation(
                "  TOP 5 MAIS RÁPIDOS:\n{FastestList}",
                string.Join("\n", fastest.Select((r, i) => $"    {i + 1}. {r.ProviderName}: {r.ElapsedMs}ms")));
        }

        // Log dos mais lentos
        if (slowest.Any())
        {
            _logger.LogInformation(
                "  TOP 5 MAIS LENTOS:\n{SlowestList}",
                string.Join("\n", slowest.Select((r, i) => $"    {i + 1}. {r.ProviderName}: {r.ElapsedMs}ms")));
        }

        // Log das falhas
        if (failed.Any())
        {
            _logger.LogWarning(
                "  PROVIDERS COM FALHA ({FailCount}):\n{FailedList}",
                failed.Count,
                string.Join("\n", failed.Select(r => $"    - {r.ProviderName}: {r.Error ?? "Timeout/Sem resposta"} ({r.ElapsedMs}ms)")));
        }
    }

    private static long Percentile(List<long> sortedValues, int percentile)
    {
        if (sortedValues.Count == 0) return 0;

        var index = (int)Math.Ceiling((percentile / 100.0) * sortedValues.Count) - 1;
        index = Math.Max(0, Math.Min(index, sortedValues.Count - 1));
        return sortedValues[index];
    }

    private static ResultType DetermineResultType(SearchResult result, int validResults)
    {
        if (result.FailedQueries == result.TotalSourcesQueried)
            return ResultType.AllFailed;

        if (validResults == 0)
            return ResultType.NoResults;

        if (result.FailedQueries > 0)
            return ResultType.PartialSuccess;

        return ResultType.Success;
    }

    private static int CalculateCost(SearchResult result)
    {
        // Custo base por transação + custo por query bem-sucedida
        const int baseCost = 1;
        const int costPerSuccessfulQuery = 1;

        if (!result.ResultadoTransacao.IsBillable)
            return 0;

        return baseCost + (result.SuccessfulQueries * costPerSuccessfulQuery);
    }
}

/// <summary>
/// Métricas de execução paralela
/// </summary>
internal class ParallelismMetrics
{
    public int SuccessCount;
    public int NoResultCount;
    public int FailedCount;
    public int ActiveTasks;
    public int PeakActiveTasks;
    public List<ProviderResponseTime> ResponseTimes { get; } = new();
}

/// <summary>
/// Tempo de resposta de um provider
/// </summary>
internal class ProviderResponseTime
{
    public string ProviderName { get; set; } = string.Empty;
    public long ElapsedMs { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}
