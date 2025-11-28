namespace Sherlock.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public required string Email { get; set; }
    public long Cpf { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public bool Active { get; set; }
    public required string Role { get; set; }

    /// <summary>
    /// Créditos disponíveis para o usuário realizar buscas.
    /// Novos usuários recebem 100 créditos de boas-vindas.
    /// </summary>
    public int AvailableCredits { get; set; } = 100;

    /// <summary>
    /// Total de créditos já consumidos pelo usuário (histórico)
    /// </summary>
    public int TotalCreditsUsed { get; set; } = 0;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime DeletedAt { get; set; }

    // Navigation properties
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public virtual ICollection<CreditTransaction> CreditTransactions { get; set; } = new List<CreditTransaction>();
}
