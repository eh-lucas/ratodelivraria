using Microsoft.EntityFrameworkCore;
using Sherlock.Data.Context;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Interfaces;

namespace Sherlock.Data.Repositories;

/// <summary>
/// Usa <see cref="IDbContextFactory{TContext}"/> porque o crawler grava lojas em
/// paralelo, e o DbContext não é seguro para uso concorrente.
/// </summary>
public class CatalogRepository : ICatalogRepository
{
    private readonly IDbContextFactory<SherlockDbContext> _contextFactory;

    public CatalogRepository(IDbContextFactory<SherlockDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<int> UpsertAsync(
        IEnumerable<CatalogItem> items, CancellationToken cancellationToken = default)
    {
        var affected = 0;

        foreach (var group in items.GroupBy(i => i.ProviderId))
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            // Carrega o que a loja já tem para decidir entre inserir e atualizar.
            var existing = await context.CatalogItems
                .Where(c => c.ProviderId == group.Key)
                .ToDictionaryAsync(c => c.ProductId, cancellationToken);

            foreach (var item in group)
            {
                if (existing.TryGetValue(item.ProductId, out var current))
                {
                    current.Name = item.Name;
                    current.NameNormalized = item.NameNormalized;
                    current.Authors = item.Authors;
                    current.AuthorsNormalized = item.AuthorsNormalized;
                    // Uma coleta sem thumb não apaga a capa que já tínhamos
                    current.ThumbUrl = item.ThumbUrl ?? current.ThumbUrl;
                    current.Price = item.Price;
                    current.Href = item.Href;
                    current.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    context.CatalogItems.Add(item);
                }

                affected++;
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        return affected;
    }

    public async Task<List<CatalogItem>> SearchByNameAsync(
        string normalizedQuery, int take, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // LIKE explícito em vez de Contains: o Npgsql traduz Contains com parâmetro
        // para strpos(), que não usa o índice trigram (seq scan de ~15 ms contra
        // ~0,7 ms com LIKE, medido em 54 mil itens).
        var pattern = "%" + EscapeLike(normalizedQuery) + "%";

        var byTitle = await context.CatalogItems
            .Where(c => EF.Functions.Like(c.NameNormalized, pattern, "\\"))
            .OrderBy(c => c.NameNormalized.Length)
            .Take(take)
            .ToListAsync(cancellationToken);

        // Busca pelo autor em separado e depois do título: sem isso, "machado"
        // traria dezenas de livros do Machado de Assis na frente de títulos que
        // contêm a palavra.
        var byAuthor = await context.CatalogItems
            .Where(c => c.AuthorsNormalized != null
                        && EF.Functions.Like(c.AuthorsNormalized, pattern, "\\")
                        && !EF.Functions.Like(c.NameNormalized, pattern, "\\"))
            .OrderBy(c => c.NameNormalized.Length)
            .Take(take)
            .ToListAsync(cancellationToken);

        byTitle.AddRange(byAuthor);
        return byTitle;
    }

    /// <summary>Escapa os curingas do LIKE para o termo ser buscado literalmente.</summary>
    public static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    public async Task<CatalogItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.CatalogItems.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<CatalogItem?> GetByIsbnAsync(string isbn, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.CatalogItems
            .Where(c => c.Isbn == isbn)
            .OrderByDescending(c => c.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> SetIsbnForTitleAsync(
        string normalizedName, string isbn, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var siblings = await context.CatalogItems
            .Where(c => c.NameNormalized == normalizedName && c.Isbn == null)
            .ToListAsync(cancellationToken);

        foreach (var sibling in siblings)
        {
            sibling.Isbn = isbn;
            sibling.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);
        return siblings.Count;
    }

    public async Task<Dictionary<int, DateTime>> GetLastCrawlByProviderAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.CatalogItems
            .GroupBy(c => c.ProviderId)
            .Select(g => new { ProviderId = g.Key, Last = g.Max(c => c.UpdatedAt) })
            .ToDictionaryAsync(x => x.ProviderId, x => x.Last, cancellationToken);
    }

    public async Task<HashSet<string>> GetKnownProductIdsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var ids = await context.CatalogItems
            .Select(c => c.ProductId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.CatalogItems.CountAsync(cancellationToken);
    }
}
