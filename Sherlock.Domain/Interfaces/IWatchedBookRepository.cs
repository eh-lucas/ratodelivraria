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
    /// <param name="ateOntem">
    /// Corta o dia de hoje do conjunto.
    ///
    /// Para EXIBIR o "menor preço do mês" no cartão, hoje tem de entrar: se o preço
    /// de hoje é o mais barato, é ele que a pessoa precisa ver.
    ///
    /// Para DECIDIR o aviso, não. A varredura das 03h grava os preços de hoje nesta
    /// mesma tabela, e o aviso lê às 09h — então o mínimo já incluía o preço de
    /// hoje e a regra comparava o preço consigo mesmo. Com isso `menor_30d ≤ hoje`
    /// era sempre verdade, a porta da queda acumulada (§3.3a) nunca podia abrir, e
    /// o guarda-corpo da porta (b) virava `hoje ≤ hoje`. Medido em 01/11/2026 num
    /// livro real: hoje 64,95 e "menor dos 30 dias" 64,95, idênticos.
    /// </param>
    Task<IReadOnlyDictionary<string, decimal>> GetLowestPricesAsync(
        IEnumerable<string> isbns, int days, bool ateOntem = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Menor preço por dia de cada ISBN, para o gráfico do cartão. Uma consulta
    /// para a lista inteira, pelo mesmo motivo do <see cref="GetLowestPricesAsync"/>.
    ///
    /// O menor do dia, e não a média: é o preço que a pessoa pagaria, e é o mesmo
    /// número que a regra de aviso usa — dois critérios diferentes fariam o
    /// gráfico contradizer o e-mail.
    /// </summary>
    Task<IReadOnlyDictionary<string, IReadOnlyList<PricePoint>>> GetPriceHistoryAsync(
        IEnumerable<string> isbns, int days, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>Um ponto da série do gráfico: o dia e o menor preço daquele dia.</summary>
public readonly record struct PricePoint(DateOnly Day, decimal Price);
