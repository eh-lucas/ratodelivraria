namespace Sherlock.Business.DTOs;

/// <summary>
/// Informações de créditos do usuário
/// </summary>
public class UserCreditsDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Créditos disponíveis atualmente
    /// </summary>
    public int AvailableCredits { get; set; }

    /// <summary>
    /// Total de créditos já consumidos
    /// </summary>
    public int TotalCreditsUsed { get; set; }

    /// <summary>
    /// Custo estimado por busca (base + 1 por provider com sucesso)
    /// </summary>
    public int EstimatedCostPerSearch { get; set; } = 2;

    /// <summary>
    /// Quantidade estimada de buscas possíveis com o saldo atual
    /// </summary>
    public int EstimatedSearchesRemaining => AvailableCredits / Math.Max(1, EstimatedCostPerSearch);
}

/// <summary>
/// Resultado de uma operação de créditos
/// </summary>
public class CreditOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Quantidade de créditos afetados pela operação
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Saldo após a operação
    /// </summary>
    public int NewBalance { get; set; }

    /// <summary>
    /// ID da transação de crédito criada
    /// </summary>
    public int? TransactionId { get; set; }

    public static CreditOperationResult Succeeded(int amount, int newBalance, int? transactionId = null) => new()
    {
        Success = true,
        Message = "Operação realizada com sucesso",
        Amount = amount,
        NewBalance = newBalance,
        TransactionId = transactionId
    };

    public static CreditOperationResult Failed(string message) => new()
    {
        Success = false,
        Message = message
    };
}

/// <summary>
/// DTO para transação de créditos
/// </summary>
public class CreditTransactionDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string TypeDescription { get; set; } = string.Empty;

    /// <summary>
    /// Quantidade (positivo = adição, negativo = consumo)
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Saldo após a transação
    /// </summary>
    public int BalanceAfter { get; set; }

    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Nome do pacote comprado (se aplicável)
    /// </summary>
    public string? PackageName { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO para pacote de créditos
/// </summary>
public class CreditPackageDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Créditos base do pacote
    /// </summary>
    public int Credits { get; set; }

    /// <summary>
    /// Créditos bônus
    /// </summary>
    public int BonusCredits { get; set; }

    /// <summary>
    /// Total de créditos (base + bônus)
    /// </summary>
    public int TotalCredits { get; set; }

    /// <summary>
    /// Preço em reais (ex: 9.90)
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Preço formatado (ex: "R$ 9,90")
    /// </summary>
    public string PriceFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Preço por crédito em centavos
    /// </summary>
    public decimal PricePerCredit { get; set; }

    /// <summary>
    /// Se é o pacote mais popular
    /// </summary>
    public bool IsPopular { get; set; }

    /// <summary>
    /// Percentual de economia comparado ao pacote básico
    /// </summary>
    public int SavingsPercent { get; set; }
}

/// <summary>
/// Requisição de compra de créditos
/// </summary>
public class PurchaseCreditsRequest
{
    /// <summary>
    /// ID do pacote a ser comprado
    /// </summary>
    public int PackageId { get; set; }

    /// <summary>
    /// ID do pagamento externo (Stripe, PayPal, etc.)
    /// Para simulação, pode ser "SIMULATED"
    /// </summary>
    public string? PaymentId { get; set; }
}

/// <summary>
/// Resultado paginado genérico
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
