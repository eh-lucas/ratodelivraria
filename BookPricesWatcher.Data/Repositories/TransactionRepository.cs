using Microsoft.EntityFrameworkCore;
using Sherlock.Data.Context;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Interfaces;

namespace Sherlock.Data.Repositories;

public class TransactionRepository : RepositoryBase<Transaction>, ITransactionRepository
{
    public TransactionRepository(SherlockDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Transaction>> GetByUserIdAsync(int userId, int limit = 20)
    {
        return await _dbSet
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.StartedAt)
            .Take(limit)
            .Include(t => t.Queries)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetRecentAsync(int count = 10)
    {
        return await _dbSet
            .OrderByDescending(t => t.StartedAt)
            .Take(count)
            .Include(t => t.Queries)
            .ToListAsync();
    }

    public async Task<Transaction?> GetWithQueriesAsync(int transactionId)
    {
        return await _dbSet
            .Include(t => t.Queries)
                .ThenInclude(q => q.Provider)
            .Include(t => t.BestQuery)
            .FirstOrDefaultAsync(t => t.Id == transactionId);
    }

    public async Task<Transaction> CreateTransactionAsync(Transaction transaction)
    {
        transaction.StartedAt = DateTime.UtcNow;
        return await AddAsync(transaction);
    }

    public async Task UpdateBestQueryAsync(int transactionId, int bestQueryId)
    {
        var transaction = await GetByIdAsync(transactionId);
        if (transaction != null)
        {
            transaction.BestQueryId = bestQueryId;
            await UpdateAsync(transaction);
        }
    }
}
