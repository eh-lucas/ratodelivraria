using Sherlock.Business.Core.Scrapers;
using Sherlock.Business.Interfaces;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Interfaces;
using System.Text.Json;

namespace Sherlock.Business.Services;

public class QueryHistoryService : IQueryHistoryService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IQueryRepository _queryRepository;

    public QueryHistoryService(
        ITransactionRepository transactionRepository,
        IQueryRepository queryRepository)
    {
        _transactionRepository = transactionRepository;
        _queryRepository = queryRepository;
    }

    public async Task<Transaction> LogTransactionAsync(
        SearchResult result,
        string inputParameters,
        List<QueryResultInfo> queryResults,
        int? userId = null,
        int? bookId = null)
    {
        // Cria a transação
        var transaction = new Transaction
        {
            UserId = userId,
            BookId = bookId,
            StartedAt = result.InicioConsulta,
            EndedAt = result.FimConsulta,
            ExecutionTimeMs = result.TempoDecorrido,
            TotalProvidersQueried = result.TotalSourcesQueried,
            SuccessfulQueries = result.SuccessfulQueries,
            FailedQueries = result.FailedQueries,
            CostCredits = result.CustoCreditos,
            ResultTypeId = result.ResultadoTransacao.Id,
            InputParameters = inputParameters,
            FromCache = result.FromCache,
            Errors = result.Errors.Count > 0 ? JsonSerializer.Serialize(result.Errors) : null
        };

        // Salva a transação primeiro para obter o ID
        var savedTransaction = await _transactionRepository.CreateTransactionAsync(transaction);

        // Cria as queries individuais
        var queries = queryResults.Select(qr => new Query
        {
            TransactionId = savedTransaction.Id,
            ProviderId = qr.ProviderId,
            ResponseTimeMs = qr.ResponseTimeMs,
            Success = qr.Success,
            Title = qr.Title,
            Author = qr.Author,
            Price = qr.Price,
            Discount = qr.Discount,
            ProductUrl = qr.ProductUrl,
            ErrorMessage = qr.ErrorMessage
        }).ToList();

        // Salva todas as queries
        if (queries.Count > 0)
        {
            await _queryRepository.AddQueriesAsync(queries);

            // Identifica a melhor query (menor preço com sucesso)
            var bestQuery = await _queryRepository.GetBestQueryForTransactionAsync(savedTransaction.Id);
            if (bestQuery != null)
            {
                await _transactionRepository.UpdateBestQueryAsync(savedTransaction.Id, bestQuery.Id);
            }
        }

        return savedTransaction;
    }

    public async Task<IEnumerable<TransactionHistoryDto>> GetUserHistoryAsync(int userId, int limit = 20)
    {
        var transactions = await _transactionRepository.GetByUserIdAsync(userId, limit);
        return transactions.Select(MapToHistoryDto);
    }

    public async Task<IEnumerable<TransactionHistoryDto>> GetRecentTransactionsAsync(int limit = 10)
    {
        var transactions = await _transactionRepository.GetRecentAsync(limit);
        return transactions.Select(MapToHistoryDto);
    }

    public async Task<TransactionDetailDto?> GetTransactionDetailAsync(int transactionId)
    {
        var transaction = await _transactionRepository.GetWithQueriesAsync(transactionId);
        if (transaction == null)
            return null;

        return MapToDetailDto(transaction);
    }

    private static TransactionHistoryDto MapToHistoryDto(Transaction transaction)
    {
        var bestQuery = transaction.Queries
            .Where(q => q.Success && q.Price > 0)
            .OrderBy(q => q.Price)
            .FirstOrDefault();

        return new TransactionHistoryDto
        {
            Id = transaction.Id,
            StartedAt = transaction.StartedAt,
            EndedAt = transaction.EndedAt,
            ExecutionTimeMs = transaction.ExecutionTimeMs,
            TotalProvidersQueried = transaction.TotalProvidersQueried,
            SuccessfulQueries = transaction.SuccessfulQueries,
            CostCredits = transaction.CostCredits,
            InputParameters = transaction.InputParameters,
            IsSuccess = transaction.ResultTypeId == 1 || transaction.ResultTypeId == 2, // Success ou PartialSuccess
            FromCache = transaction.FromCache,
            BestTitle = bestQuery?.Title,
            BestPrice = bestQuery?.Price,
            BestProvider = bestQuery?.Provider?.Name
        };
    }

    private static TransactionDetailDto MapToDetailDto(Transaction transaction)
    {
        var dto = new TransactionDetailDto
        {
            Id = transaction.Id,
            StartedAt = transaction.StartedAt,
            EndedAt = transaction.EndedAt,
            ExecutionTimeMs = transaction.ExecutionTimeMs,
            TotalProvidersQueried = transaction.TotalProvidersQueried,
            SuccessfulQueries = transaction.SuccessfulQueries,
            CostCredits = transaction.CostCredits,
            InputParameters = transaction.InputParameters,
            IsSuccess = transaction.ResultTypeId == 1 || transaction.ResultTypeId == 2,
            FromCache = transaction.FromCache,
            Queries = transaction.Queries.Select(q => new QueryDetailDto
            {
                Id = q.Id,
                ProviderId = q.ProviderId,
                ProviderName = q.Provider?.Name ?? $"Provider #{q.ProviderId}",
                ResponseTimeMs = q.ResponseTimeMs,
                Success = q.Success,
                Title = q.Title,
                Author = q.Author,
                Price = q.Price,
                Discount = q.Discount,
                ErrorMessage = q.ErrorMessage
            }).OrderBy(q => q.Price ?? decimal.MaxValue).ToList()
        };

        var bestQuery = dto.Queries.FirstOrDefault(q => q.Success && q.Price > 0);
        dto.BestTitle = bestQuery?.Title;
        dto.BestPrice = bestQuery?.Price;
        dto.BestProvider = bestQuery?.ProviderName;

        return dto;
    }
}
