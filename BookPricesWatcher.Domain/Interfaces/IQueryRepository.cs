using Sherlock.Domain.Entities;

namespace Sherlock.Domain.Interfaces;

public interface IQueryRepository : IRepository<Query>
{
    Task<IEnumerable<Query>> GetByUserTokenAsync(int tokenId);
    Task<IEnumerable<Query>> GetByBookIdAsync(int bookId);
    Task<IEnumerable<Query>> GetRecentAsync(int count = 10);
    Task<Query> LogQueryAsync(Query query);
}
