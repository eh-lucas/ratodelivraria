using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sherlock.Business.Configuration;
using Sherlock.Business.Core.Exceptions;
using Sherlock.Business.Core.Resilience;
using Sherlock.Business.Core.Scrapers;
using Sherlock.Business.Interfaces;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

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
    private readonly ITransactionPersistenceService? _persistenceService;
    private readonly IQueryRepository? _queryRepository;
    private readonly QueryCacheSettings _cacheSettings;

    /// <summary>
    /// Nível de paralelismo para buscas. Ajuste este valor para encontrar o melhor desempenho.
    /// Valores sugeridos para teste: 5, 10, 15, 20, 30
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = 10;

    public W16Engine() : this(NullLoggerFactory.Instance, NullLogger<W16Engine>.Instance, null, null, null, null, null)
    {
    }

    public W16Engine(ILogger<W16Engine> logger) : this(NullLoggerFactory.Instance, logger, null, null, null, null, null)
    {
    }

    public W16Engine(
        ILoggerFactory loggerFactory,
        ILogger<W16Engine> logger,
        ICacheService? cacheService,
        ResilientScraperWrapper? resilientWrapper,
        ITransactionPersistenceService? persistenceService,
        IQueryRepository? queryRepository,
        IOptions<QueryCacheSettings>? cacheSettings)
    {
        _comparator = new Comparator();
        _scraperFactory = new ScraperFactory(loggerFactory);
        _logger = logger;
        _cacheService = cacheService;
        _resilientWrapper = resilientWrapper;
        _persistenceService = persistenceService;
        _queryRepository = queryRepository;
        _cacheSettings = cacheSettings?.Value ?? new QueryCacheSettings();
    }

    /// <summary>
    /// Executa uma transação de busca de preços.
    /// </summary>
    /// <param name="requestor">Dados da requisição (parâmetros de busca e providers)</param>
    /// <param name="userId">ID do usuário que está realizando a busca</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resultado da busca com preços e métricas</returns>
    public async Task<SearchResult> ExecuteTransaction(Requestor requestor, int userId, CancellationToken cancellationToken = default)
    {
        // Inicia contador
        var stopwatch = Stopwatch.StartNew();

        // Cria transaction ID
        var transactionId = GenerateTransactionId();
        LogTransactionStart(transactionId, requestor);

        // Cria resultado inicial
        var queryResults = new ConcurrentBag<QueryResult>();
        var result = CreateInitialSearchResult(requestor);
        var metrics = new ParallelismMetrics();

        try
        {
            // Executa buscas
            await ExecuteSearch(requestor, metrics, queryResults, cancellationToken);

            FinalizeSearchResult(result, queryResults, metrics);
            LogParallelismMetrics(transactionId, metrics, stopwatch.ElapsedMilliseconds, result);

            stopwatch.Stop();

        }
        catch (Exception ex)
        {
            HandleFatalError(result, ex, transactionId);
        }
        finally
        {
            FinalizeTransaction(result, stopwatch);
        }

        // Persistir transação e queries no banco
        await PersistTransactionAsync(result, requestor.SearchParameters, userId, cancellationToken);

        return result;
    }

    public async Task ExecuteSearch(Requestor requestor, ParallelismMetrics metrics,
        ConcurrentBag<QueryResult> queryResults, CancellationToken cancellationToken = default)
    {
        var parallel = false;

        try
        {
            // Prepara os providers a serem consultados por categoria
            var sourcesByCategory = GroupSourcesByCategory(requestor.SourcesToSearch);

            foreach (var (category, sources) in sourcesByCategory)
            {
                // Cria scraper para a categoria
                var scraper = _scraperFactory.CreateScraper(category);
                if (scraper == null)
                {
                    _logger.LogWarning("Scraper não encontrado para categoria {Category}", category);
                    Interlocked.Add(ref metrics.FailedCount, sources.Count);
                    continue;
                }

                if (parallel)
                    await ExecuteParallely(requestor, queryResults, sources, scraper, metrics, cancellationToken);
                else
                    await ExecuteNonConcurrently(requestor, queryResults, sources, scraper, metrics, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar buscas");
            throw;
        }
    }

    private async Task<bool> GetCacheFromDatabase(string isbn, ConcurrentBag<QueryResult> queryResults, Provider source,
        int cacheTimeMinutes, ParallelismMetrics metrics)
    {
        var cachedQuery = await _queryRepository!.GetCachedQueryAsync(isbn!, source.Id, cacheTimeMinutes);
        if (cachedQuery != null)
        {
            _logger.LogInformation(
                "Cache hit (DB): ISBN {Isbn} no provider {ProviderId} ({ProviderName})",
                isbn, source.Id, source.Name);

            var cachedResult = ConvertQueryToQueryResult(cachedQuery, source);
            queryResults.Add(cachedResult);
            Interlocked.Increment(ref metrics.SuccessCount);
            return true;
        }

        return false;
    }

    private async Task<bool> GetCacheFromRedis(Requestor requestor, ConcurrentBag<QueryResult> queryResults, Provider source)
    {
        var cacheKey = _cacheService.GenerateBookProviderKey(requestor.SearchParameters.BookTitle, source.Id);
        var cachedResult = await _cacheService.GetAsync<QueryResult>(cacheKey);

        if (cachedResult != null)
        {
            _logger.LogInformation("Cache hit (Redis): {BookTitle} no provider {ProviderId}",
                requestor.SearchParameters.BookTitle, source.Id);
            queryResults.Add(cachedResult);
            return true;
        }

        return false;
    }

    private async Task ExecuteParallely(Requestor requestor, ConcurrentBag<QueryResult> queryResults, List<Provider> sources, IScraper scraper,
        ParallelismMetrics metrics, CancellationToken cancellationToken)
    {
        foreach (var source in sources)
        {
            // Determina tempo de cache (usa valor do requestor ou default)
            var cacheTimeMinutes = requestor.CacheTimeMinutes ?? _cacheSettings.DefaultCacheTimeMinutes;
            var isbn = requestor.SearchParameters.Isbn;
            var canUseDbCache = !string.IsNullOrEmpty(isbn) && _queryRepository != null;

            // Verifica cache no banco por ISBN (somente se ISBN foi informado)
            if (canUseDbCache)
            {
                var hasCache = await GetCacheFromDatabase(isbn, queryResults, source, cacheTimeMinutes, metrics);
                if (hasCache) continue;
            }

            // Verifica cache Redis se disponível (fallback para busca por título)
            if (_cacheService != null)
            {
                var hasCache = await GetCacheFromRedis(requestor, queryResults, source);
                if (hasCache) continue;
            }

            var semaphore = new SemaphoreSlim(MaxDegreeOfParallelism);
            var tasks = new List<Task>();

            if (cancellationToken.IsCancellationRequested)
                return;

            await semaphore.WaitAsync(cancellationToken);

            // Se chegou ate aqui eh porque nao encontrou cache - decide se executa em paralelo ou nao
            tasks.Add(ExecuteSingleScrapingAsync(scraper, source, requestor.SearchParameters, queryResults,
                metrics, semaphore, cancellationToken));

            await Task.WhenAll(tasks);
        }
    }


    private async Task ExecuteNonConcurrently(Requestor requestor, ConcurrentBag<QueryResult> queryResults, List<Provider> sources, IScraper scraper,
        ParallelismMetrics metrics, CancellationToken cancellationToken)
    {
        foreach (var source in sources)
        {
            // Determina tempo de cache (usa valor do requestor ou default)
            var cacheTimeMinutes = requestor.CacheTimeMinutes ?? _cacheSettings.DefaultCacheTimeMinutes;
            var isbn = requestor.SearchParameters.Isbn;
            var canUseDbCache = !string.IsNullOrEmpty(isbn) && _queryRepository != null;

            // Verifica cache no banco por ISBN (somente se ISBN foi informado)
            if (canUseDbCache)
            {
                var hasCache = await GetCacheFromDatabase(isbn, queryResults, source, cacheTimeMinutes, metrics);
                if (hasCache) continue;
            }

            // Verifica cache Redis se disponível (fallback para busca por título)
            if (_cacheService != null)
            {
                var hasCache = await GetCacheFromRedis(requestor, queryResults, source);
                if (hasCache) continue;
            }

            await ExecuteSingleScrapingAsync(scraper, source, requestor.SearchParameters, queryResults,
                metrics, cancellationToken);

        }
    }

    private async Task PersistTransactionAsync(
        SearchResult result,
        SearchParameter searchParameter,
        int userId,
        CancellationToken cancellationToken)
    {
        if (_persistenceService == null)
        {
            _logger.LogWarning("Serviço de persistência não configurado - transação não será salva");
            return;
        }

        try
        {
            await _persistenceService.PersistAsync(result, searchParameter, userId, cancellationToken);
        }
        catch (Exception ex)
        {
            // Log do erro mas não falha a busca - persistência é secundária
            _logger.LogError(ex, "Erro ao persistir transação - a busca foi realizada mas não foi salva no banco");
        }
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

    private static Dictionary<ProviderCategoryEnum, List<Provider>> GroupSourcesByCategory(IEnumerable<Provider> sources)
    {
        return sources
            .GroupBy(s => s.ProviderCategoryEnum)
            .ToDictionary(g => g.Key, g => g.ToList());
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
            var stopwatch = Stopwatch.StartNew();
            TrackActiveTask(metrics, true);

            try
            {
                var parameters = CreateSearchParameter(baseParameters, source);
                var queryResult = await scraper.ExecuteSearch(parameters);

                queryResults.Add(queryResult);
                RecordQueryResult(metrics, queryResult);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                var errorResult = QueryResult.CreateFailure(source, QueryErrorType.Timeout, "Operação cancelada pelo usuário", stopwatch.ElapsedMilliseconds);
                queryResults.Add(errorResult);
                RecordErrorResult(metrics, source.Name, stopwatch.ElapsedMilliseconds, QueryErrorType.Timeout, "Cancelado");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var (errorType, errorMessage, httpStatusCode) = ClassifyException(ex, source.Name);
                var errorResult = QueryResult.CreateFailure(source, errorType, errorMessage, stopwatch.ElapsedMilliseconds, httpStatusCode);
                queryResults.Add(errorResult);
                RecordErrorResult(metrics, source.Name, stopwatch.ElapsedMilliseconds, errorType, errorMessage, ex);
            }
            finally
            {
                TrackActiveTask(metrics, false);
                semaphore.Release();
            }
        }, cancellationToken);
    }

    private async Task ExecuteSingleScrapingAsync(
        IScraper scraper,
        Provider source,
        SearchParameter baseParameters,
        ConcurrentBag<QueryResult> queryResults,
        ParallelismMetrics metrics,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        TrackActiveTask(metrics, true);

        try
        {
            var parameters = CreateSearchParameter(baseParameters, source);
            var queryResult = await scraper.ExecuteSearch(parameters);

            queryResults.Add(queryResult);
            RecordQueryResult(metrics, queryResult);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            var errorResult = QueryResult.CreateFailure(source, QueryErrorType.Timeout, "Operação cancelada pelo usuário", stopwatch.ElapsedMilliseconds);
            queryResults.Add(errorResult);
            RecordErrorResult(metrics, source.Name, stopwatch.ElapsedMilliseconds, QueryErrorType.Timeout, "Cancelado");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var (errorType, errorMessage, httpStatusCode) = ClassifyException(ex, source.Name);
            var errorResult = QueryResult.CreateFailure(source, errorType, errorMessage, stopwatch.ElapsedMilliseconds, httpStatusCode);
            queryResults.Add(errorResult);
            RecordErrorResult(metrics, source.Name, stopwatch.ElapsedMilliseconds, errorType, errorMessage, ex);
        }
        finally
        {
            TrackActiveTask(metrics, false);
        }
    }

    private (QueryErrorType errorType, string message, int? httpStatusCode) ClassifyException(Exception ex, string providerName)
    {
        return ex switch
        {
            // Exceções customizadas do scraper
            ScraperTimeoutException ste => (QueryErrorType.Timeout, ste.Message, null),
            ScraperNetworkException sne => (QueryErrorType.Network, sne.Message, null),
            ScraperHttpException she => (QueryErrorType.HttpError, she.Message, she.StatusCode),
            ScraperParseException spe => (QueryErrorType.ParseError, spe.Message, null),
            ScraperBlockedException sbe => (QueryErrorType.Blocked, sbe.Message, null),
            ScraperRateLimitException sre => (QueryErrorType.Blocked, sre.Message, 429),

            // Timeout genérico
            TaskCanceledException or OperationCanceledException => (QueryErrorType.Timeout, "Request timeout", null),

            // Erros de rede
            HttpRequestException hre => ClassifyHttpRequestException(hre),
            SocketException se => (QueryErrorType.Network, $"Erro de socket: {se.SocketErrorCode}", null),
            WebException we => (QueryErrorType.Network, $"Erro web: {we.Status}", null),

            // Erros de parsing
            InvalidOperationException ioe when ioe.Message.Contains("parse", StringComparison.OrdinalIgnoreCase)
                => (QueryErrorType.ParseError, ioe.Message, null),
            FormatException fe => (QueryErrorType.ParseError, $"Formato inválido: {fe.Message}", null),

            // Erro genérico
            _ => (QueryErrorType.Unknown, TruncateMessage(ex.Message), null)
        };
    }

    private static (QueryErrorType, string, int?) ClassifyHttpRequestException(HttpRequestException hre)
    {
        if (hre.StatusCode.HasValue)
        {
            var statusCode = (int)hre.StatusCode.Value;
            var errorType = statusCode switch
            {
                429 => QueryErrorType.Blocked,
                >= 400 and < 500 => QueryErrorType.HttpError,
                >= 500 => QueryErrorType.HttpError,
                _ => QueryErrorType.Network
            };
            return (errorType, $"HTTP {statusCode}", statusCode);
        }

        // Sem status code - provavelmente erro de conexão
        if (hre.InnerException is SocketException se)
        {
            return (QueryErrorType.Network, $"Conexão falhou: {se.SocketErrorCode}", null);
        }

        return (QueryErrorType.Network, hre.Message, null);
    }

    private static string TruncateMessage(string message, int maxLength = 200)
    {
        if (string.IsNullOrEmpty(message)) return "Erro desconhecido";
        return message.Length <= maxLength ? message : message[..maxLength] + "...";
    }

    private void RecordErrorResult(ParallelismMetrics metrics, string providerName, long elapsedMs, QueryErrorType errorType, string errorMessage, Exception? ex = null)
    {
        Interlocked.Increment(ref metrics.FailedCount);

        lock (metrics.ResponseTimes)
        {
            metrics.ResponseTimes.Add(new ProviderResponseTime
            {
                ProviderName = providerName,
                ElapsedMs = elapsedMs,
                Success = false,
                Error = errorMessage,
                ErrorType = errorType
            });
        }

        // Log estruturado com nível apropriado baseado no tipo de erro
        var logLevel = errorType switch
        {
            QueryErrorType.Timeout => LogLevel.Warning,
            QueryErrorType.Network => LogLevel.Warning,
            QueryErrorType.HttpError => LogLevel.Warning,
            QueryErrorType.Blocked => LogLevel.Information, // Rate limit é esperado às vezes
            QueryErrorType.ParseError => LogLevel.Warning,
            _ => LogLevel.Error
        };

        _logger.Log(logLevel, ex, "[{Provider}] {ErrorType}: {Message} ({ElapsedMs}ms)",
            providerName, errorType, errorMessage, elapsedMs);
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

    /// <summary>
    /// Converte uma Query do banco (cache) para QueryResult
    /// </summary>
    private static QueryResult ConvertQueryToQueryResult(Query query, Provider provider)
    {
        return new QueryResult
        {
            ProviderId = query.ProviderId,
            ProviderName = provider.Name,
            ProviderUrl = provider.Url,
            Success = query.Success,
            Title = query.Title,
            Author = query.Author,
            Price = query.Price ?? 0,
            Discount = (int)(query.Discount ?? 0),
            ProductUrl = query.ProductUrl,
            ResponseTimeMs = 0, // Cache hit - tempo de resposta não aplicável
            QueriedAt = query.QueriedAt,
            FromCache = true
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

    private void HandleFatalError(SearchResult result, Exception ex, string transactionId)
    {
        result.ResultadoTransacao = TransactionResult.AllFailed;
        result.Errors.Add($"Erro fatal: {ex.Message}");
        _logger.LogError(ex, "Erro fatal na transação {TransactionId}", transactionId);
    }

    private static void FinalizeTransaction(SearchResult result, Stopwatch stopwatch)
    {
        stopwatch.Stop();
        result.TempoDecorrido = stopwatch.ElapsedMilliseconds;
        result.FimConsulta = DateTime.Now;
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

        // Log das falhas agrupadas por tipo
        if (failed.Any())
        {
            var errorsByType = failed
                .GroupBy(r => r.ErrorType ?? QueryErrorType.Unknown)
                .OrderByDescending(g => g.Count())
                .ToList();

            _logger.LogWarning(
                "  PROVIDERS COM FALHA ({FailCount}):\n" +
                "    Por tipo:\n{ErrorSummary}\n" +
                "    Detalhes:\n{FailedList}",
                failed.Count,
                string.Join("\n", errorsByType.Select(g => $"      {g.Key}: {g.Count()}")),
                string.Join("\n", failed.Take(10).Select(r => $"    - {r.ProviderName}: [{r.ErrorType}] {r.Error ?? "Sem detalhes"} ({r.ElapsedMs}ms)")));

            // Se houver mais de 10 falhas, indica que há mais
            if (failed.Count > 10)
            {
                _logger.LogWarning("    ... e mais {RemainingCount} falhas", failed.Count - 10);
            }
        }
    }

    private static long Percentile(List<long> sortedValues, int percentile)
    {
        if (sortedValues.Count == 0) return 0;

        var index = (int)Math.Ceiling((percentile / 100.0) * sortedValues.Count) - 1;
        index = Math.Max(0, Math.Min(index, sortedValues.Count - 1));
        return sortedValues[index];
    }

    private static TransactionResult DetermineResultType(SearchResult result, int validResults)
    {
        if (result.FailedQueries == result.TotalSourcesQueried)
            return TransactionResult.AllFailed;

        if (validResults == 0)
            return TransactionResult.NoResults;

        if (result.FailedQueries > 0)
            return TransactionResult.PartialSuccess;

        return TransactionResult.Success;
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
public class ParallelismMetrics
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
public class ProviderResponseTime
{
    public string ProviderName { get; set; } = string.Empty;
    public long ElapsedMs { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public QueryErrorType? ErrorType { get; set; }
}
