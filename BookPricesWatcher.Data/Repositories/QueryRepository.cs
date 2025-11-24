using Microsoft.EntityFrameworkCore;
using Sherlock.Data.Context;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Interfaces;

namespace Sherlock.Data.Repositories;

public class QueryRepository : RepositoryBase<Query>, IQueryRepository
{
    public QueryRepository(SherlockDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Query>> GetByUserTokenAsync(int userId)
    {
        return await _dbSet
            .Where(q => q.UserId == userId)
            .OrderByDescending(q => q.StartDateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Query>> GetByBookIdAsync(int bookId)
    {
        return await _dbSet
            .Where(q => q.BookId == bookId)
            .OrderByDescending(q => q.StartDateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Query>> GetRecentAsync(int count = 10)
    {
        return await _dbSet
            .OrderByDescending(q => q.StartDateTime)
            .Take(count)
            .ToListAsync();
    }

    public async Task<Query> LogQueryAsync(Query query)
    {
        query.StartDateTime = DateTime.UtcNow;
        return await AddAsync(query);
    }
}
