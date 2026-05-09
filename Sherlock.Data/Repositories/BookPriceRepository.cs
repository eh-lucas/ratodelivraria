using Microsoft.EntityFrameworkCore;
using Sherlock.Data.Context;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Interfaces;

namespace Sherlock.Data.Repositories;

public class BookPriceRepository : RepositoryBase<BookPrice>, IBookPriceRepository
{
    public BookPriceRepository(SherlockDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<BookPrice>> GetByBookIdAsync(int bookId)
    {
        return await _dbSet
            .Where(bp => bp.BookId == bookId)
            .OrderByDescending(bp => bp.QueryDateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<BookPrice>> GetByProviderIdAsync(int providerId)
    {
        return await _dbSet
            .Where(bp => bp.ProviderId == providerId)
            .OrderByDescending(bp => bp.QueryDateTime)
            .ToListAsync();
    }

    public async Task<BookPrice?> GetLatestPriceAsync(int bookId, int providerId)
    {
        return await _dbSet
            .Where(bp => bp.BookId == bookId && bp.ProviderId == providerId)
            .OrderByDescending(bp => bp.QueryDateTime)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<BookPrice>> GetPriceHistoryAsync(int bookId, DateTime from, DateTime to)
    {
        return await _dbSet
            .Where(bp => bp.BookId == bookId &&
                         bp.QueryDateTime >= from &&
                         bp.QueryDateTime <= to)
            .OrderByDescending(bp => bp.QueryDateTime)
            .ToListAsync();
    }

    public async Task SavePricesAsync(IEnumerable<BookPrice> prices)
    {
        await _dbSet.AddRangeAsync(prices);
        await _context.SaveChangesAsync();
    }
}
