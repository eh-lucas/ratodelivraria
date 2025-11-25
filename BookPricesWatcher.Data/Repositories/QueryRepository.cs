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

    public async Task<IEnumerable<Query>> GetByTransactionIdAsync(int transactionId)
    {
        return await _dbSet
            .Where(q => q.TransactionId == transactionId)
            .Include(q => q.Provider)
            .OrderBy(q => q.Price)
            .ToListAsync();
    }

    public async Task<IEnumerable<Query>> GetByProviderIdAsync(int providerId, int limit = 100)
    {
        return await _dbSet
            .Where(q => q.ProviderId == providerId)
            .OrderByDescending(q => q.QueriedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<Query?> GetBestQueryForTransactionAsync(int transactionId)
    {
        return await _dbSet
            .Where(q => q.TransactionId == transactionId && q.Success && q.Price > 0)
            .OrderBy(q => q.Price)
            .FirstOrDefaultAsync();
    }

    public async Task<Query> AddQueryAsync(Query query)
    {
        query.QueriedAt = DateTime.UtcNow;
        return await AddAsync(query);
    }

    public async Task AddQueriesAsync(IEnumerable<Query> queries)
    {
        var now = DateTime.UtcNow;
        foreach (var query in queries)
        {
            query.QueriedAt = now;
        }
        await _dbSet.AddRangeAsync(queries);
        await _context.SaveChangesAsync();
    }
}
