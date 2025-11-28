namespace Sherlock.Domain.Entities;

/// <summary>
/// Registra todas as movimentações de créditos (compras, consumos, bônus)
/// </summary>
public class CreditTransaction
{
    public int Id { get; set; }
    public int UserId { get; set; }

    /// <summary>
    /// Tipo da transação: Purchase, Consumption, Bonus, Refund
    /// </summary>
    public CreditTransactionType Type { get; set; }

    /// <summary>
    /// Quantidade de créditos (positivo para adição, negativo para consumo)
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Saldo após a transação
    /// </summary>
    public int BalanceAfter { get; set; }

    /// <summary>
    /// Descrição da transação
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// ID do pacote de créditos comprado (se aplicável)
    /// </summary>
    public int? CreditPackageId { get; set; }

    /// <summary>
    /// ID da transação de busca que consumiu os créditos (se aplicável)
    /// </summary>
    public int? SearchTransactionId { get; set; }

    /// <summary>
    /// Identificador externo do pagamento (Stripe, PayPal, etc.)
    /// </summary>
    public string? ExternalPaymentId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual CreditPackage? CreditPackage { get; set; }
    public virtual Transaction? SearchTransaction { get; set; }
}

public enum CreditTransactionType
{
    /// <summary>
    /// Compra de pacote de créditos
    /// </summary>
    Purchase = 1,

    /// <summary>
    /// Consumo em busca de livros
    /// </summary>
    Consumption = 2,

    /// <summary>
    /// Bônus de boas-vindas ou promoção
    /// </summary>
    Bonus = 3,

    /// <summary>
    /// Reembolso
    /// </summary>
    Refund = 4
}
