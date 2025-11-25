using Sherlock.Business.Core.Scrapers;
using Sherlock.Domain.Entities;

namespace Sherlock.Business.Interfaces;

public interface IQueryHistoryService
{
    /// <summary>
    /// Registra uma transação completa com todas as queries individuais
    /// </summary>
    Task<Transaction> LogTransactionAsync(
        SearchResult result,
        string inputParameters,
        List<QueryResultInfo> queryResults,
        int? userId = null,
        int? bookId = null);

    Task<IEnumerable<TransactionHistoryDto>> GetUserHistoryAsync(int userId, int limit = 20);
    Task<IEnumerable<TransactionHistoryDto>> GetRecentTransactionsAsync(int limit = 10);
    Task<TransactionDetailDto?> GetTransactionDetailAsync(int transactionId);
}

/// <summary>
/// Informações de resultado de uma query individual para logging
/// </summary>
public class QueryResultInfo
{
    public int ProviderId { get; set; }
    public long ResponseTimeMs { get; set; }
    public bool Success { get; set; }
    public string? Title { get; set; }
    public string? Author { get; set; }
    public decimal? Price { get; set; }
    public int? Discount { get; set; }
    public string? ProductUrl { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// DTO para histórico de transações (listagem)
/// </summary>
public class TransactionHistoryDto
{
    public int Id { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public long ExecutionTimeMs { get; set; }
    public int TotalProvidersQueried { get; set; }
    public int SuccessfulQueries { get; set; }
    public int CostCredits { get; set; }
    public string InputParameters { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public bool FromCache { get; set; }

    // Melhor resultado
    public string? BestTitle { get; set; }
    public decimal? BestPrice { get; set; }
    public string? BestProvider { get; set; }
}

/// <summary>
/// DTO para detalhes de uma transação específica
/// </summary>
public class TransactionDetailDto : TransactionHistoryDto
{
    public List<QueryDetailDto> Queries { get; set; } = new();
}

/// <summary>
/// DTO para detalhes de uma query individual
/// </summary>
public class QueryDetailDto
{
    public int Id { get; set; }
    public int ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public long ResponseTimeMs { get; set; }
    public bool Success { get; set; }
    public string? Title { get; set; }
    public string? Author { get; set; }
    public decimal? Price { get; set; }
    public int? Discount { get; set; }
    public string? ErrorMessage { get; set; }
}
