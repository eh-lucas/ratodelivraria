namespace Sherlock.Domain.Entities;

/// <summary>
/// Pacotes de créditos disponíveis para compra
/// </summary>
public class CreditPackage
{
    public int Id { get; set; }

    /// <summary>
    /// Nome do pacote (ex: "Básico", "Popular", "Premium")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Descrição do pacote
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Quantidade de créditos no pacote
    /// </summary>
    public int Credits { get; set; }

    /// <summary>
    /// Preço em centavos (R$ 9,90 = 990)
    /// </summary>
    public int PriceInCents { get; set; }

    /// <summary>
    /// Créditos bônus (ex: compre 100, ganhe 20 de bônus)
    /// </summary>
    public int BonusCredits { get; set; } = 0;

    /// <summary>
    /// Se o pacote está ativo para venda
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Se é o pacote mais popular (destaque na UI)
    /// </summary>
    public bool IsPopular { get; set; } = false;

    /// <summary>
    /// Ordem de exibição na UI
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Total de créditos (base + bônus)
    /// </summary>
    public int TotalCredits => Credits + BonusCredits;

    /// <summary>
    /// Preço formatado em reais
    /// </summary>
    public decimal PriceInReais => PriceInCents / 100m;

    /// <summary>
    /// Preço por crédito em centavos
    /// </summary>
    public decimal PricePerCredit => TotalCredits > 0 ? (decimal)PriceInCents / TotalCredits : 0;

    // Pacotes padrão pré-definidos
    public static readonly List<CreditPackage> DefaultPackages = new()
    {
        new CreditPackage
        {
            Id = 1,
            Name = "Starter",
            Description = "Ideal para testar o serviço",
            Credits = 50,
            BonusCredits = 0,
            PriceInCents = 490, // R$ 4,90
            IsActive = true,
            IsPopular = false,
            DisplayOrder = 1
        },
        new CreditPackage
        {
            Id = 2,
            Name = "Básico",
            Description = "Para uso casual",
            Credits = 100,
            BonusCredits = 10,
            PriceInCents = 890, // R$ 8,90
            IsActive = true,
            IsPopular = false,
            DisplayOrder = 2
        },
        new CreditPackage
        {
            Id = 3,
            Name = "Popular",
            Description = "Melhor custo-benefício",
            Credits = 300,
            BonusCredits = 50,
            PriceInCents = 1990, // R$ 19,90
            IsActive = true,
            IsPopular = true,
            DisplayOrder = 3
        },
        new CreditPackage
        {
            Id = 4,
            Name = "Premium",
            Description = "Para usuários frequentes",
            Credits = 1000,
            BonusCredits = 200,
            PriceInCents = 4990, // R$ 49,90
            IsActive = true,
            IsPopular = false,
            DisplayOrder = 4
        }
    };
}
