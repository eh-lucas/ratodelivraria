using Sherlock.Domain.Entities;

namespace Sherlock.Domain.Interfaces;

public interface IProviderRepository : IRepository<Provider>
{
    Task<IEnumerable<Provider>> GetActivesAsync();
    Task<IEnumerable<Provider>> GetByCategoryAsync(ProviderCategoryEnum category);
    Task<Provider?> GetByUrlAsync(string url);
}
