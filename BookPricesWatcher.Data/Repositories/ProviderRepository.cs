using Microsoft.EntityFrameworkCore;
using Sherlock.Data.Context;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Interfaces;

namespace Sherlock.Data.Repositories;

public class ProviderRepository : RepositoryBase<Provider>, IProviderRepository
{
    public ProviderRepository(SherlockDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Provider>> GetActivesAsync()
    {
        // Por enquanto retorna todos, futuramente adicionar campo Active
        return await _dbSet.ToListAsync();
    }

    public async Task<IEnumerable<Provider>> GetByCategoryAsync(ProviderCategoryEnum category)
    {
        return await _dbSet
            .Where(p => p.ProviderCategoryEnum == category)
            .ToListAsync();
    }

    public async Task<Provider?> GetByUrlAsync(string url)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.Url == url);
    }

    /// <summary>
    /// Seed inicial de providers a partir da lista estática.
    /// </summary>
    public async Task SeedProvidersAsync()
    {
        if (await _dbSet.AnyAsync())
            return;

        var providers = Provider.AllSources.Select((p, index) => new Provider
        {
            Id = index + 1,
            Name = p.Name ?? ExtractNameFromUrl(p.Url),
            Url = p.Url,
            ProviderCategoryEnum = p.ProviderCategoryEnum
        }).ToList();

        await _dbSet.AddRangeAsync(providers);
        await _context.SaveChangesAsync();
    }

    private static string ExtractNameFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var host = uri.Host.Replace("www.", "");
            var name = host.Split('.')[0];
            return char.ToUpper(name[0]) + name[1..];
        }
        catch
        {
            return url;
        }
    }
}
