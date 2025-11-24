using Sherlock.Business.DTOs;

namespace Sherlock.Business.Interfaces;

public interface ICartOptimizationService
{
    /// <summary>
    /// Otimiza o carrinho de compras buscando os melhores preços em múltiplos providers
    /// </summary>
    Task<CartOptimizationResult> OptimizeCartAsync(
        CartOptimizationRequest request,
        int? userId = null,
        CancellationToken cancellationToken = default);
}
