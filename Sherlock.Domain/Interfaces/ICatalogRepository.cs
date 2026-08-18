using Sherlock.Domain.Entities;

namespace Sherlock.Domain.Interfaces;

public interface ICatalogRepository
{
    /// <summary>Insere os itens novos e atualiza os já conhecidos, por (loja, produto).</summary>
    Task<int> UpsertAsync(IEnumerable<CatalogItem> items, CancellationToken cancellationToken = default);

    /// <summary>Busca por trecho do título normalizado.</summary>
    Task<List<CatalogItem>> SearchByNameAsync(
        string normalizedQuery, int take, CancellationToken cancellationToken = default);

    Task<CatalogItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Grava o ISBN em todas as lojas que vendem o mesmo título.</summary>
    Task<int> SetIsbnForTitleAsync(
        string normalizedName, string isbn, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Todos os product_id já vistos, de qualquer loja.
    ///
    /// O identificador é global da plataforma: o mesmo livro tem o mesmo id em todas as
    /// lojas, então conhecer um produto numa loja significa conhecê-lo em todas.
    /// </summary>
    Task<HashSet<string>> GetKnownProductIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>Data da última coleta de cada loja, para pular as recém-varridas.</summary>
    Task<Dictionary<int, DateTime>> GetLastCrawlByProviderAsync(
        CancellationToken cancellationToken = default);
}
