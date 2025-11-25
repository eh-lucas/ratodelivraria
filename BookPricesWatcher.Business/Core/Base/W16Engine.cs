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
        var transactionId = Guid.NewGuid().ToString("N")[..8];

        _logger.LogInformation(
            "========== TRANSAÇÃO {TransactionId} INICIADA ==========\n" +
            "  Livro: \"{BookTitle}\"\n" +
            "  Providers: {SourceCount}\n" +
            "  Paralelismo: {Parallelism}",
            transactionId,
            requestor.SearchParameters.BookTitle,
            requestor.SourcesToSearch.Count,
            MaxDegreeOfParallelism);

        var preResults = new ConcurrentBag<BookPriceResult>();
        var result = new SearchResult
        {
            InicioConsulta = DateTime.Now,
            TotalSourcesQueried = requestor.SourcesToSearch.Count
        };

        // Métricas de paralelismo
        var metrics = new ParallelismMetrics();

        try
        {
            // Verifica cache agregado primeiro (resultado completo da busca)
            if (_cacheService != null)
            {
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
            }

            // Agrupa sources por categoria para usar o scraper correto
            var sourcesByCategory = requestor.SourcesToSearch
                .GroupBy(s => s.ProviderCategoryEnum)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var (category, sources) in sourcesByCategory)
            {
                var scraper = _scraperFactory.CreateScraper(category);
                if (scraper == null)
                {
                    _logger.LogWarning("Scraper não encontrado para categoria {Category}", category);
                    Interlocked.Add(ref metrics.FailedCount, sources.Count);
                    continue;
                }

                // Executa buscas em paralelo com limite de concorrência
                var semaphore = new SemaphoreSlim(MaxDegreeOfParallelism);
                var tasks = new List<Task>();

                foreach (var source in sources)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    await semaphore.WaitAsync(cancellationToken);

                    tasks.Add(Task.Run(async () =>
                    {
                        var sourceStopwatch = Stopwatch.StartNew();
                        Interlocked.Increment(ref metrics.ActiveTasks);
                        var currentActive = metrics.ActiveTasks;

                        // Atualiza pico de tarefas simultâneas
                        lock (metrics)
                        {
                            if (currentActive > metrics.PeakActiveTasks)
                                metrics.PeakActiveTasks = currentActive;
                        }

                        try
                        {
                            var parameters = new SearchParameter
                            {
                                BookTitle = requestor.SearchParameters.BookTitle,
                                Isbn = requestor.SearchParameters.Isbn,
                                AuthorName = requestor.SearchParameters.AuthorName,
                                IsExactSearch = requestor.SearchParameters.IsExactSearch,
                                Source = source
                            };

                            BookPriceResult? singleResult;

                            // Usa wrapper resiliente se disponível
                            if (_resilientWrapper != null)
                            {
                                singleResult = await _resilientWrapper.ExecuteWithResilienceAsync(
                                    scraper, parameters, cancellationToken);
                            }
                            else
                            {
                                singleResult = await scraper.ExecuteSearch(parameters);
                            }

                            sourceStopwatch.Stop();

                            // Registra tempo de resposta
                            lock (metrics.ResponseTimes)
                            {
                                metrics.ResponseTimes.Add(new ProviderResponseTime
                                {
                                    ProviderName = source.Name,
                                    ElapsedMs = sourceStopwatch.ElapsedMilliseconds,
                                    Success = singleResult != null && !string.IsNullOrEmpty(singleResult.Title) && singleResult.Price > 0
                                });
                            }

                            // Só adiciona se teve resultado válido
                            if (singleResult != null && !string.IsNullOrEmpty(singleResult.Title) && singleResult.Price > 0)
                            {
                                preResults.Add(singleResult);
                                Interlocked.Increment(ref metrics.SuccessCount);
                            }
                            else
                            {
                                Interlocked.Increment(ref metrics.NoResultCount);
                            }
                        }
                        catch (Exception ex)
                        {
                            sourceStopwatch.Stop();
                            Interlocked.Increment(ref metrics.FailedCount);

                            lock (metrics.ResponseTimes)
                            {
                                metrics.ResponseTimes.Add(new ProviderResponseTime
                                {
                                    ProviderName = source.Name,
                                    ElapsedMs = sourceStopwatch.ElapsedMilliseconds,
                                    Success = false,
                                    Error = ex.Message
                                });
                            }

                            _logger.LogWarning("[{Provider}] Falha: {Message}", source.Name, ex.Message);
                        }
                        finally
                        {
                            Interlocked.Decrement(ref metrics.ActiveTasks);
                            semaphore.Release();
                        }
                    }, cancellationToken));
                }

                // Aguarda todas as tarefas completarem
                await Task.WhenAll(tasks);
            }

            stopwatch.Stop();

            // Atualiza resultado
            result.SuccessfulQueries = metrics.SuccessCount;
            result.FailedQueries = metrics.FailedCount;

            // Compara e seleciona o melhor resultado
            var resultList = preResults.ToList();
            result.BookPriceResult = _comparator.Compare(resultList);
            result.AllResults = resultList;

            // Define o status da transação
            result.ResultadoTransacao = DetermineResultType(result, resultList.Count);

            // Calcula custo baseado em queries bem-sucedidas
            result.CustoCreditos = CalculateCost(result);

            // Log detalhado de métricas de paralelismo
            LogParallelismMetrics(transactionId, metrics, stopwatch.ElapsedMilliseconds, result);

            // Cacheia resultado agregado se teve sucesso
            if (_cacheService != null && result.ResultadoTransacao.IsSuccess)
            {
                var cacheKey = _cacheService.GenerateBookPriceKey(
                    requestor.SearchParameters.BookTitle,
                    requestor.SearchParameters.Isbn);

                await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromHours(2));
            }
        }
        catch (Exception ex)
        {
            result.ResultadoTransacao = ResultType.AllFailed;
            result.Errors.Add($"Erro fatal: {ex.Message}");

            _logger.LogError(ex, "Erro fatal na transação {TransactionId}", transactionId);
        }
        finally
        {
            stopwatch.Stop();
            result.TempoDecorrido = stopwatch.ElapsedMilliseconds;
            result.FimConsulta = DateTime.Now;
        }

        return result;
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
    public string ProviderName { get; set; } = "";
    public long ElapsedMs { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}
