using Sherlock.Business.DTOs;

namespace Sherlock.Business.Interfaces;

public interface IBookPriceService
{
    Task<IEnumerable<BookPriceDto>> GetByBookIdAsync(int bookId);
    Task<BookPriceDto?> GetLatestPriceAsync(int bookId, int providerId);
    Task<IEnumerable<BookPriceDto>> GetPriceHistoryAsync(int bookId, DateTime from, DateTime to);
    Task SavePricesAsync(int bookId, IEnumerable<BookPriceDto> prices);
}
