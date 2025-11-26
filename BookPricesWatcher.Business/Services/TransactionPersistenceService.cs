using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sherlock.Business.Core.Scrapers;
using Sherlock.Business.Interfaces;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Interfaces;

namespace Sherlock.Business.Services;

/// <summary>
/// Serviço responsável por persistir transações e queries no banco de dados.
/// </summary>
public class TransactionPersistenceService : ITransactionPersistenceService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IQueryRepository _queryRepository;
    private readonly ILogger<TransactionPersistenceService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public TransactionPersistenceService(
        ITransactionRepository transactionRepository,
        IQueryRepository queryRepository,
        ILogger<TransactionPersistenceService> logger)
    {
        _transactionRepository = transactionRepository;
        _queryRepository = queryRepository;
        _logger = logger;
    }

    public async Task<Transaction> PersistAsync(
        SearchResult searchResult,
        SearchParameter searchParameter,
        int userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Persistindo transação para usuário {UserId} com {QueryCount} queries",
            userId, searchResult.AllQueryResults.Count);

        try
        {
            // 1. Criar e salvar Transaction
            var transaction = CreateTransaction(searchResult, searchParameter, userId);
            transaction = await _transactionRepository.CreateTransactionAsync(transaction);

            _logger.LogDebug("Transaction {TransactionId} criada", transaction.Id);

            // 2. Converter e salvar Queries
            if (searchResult.AllQueryResults.Count > 0)
            {
                var isbn = searchParameter.Isbn;
                var queries = searchResult.AllQueryResults
                    .Select(qr => CreateQuery(qr, transaction.Id, isbn))
                    .ToList();

                await _queryRepository.AddQueriesAsync(queries);

                _logger.LogDebug(
                    "{QueryCount} queries persistidas para transaction {TransactionId}",
                    queries.Count, transaction.Id);

                // 3. Encontrar e atualizar BestQuery
                var bestQuery = await _queryRepository.GetBestQueryForTransactionAsync(transaction.Id);
                if (bestQuery != null)
                {
                    await _transactionRepository.UpdateBestQueryAsync(transaction.Id, bestQuery.Id);
                    transaction.BestQueryId = bestQuery.Id;

                    _logger.LogDebug(
                        "BestQuery definida: {BestQueryId} (Provider: {ProviderId}, Preço: {Price})",
                        bestQuery.Id, bestQuery.ProviderId, bestQuery.Price);
                }
            }

            _logger.LogInformation(
                "Transação {TransactionId} persistida com sucesso: {SuccessfulQueries}/{TotalQueries} queries bem-sucedidas",
                transaction.Id, transaction.SuccessfulQueries, transaction.TotalProvidersQueried);

            return transaction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Erro ao persistir transação para usuário {UserId}: {Message}",
                userId, ex.Message);
            throw;
        }
    }

    private Transaction CreateTransaction(
        SearchResult searchResult,
        SearchParameter searchParameter,
        int userId)
    {
        return new Transaction
        {
            UserId = userId,
            ResultTypeId = GetResultTypeId(searchResult.ResultadoTransacao),
            StartedAt = searchResult.InicioConsulta.ToUniversalTime(),
            EndedAt = searchResult.FimConsulta.ToUniversalTime(),
            ExecutionTimeMs = searchResult.TempoDecorrido,
            TotalProvidersQueried = searchResult.TotalSourcesQueried,
            SuccessfulQueries = searchResult.SuccessfulQueries,
            FailedQueries = searchResult.FailedQueries,
            CostCredits = searchResult.CustoCreditos,
            InputParameters = SerializeInputParameters(searchParameter),
            Errors = SerializeErrors(searchResult)
        };
    }

    private Query CreateQuery(QueryResult queryResult, int transactionId, string? searchIsbn)
    {
        return queryResult.ToEntity(transactionId, searchIsbn);
    }

    private static int GetResultTypeId(TransactionResult resultType)
    {
        // IDs assumidos baseados na seed do banco
        // 1 = Success, 2 = PartialSuccess, 3 = NoResults, 4 = AllFailed
        return resultType.Name switch
        {
            "Success" => 1,
            "PartialSuccess" => 2,
            "NoResults" => 3,
            "AllFailed" => 4,
            _ => 3 // Default para NoResults
        };
    }

    private static string SerializeInputParameters(SearchParameter searchParameter)
    {
        var parameters = new
        {
            bookTitle = searchParameter.BookTitle,
            isbn = searchParameter.Isbn,
            author = searchParameter.AuthorName,
            isExactSearch = searchParameter.IsExactSearch
        };

        return JsonSerializer.Serialize(parameters, JsonOptions);
    }

    private static string? SerializeErrors(SearchResult searchResult)
    {
        if (searchResult.Errors.Count == 0 && searchResult.ErrorSummary == null)
            return null;

        var errorData = new
        {
            messages = searchResult.Errors,
            summary = searchResult.ErrorSummary != null ? new
            {
                total = searchResult.ErrorSummary.TotalErrors,
                timeout = searchResult.ErrorSummary.TimeoutCount,
                network = searchResult.ErrorSummary.NetworkCount,
                httpError = searchResult.ErrorSummary.HttpErrorCount,
                parseError = searchResult.ErrorSummary.ParseErrorCount,
                blocked = searchResult.ErrorSummary.BlockedCount,
                unknown = searchResult.ErrorSummary.UnknownCount,
                mostCommon = searchResult.ErrorSummary.MostCommonErrorType?.ToString()
            } : null
        };

        return JsonSerializer.Serialize(errorData, JsonOptions);
    }
}
