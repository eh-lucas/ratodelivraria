using Sherlock.Business.DTOs;

namespace Sherlock.Business.Interfaces;

public interface IWatchedBookService
{
    Task<IReadOnlyList<WatchedBookDto>> ListAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Histórico de preço dos livros da pessoa, por ISBN. Uma chamada para a lista
    /// inteira: o gráfico é por cartão, mas buscar um por vez seriam 30 requisições
    /// para desenhar uma tela.
    /// </summary>
    Task<IReadOnlyDictionary<string, IReadOnlyList<PricePointDto>>> HistoryAsync(
        int userId, int days, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona em lote. Pedir de novo um livro que já está na lista não dá erro —
    /// vira edição da regra, que é o que a pessoa quis dizer ao repetir o pedido.
    /// </summary>
    Task<WatchedBookBatchResult> AddAsync(
        int userId, WatchedBookBatchRequest request, CancellationToken cancellationToken = default);

    /// <summary>Devolve null se o id não existe ou não é de quem pediu.</summary>
    Task<WatchedBookDto?> UpdateAsync(
        int userId, int id, WatchedBookPatchRequest request, CancellationToken cancellationToken = default);

    Task<bool> RemoveAsync(int userId, int id, CancellationToken cancellationToken = default);
}
