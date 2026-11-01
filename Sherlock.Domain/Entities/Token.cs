namespace Sherlock.Domain.Entities;

/// <summary>
/// Um refresh token: a prova de que aquele navegador pode pedir um novo token de
/// acesso sem a pessoa entrar de novo (docs/PLANO-OBSERVADOS.md §8.1).
///
/// Três decisões que explicam o formato:
///
/// **O valor não fica guardado.** A coluna tem o SHA-256 dele. Um vazamento de
/// banco entrega hashes, não sessões — e o segredo em si só existe no cookie do
/// navegador de quem entrou.
///
/// **Ele roda.** Cada uso gasta o token e emite outro. Então um token roubado tem
/// validade de um uso, e o uso de um token já gasto é sinal de roubo — ver
/// <see cref="ReplacedByHash"/>.
///
/// **Nada de aparelho ou IP aqui.** Dá para montar "sair de todos os aparelhos"
/// sem guardar de onde a pessoa entrou, e dado que não se coleta não vaza (§2.1).
/// </summary>
public class Token
{
    public int Id { get; set; }

    public int UserId { get; set; }

    /// <summary>SHA-256 do valor que foi para o cookie, em hexadecimal.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Preenchido quando o token é gasto, revogado, ou morre junto com a conta.
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Hash do token que substituiu este na rotação.
    ///
    /// É o que permite detectar roubo: se alguém apresentar um token já
    /// substituído, existem duas cópias dele no mundo. A resposta é derrubar a
    /// cadeia inteira daquela pessoa — melhor obrigar um login a mais do que
    /// deixar uma sessão clonada viva.
    /// </summary>
    public string? ReplacedByHash { get; set; }

    public bool Ativo => RevokedAt == null && ExpiresAt > DateTime.UtcNow;

    public User? User { get; set; }
}
