namespace Sherlock.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Como chamar a pessoa nas telas ("Bom dia, Fulano"). Vem do `given_name` do
    /// Google, ou do cadastro por senha.
    /// </summary>
    public string? FirstName { get; set; }

    public required string Email { get; set; }

    /// <summary>
    /// O `sub` do token do Google — o identificador que não muda. Nulo para conta
    /// criada por e-mail e senha.
    ///
    /// Guardado além do e-mail porque e-mail de conta Google pode mudar, e aí só o
    /// `sub` continua apontando para a mesma pessoa.
    /// </summary>
    public string? GoogleId { get; set; }

    /// <summary>
    /// Nulo para quem entra pelo Google: essa conta nunca teve senha. É o que permite
    /// dizer "entre pelo Google" em vez de "senha incorreta" — ver §1.4 do
    /// docs/PLANO-OBSERVADOS.md.
    /// </summary>
    public string? PasswordHash { get; set; }
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
