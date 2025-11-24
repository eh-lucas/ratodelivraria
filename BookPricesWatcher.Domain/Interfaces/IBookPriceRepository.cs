using Sherlock.Domain.Entities;

namespace Sherlock.Domain.Interfaces;

public interface IBookPriceRepository : IRepository<BookPrice>
{
    Task<IEnumerable<BookPrice>> GetByBookIdAsync(int bookId);
    Task<IEnumerable<BookPrice>> GetByProviderIdAsync(int providerId);
    Task<BookPrice?> GetLatestPriceAsync(int bookId, int providerId);
    Task<IEnumerable<BookPrice>> GetPriceHistoryAsync(int bookId, DateTime from, DateTime to);
    Task SavePricesAsync(IEnumerable<BookPrice> prices);
}
