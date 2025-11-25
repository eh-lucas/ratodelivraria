using Sherlock.Domain.Entities;

namespace Sherlock.Domain.Interfaces;

public interface IQueryRepository : IRepository<Query>
{
    Task<IEnumerable<Query>> GetByTransactionIdAsync(int transactionId);
    Task<IEnumerable<Query>> GetByProviderIdAsync(int providerId, int limit = 100);
    Task<Query?> GetBestQueryForTransactionAsync(int transactionId);
    Task<Query> AddQueryAsync(Query query);
    Task AddQueriesAsync(IEnumerable<Query> queries);
}
