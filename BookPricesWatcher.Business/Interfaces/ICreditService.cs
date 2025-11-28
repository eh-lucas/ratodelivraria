using Sherlock.Business.DTOs;

namespace Sherlock.Business.Interfaces;

public interface ICreditService
{
    /// <summary>
    /// Obtém o saldo atual de créditos do usuário
    /// </summary>
    Task<UserCreditsDto> GetUserCreditsAsync(int userId);

    /// <summary>
    /// Verifica se o usuário tem créditos suficientes para uma operação
    /// </summary>
    Task<bool> HasSufficientCreditsAsync(int userId, int requiredCredits);

    /// <summary>
    /// Consome créditos do usuário após uma transação de busca
    /// </summary>
    Task<CreditOperationResult> ConsumeCreditsAsync(int userId, int amount, int? searchTransactionId = null, string? description = null);

    /// <summary>
    /// Adiciona créditos ao usuário (compra de pacote)
    /// </summary>
    Task<CreditOperationResult> AddCreditsAsync(int userId, int packageId, string? externalPaymentId = null);

    /// <summary>
    /// Adiciona créditos de bônus ao usuário
    /// </summary>
    Task<CreditOperationResult> AddBonusCreditsAsync(int userId, int amount, string description);

    /// <summary>
    /// Obtém histórico de transações de créditos do usuário
    /// </summary>
    Task<PagedResult<CreditTransactionDto>> GetCreditHistoryAsync(int userId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Lista todos os pacotes de créditos disponíveis
    /// </summary>
    Task<List<CreditPackageDto>> GetAvailablePackagesAsync();

    /// <summary>
    /// Obtém um pacote específico pelo ID
    /// </summary>
    Task<CreditPackageDto?> GetPackageByIdAsync(int packageId);

    /// <summary>
    /// Estima o custo de uma busca antes de executá-la
    /// </summary>
    int EstimateSearchCost(int providerCount);
}
