using Microsoft.EntityFrameworkCore;
using Sherlock.Data.Context;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Interfaces;

namespace Sherlock.Data.Repositories;

public class TransactionRepository : RepositoryBase<Transaction>, ITransactionRepository
{
    private readonly IDbContextFactory<SherlockDbContext> _contextFactory;

    public TransactionRepository(
        SherlockDbContext context,
        IDbContextFactory<SherlockDbContext> contextFactory) : base(context)
    {
        _contextFactory = contextFactory;
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

    /// <summary>
    /// Cria uma transação usando DbContext separado para permitir operações concorrentes.
    /// Este método é thread-safe.
    /// </summary>
    public async Task<Transaction> CreateTransactionAsync(Transaction transaction)
    {
        transaction.StartedAt = DateTime.UtcNow;

        // Usa factory para criar DbContext separado, permitindo chamadas concorrentes
        await using var context = await _contextFactory.CreateDbContextAsync();
        await context.Set<Transaction>().AddAsync(transaction);
        await context.SaveChangesAsync();

        return transaction;
    }

    /// <summary>
    /// Atualiza a melhor query de uma transação usando DbContext separado.
    /// Este método é thread-safe.
    /// </summary>
    public async Task UpdateBestQueryAsync(int transactionId, int bestQueryId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var transaction = await context.Set<Transaction>().FindAsync(transactionId);
        if (transaction != null)
        {
            transaction.BestQueryId = bestQueryId;
            await context.SaveChangesAsync();
        }
    }
}
