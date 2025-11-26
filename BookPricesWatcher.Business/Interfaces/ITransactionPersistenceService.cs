using Sherlock.Business.Core.Scrapers;
using Sherlock.Domain.Entities;

namespace Sherlock.Business.Interfaces;

/// <summary>
/// Serviço responsável por persistir transações e queries no banco de dados.
/// </summary>
public interface ITransactionPersistenceService
{
    /// <summary>
    /// Persiste uma transação completa com todas as queries no banco de dados.
    /// </summary>
    /// <param name="searchResult">Resultado da busca do W16Engine</param>
    /// <param name="searchParameter">Parâmetros usados na busca</param>
    /// <param name="userId">ID do usuário que realizou a busca</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>A transação persistida com Id preenchido</returns>
    Task<Transaction> PersistAsync(
        SearchResult searchResult,
        SearchParameter searchParameter,
        int userId,
        CancellationToken cancellationToken = default);
}
