using Sherlock.Domain.Entities;

namespace Sherlock.Domain.Interfaces;

public interface IWatchedBookRepository : IRepository<WatchedBook>
{
    /// <summary>A lista de uma pessoa, do mais recente para o mais antigo.</summary>
    Task<IReadOnlyList<WatchedBook>> GetByUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<WatchedBook?> GetByUserAndIsbnAsync(string isbn, int userId, CancellationToken cancellationToken = default);

    /// <summary>Quantos a pessoa já tem — é contra isto que o teto de 20 é medido.</summary>
    Task<int> CountByUserAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Os ISBNs distintos que o vigia precisa varrer. É esta consulta que faz a
    /// feature escalar: o custo é o número de livros diferentes, não o número de
    /// pessoas vezes livros (docs/PLANO-OBSERVADOS.md §4.1).
    /// </summary>
    Task<IReadOnlyList<string>> GetDistinctActiveIsbnsAsync(CancellationToken cancellationToken = default);

    /// <summary>Todas as linhas ativas de um ISBN — uma por pessoa que o observa.</summary>
    Task<IReadOnlyList<WatchedBook>> GetActiveByIsbnAsync(string isbn, CancellationToken cancellationToken = default);

    /// <summary>Tudo que está ativo, com o dono junto. Base da rodada de avisos.</summary>
    Task<IReadOnlyList<WatchedBook>> GetAllActiveWithUserAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Menor preço de cada ISBN nos últimos N dias, lido de `queries`. Uma consulta
    /// para a lista inteira — fazer uma por cartão seria 20 idas ao banco para
    /// desenhar uma tela.
    /// </summary>
    Task<IReadOnlyDictionary<string, decimal>> GetLowestPricesAsync(
        IEnumerable<string> isbns, int days, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
