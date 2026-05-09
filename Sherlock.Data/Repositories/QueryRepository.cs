using Microsoft.EntityFrameworkCore;
using Sherlock.Data.Context;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Interfaces;

namespace Sherlock.Data.Repositories;

public class QueryRepository : RepositoryBase<Query>, IQueryRepository
{
    private readonly IDbContextFactory<SherlockDbContext> _contextFactory;

    public QueryRepository(
        SherlockDbContext context,
        IDbContextFactory<SherlockDbContext> contextFactory) : base(context)
    {
        _contextFactory = contextFactory;
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

    /// <summary>
    /// Busca a melhor query de uma transação usando DbContext separado.
    /// Este método é thread-safe.
    /// </summary>
    public async Task<Query?> GetBestQueryForTransactionAsync(int transactionId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<Query>()
            .Where(q => q.TransactionId == transactionId && q.Success && q.Price > 0)
            .OrderBy(q => q.Price)
            .FirstOrDefaultAsync();
    }

    public async Task<Query> AddQueryAsync(Query query)
    {
        query.QueriedAt = DateTime.UtcNow;
        return await AddAsync(query);
    }

    /// <summary>
    /// Adiciona múltiplas queries usando DbContext separado para permitir operações concorrentes.
    /// Este método é thread-safe.
    /// </summary>
    public async Task AddQueriesAsync(IEnumerable<Query> queries)
    {
        var now = DateTime.UtcNow;
        var queryList = queries.ToList();
        foreach (var query in queryList)
        {
            query.QueriedAt = now;
        }

        // Usa factory para criar DbContext separado, permitindo chamadas concorrentes
        await using var context = await _contextFactory.CreateDbContextAsync();
        await context.Set<Query>().AddRangeAsync(queryList);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Busca query em cache usando um DbContext separado para permitir operações concorrentes.
    /// Este método é thread-safe e pode ser chamado em paralelo.
    /// </summary>
    public async Task<Query?> GetCachedQueryAsync(string isbn, int providerId, int cacheTimeMinutes)
    {
        var cacheThreshold = DateTime.UtcNow.AddMinutes(-cacheTimeMinutes);

        // Usa factory para criar DbContext separado, permitindo chamadas concorrentes
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Set<Query>()
            .Where(q => q.SearchIsbn == isbn
                && q.ProviderId == providerId
                && q.QueriedAt >= cacheThreshold
                && q.Success)
            .OrderByDescending(q => q.QueriedAt)
            .FirstOrDefaultAsync();
    }
}
