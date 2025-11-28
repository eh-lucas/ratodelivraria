using Microsoft.Extensions.Logging;
using Sherlock.Business.DTOs;
using Sherlock.Business.Interfaces;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Interfaces;

namespace Sherlock.Business.Services;

public class CreditService : ICreditService
{
    private readonly ICreditRepository _creditRepository;
    private readonly ILogger<CreditService> _logger;

    // Custo base por transação de busca
    private const int BaseCostPerSearch = 1;
    // Custo por query bem-sucedida
    private const int CostPerSuccessfulQuery = 1;

    public CreditService(ICreditRepository creditRepository, ILogger<CreditService> logger)
    {
        _creditRepository = creditRepository;
        _logger = logger;
    }

    public async Task<UserCreditsDto> GetUserCreditsAsync(int userId)
    {
        var user = await _creditRepository.GetUserByIdAsync(userId);

        if (user == null)
        {
            throw new InvalidOperationException($"Usuário {userId} não encontrado");
        }

        return new UserCreditsDto
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            AvailableCredits = user.AvailableCredits,
            TotalCreditsUsed = user.TotalCreditsUsed,
            EstimatedCostPerSearch = EstimateSearchCost(10) // Média de 10 providers
        };
    }

    public async Task<bool> HasSufficientCreditsAsync(int userId, int requiredCredits)
    {
        var availableCredits = await _creditRepository.GetUserCreditsAsync(userId);
        return availableCredits >= requiredCredits;
    }

    public async Task<CreditOperationResult> ConsumeCreditsAsync(
        int userId,
        int amount,
        int? searchTransactionId = null,
        string? description = null)
    {
        if (amount <= 0)
        {
            return CreditOperationResult.Succeeded(0, 0);
        }

        try
        {
            var user = await _creditRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return CreditOperationResult.Failed("Usuário não encontrado");
            }

            if (user.AvailableCredits < amount)
            {
                return CreditOperationResult.Failed(
                    $"Créditos insuficientes. Disponível: {user.AvailableCredits}, Necessário: {amount}");
            }

            var newBalance = user.AvailableCredits - amount;
            var newTotalUsed = user.TotalCreditsUsed + amount;

            // Atualiza saldo do usuário
            await _creditRepository.UpdateUserCreditsAsync(userId, newBalance, newTotalUsed);

            // Registra a transação de créditos
            var creditTransaction = new CreditTransaction
            {
                UserId = userId,
                Type = CreditTransactionType.Consumption,
                Amount = -amount, // Negativo para consumo
                BalanceAfter = newBalance,
                Description = description ?? "Consumo em busca de livros",
                SearchTransactionId = searchTransactionId,
                CreatedAt = DateTime.UtcNow
            };

            var savedTransaction = await _creditRepository.AddCreditTransactionAsync(creditTransaction);

            _logger.LogInformation(
                "Créditos consumidos: UserId={UserId}, Amount={Amount}, NewBalance={NewBalance}",
                userId, amount, newBalance);

            return CreditOperationResult.Succeeded(amount, newBalance, savedTransaction.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consumir créditos para usuário {UserId}", userId);
            return CreditOperationResult.Failed("Erro ao processar consumo de créditos");
        }
    }

    public async Task<CreditOperationResult> AddCreditsAsync(
        int userId,
        int packageId,
        string? externalPaymentId = null)
    {
        var package = await _creditRepository.GetPackageByIdAsync(packageId);

        if (package == null)
        {
            return CreditOperationResult.Failed("Pacote de créditos não encontrado ou inativo");
        }

        try
        {
            var user = await _creditRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return CreditOperationResult.Failed("Usuário não encontrado");
            }

            var totalCredits = package.TotalCredits;
            var newBalance = user.AvailableCredits + totalCredits;

            // Atualiza saldo do usuário
            await _creditRepository.UpdateUserCreditsAsync(userId, newBalance, user.TotalCreditsUsed);

            // Registra a transação de créditos
            var creditTransaction = new CreditTransaction
            {
                UserId = userId,
                Type = CreditTransactionType.Purchase,
                Amount = totalCredits,
                BalanceAfter = newBalance,
                Description = $"Compra do pacote {package.Name}",
                CreditPackageId = packageId,
                ExternalPaymentId = externalPaymentId,
                CreatedAt = DateTime.UtcNow
            };

            var savedTransaction = await _creditRepository.AddCreditTransactionAsync(creditTransaction);

            _logger.LogInformation(
                "Créditos adicionados: UserId={UserId}, PackageId={PackageId}, Amount={Amount}, NewBalance={NewBalance}",
                userId, packageId, totalCredits, newBalance);

            return CreditOperationResult.Succeeded(totalCredits, newBalance, savedTransaction.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar créditos para usuário {UserId}", userId);
            return CreditOperationResult.Failed("Erro ao processar compra de créditos");
        }
    }

    public async Task<CreditOperationResult> AddBonusCreditsAsync(
        int userId,
        int amount,
        string description)
    {
        if (amount <= 0)
        {
            return CreditOperationResult.Failed("Quantidade de créditos deve ser positiva");
        }

        try
        {
            var user = await _creditRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return CreditOperationResult.Failed("Usuário não encontrado");
            }

            var newBalance = user.AvailableCredits + amount;

            // Atualiza saldo do usuário
            await _creditRepository.UpdateUserCreditsAsync(userId, newBalance, user.TotalCreditsUsed);

            // Registra a transação de créditos
            var creditTransaction = new CreditTransaction
            {
                UserId = userId,
                Type = CreditTransactionType.Bonus,
                Amount = amount,
                BalanceAfter = newBalance,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };

            var savedTransaction = await _creditRepository.AddCreditTransactionAsync(creditTransaction);

            _logger.LogInformation(
                "Créditos bônus adicionados: UserId={UserId}, Amount={Amount}, NewBalance={NewBalance}",
                userId, amount, newBalance);

            return CreditOperationResult.Succeeded(amount, newBalance, savedTransaction.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar créditos bônus para usuário {UserId}", userId);
            return CreditOperationResult.Failed("Erro ao processar bônus de créditos");
        }
    }

    public async Task<PagedResult<CreditTransactionDto>> GetCreditHistoryAsync(
        int userId,
        int page = 1,
        int pageSize = 20)
    {
        var (items, totalCount) = await _creditRepository.GetCreditHistoryAsync(userId, page, pageSize);

        var dtos = items.Select(ct => new CreditTransactionDto
        {
            Id = ct.Id,
            Type = ct.Type.ToString(),
            TypeDescription = GetTypeDescription(ct.Type),
            Amount = ct.Amount,
            BalanceAfter = ct.BalanceAfter,
            Description = ct.Description,
            PackageName = ct.CreditPackage?.Name,
            CreatedAt = ct.CreatedAt
        }).ToList();

        return new PagedResult<CreditTransactionDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<CreditPackageDto>> GetAvailablePackagesAsync()
    {
        var packages = await _creditRepository.GetActivePackagesAsync();

        // Calcula economia baseado no preço por crédito do primeiro pacote
        var basePricePerCredit = packages.FirstOrDefault()?.PricePerCredit ?? 0;

        return packages.Select(p => new CreditPackageDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Credits = p.Credits,
            BonusCredits = p.BonusCredits,
            TotalCredits = p.TotalCredits,
            Price = p.PriceInReais,
            PriceFormatted = $"R$ {p.PriceInReais:N2}",
            PricePerCredit = p.PricePerCredit,
            IsPopular = p.IsPopular,
            SavingsPercent = basePricePerCredit > 0
                ? (int)Math.Round((1 - p.PricePerCredit / basePricePerCredit) * 100)
                : 0
        }).ToList();
    }

    public async Task<CreditPackageDto?> GetPackageByIdAsync(int packageId)
    {
        var package = await _creditRepository.GetPackageByIdAsync(packageId);

        if (package == null) return null;

        return new CreditPackageDto
        {
            Id = package.Id,
            Name = package.Name,
            Description = package.Description,
            Credits = package.Credits,
            BonusCredits = package.BonusCredits,
            TotalCredits = package.TotalCredits,
            Price = package.PriceInReais,
            PriceFormatted = $"R$ {package.PriceInReais:N2}",
            PricePerCredit = package.PricePerCredit,
            IsPopular = package.IsPopular
        };
    }

    public int EstimateSearchCost(int providerCount)
    {
        // Base + 1 por provider (assumindo 50% de sucesso)
        var estimatedSuccessful = Math.Max(1, providerCount / 2);
        return BaseCostPerSearch + (estimatedSuccessful * CostPerSuccessfulQuery);
    }

    private static string GetTypeDescription(CreditTransactionType type) => type switch
    {
        CreditTransactionType.Purchase => "Compra de créditos",
        CreditTransactionType.Consumption => "Consumo em busca",
        CreditTransactionType.Bonus => "Bônus",
        CreditTransactionType.Refund => "Reembolso",
        _ => "Outro"
    };
}
