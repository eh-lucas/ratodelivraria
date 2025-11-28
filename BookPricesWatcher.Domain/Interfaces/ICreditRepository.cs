using Sherlock.Domain.Entities;

namespace Sherlock.Domain.Interfaces;

public interface ICreditRepository
{
    /// <summary>
    /// Obtém o saldo de créditos do usuário
    /// </summary>
    Task<int> GetUserCreditsAsync(int userId);

    /// <summary>
    /// Atualiza o saldo de créditos do usuário
    /// </summary>
    Task<bool> UpdateUserCreditsAsync(int userId, int newBalance, int totalUsed);

    /// <summary>
    /// Adiciona uma transação de créditos
    /// </summary>
    Task<CreditTransaction> AddCreditTransactionAsync(CreditTransaction transaction);

    /// <summary>
    /// Obtém o histórico de transações de créditos do usuário
    /// </summary>
    Task<(List<CreditTransaction> Items, int TotalCount)> GetCreditHistoryAsync(int userId, int page, int pageSize);

    /// <summary>
    /// Obtém todos os pacotes de créditos ativos
    /// </summary>
    Task<List<CreditPackage>> GetActivePackagesAsync();

    /// <summary>
    /// Obtém um pacote de créditos pelo ID
    /// </summary>
    Task<CreditPackage?> GetPackageByIdAsync(int packageId);

    /// <summary>
    /// Obtém um usuário pelo ID (com campos de créditos)
    /// </summary>
    Task<User?> GetUserByIdAsync(int userId);
}
