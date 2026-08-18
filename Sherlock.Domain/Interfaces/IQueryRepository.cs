using Sherlock.Domain.Entities;

namespace Sherlock.Domain.Interfaces;

public interface IQueryRepository : IRepository<Query>
{
    Task<IEnumerable<Query>> GetByTransactionIdAsync(int transactionId);
    Task<IEnumerable<Query>> GetByProviderIdAsync(int providerId, int limit = 100);
    Task<Query?> GetBestQueryForTransactionAsync(int transactionId);
    Task<Query> AddQueryAsync(Query query);
    Task AddQueriesAsync(IEnumerable<Query> queries);

    /// <summary>
    /// Busca query em cache por ISBN e ProviderId dentro do tempo de cache
    /// </summary>
    /// <param name="isbn">ISBN buscado</param>
    /// <param name="providerId">ID do provider</param>
    /// <param name="cacheTimeMinutes">Tempo de cache em minutos</param>
    /// <returns>Query cacheada ou null se não encontrada/expirada</returns>
    Task<Query?> GetCachedQueryAsync(string isbn, int providerId, int cacheTimeMinutes);

    /// <summary>
    /// Ranking dos ISBNs mais consultados, com o menor preço já visto para cada um.
    /// </summary>
    /// <param name="marco">
    /// Marco de reset: só contam buscas a partir desta data, e todo livro
    /// conhecido entra valendo 1. Null conta o histórico inteiro.
    /// </param>
    Task<IReadOnlyList<PopularBook>> GetMostSearchedAsync(
        int limit, DateTime? marco = null, CancellationToken cancellationToken = default);
}
