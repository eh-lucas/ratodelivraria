using Sherlock.Business.DTOs;
using Sherlock.Business.Interfaces;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Interfaces;

namespace Sherlock.Business.Services;

public class BookPriceService : IBookPriceService
{
    private readonly IBookPriceRepository _bookPriceRepository;
    private readonly IProviderRepository _providerRepository;

    public BookPriceService(IBookPriceRepository bookPriceRepository, IProviderRepository providerRepository)
    {
        _bookPriceRepository = bookPriceRepository;
        _providerRepository = providerRepository;
    }

    public async Task<IEnumerable<BookPriceDto>> GetByBookIdAsync(int bookId)
    {
        var prices = await _bookPriceRepository.GetByBookIdAsync(bookId);
        var providers = (await _providerRepository.GetAllAsync()).ToDictionary(p => p.Id);

        return prices.Select(p => MapToDto(p, providers.GetValueOrDefault(p.ProviderId)));
    }

    public async Task<BookPriceDto?> GetLatestPriceAsync(int bookId, int providerId)
    {
        var price = await _bookPriceRepository.GetLatestPriceAsync(bookId, providerId);
        if (price == null) return null;

        var provider = await _providerRepository.GetByIdAsync(providerId);
        return MapToDto(price, provider);
    }

    public async Task<IEnumerable<BookPriceDto>> GetPriceHistoryAsync(int bookId, DateTime from, DateTime to)
    {
        var prices = await _bookPriceRepository.GetPriceHistoryAsync(bookId, from, to);
        var providers = (await _providerRepository.GetAllAsync()).ToDictionary(p => p.Id);

        return prices.Select(p => MapToDto(p, providers.GetValueOrDefault(p.ProviderId)));
    }

    public async Task SavePricesAsync(int bookId, IEnumerable<BookPriceDto> prices)
    {
        var entities = prices.Select(p => new BookPrice
        {
            BookId = bookId,
            ProviderId = p.ProviderId,
            Price = p.Price,
            Discount = p.Discount,
            QueryDateTime = DateTime.UtcNow
        });

        await _bookPriceRepository.SavePricesAsync(entities);
    }

    private static BookPriceDto MapToDto(BookPrice price, Provider? provider)
    {
        return new BookPriceDto
        {
            Id = price.Id,
            BookId = price.BookId,
            ProviderId = price.ProviderId,
            ProviderName = provider?.Name ?? "Unknown",
            ProviderUrl = provider?.Url ?? string.Empty,
            Price = price.Price,
            Discount = price.Discount,
            QueryDateTime = price.QueryDateTime
        };
    }
}
