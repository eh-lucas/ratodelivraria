using Sherlock.Business.DTOs;

namespace Sherlock.Business.Interfaces;

public interface ICartOptimizationService
{
    /// <summary>
    /// Otimiza o carrinho de compras buscando os melhores preços em múltiplos providers
    /// </summary>
    /// <param name="request">Dados do carrinho (livros, providers)</param>
    /// <param name="userId">ID do usuário que está realizando a busca</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resultado da otimização com distribuição de compras</returns>
    Task<CartOptimizationResult> OptimizeCartAsync(
        CartOptimizationRequest request,
        int userId,
        CancellationToken cancellationToken = default);
}
