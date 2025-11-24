using Sherlock.Business.Core.Scrapers;

namespace Sherlock.Business.Interfaces;

public interface IQueryHistoryService
{
    Task LogQueryAsync(SearchResult result, string inputParameters, int? userId = null, int? bookId = null);
    Task<IEnumerable<QueryHistoryDto>> GetUserHistoryAsync(int userId, int limit = 20);
    Task<IEnumerable<QueryHistoryDto>> GetRecentQueriesAsync(int limit = 10);
}

public class QueryHistoryDto
{
    public int Id { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
    public long ExecutionTimeMs { get; set; }
    public int ProvidersQueried { get; set; }
    public int SuccessfulQueries { get; set; }
    public int CostCredits { get; set; }
    public string InputParameters { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
}
