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
}
