using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Sherlock.Data.Context;
using Sherlock.Domain.Entities;

namespace Sherlock.Api.Services;

/// <summary>Resultado de apresentar um refresh token.</summary>
public record RefreshOutcome(User? User, string? NewToken, DateTime? NewExpiresAt, string? Error)
{
    public bool Success => User != null && NewToken != null;

    public static RefreshOutcome Falha(string erro) => new(null, null, null, erro);
}

/// <summary>
/// Emite, roda e revoga os refresh tokens (docs/PLANO-OBSERVADOS.md §8.1).
///
/// Por que não um JWT de 30 dias: **sem senha, expirar sessão custa uma ida ao
/// e-mail.** Com rotação, quem usa o site nunca é deslogado — a sessão se renova
/// sozinha. Com prazo fixo, no dia 31 a pessoa cai no meio de uma busca e tem que
/// abrir a caixa de entrada. Passwordless existe para tirar fricção; não faz
/// sentido reintroduzi-la do outro lado.
///
/// E é isto que faz "sair de todos os aparelhos" e a exclusão de conta (§2.4)
/// funcionarem de verdade, em vez de só sumirem da tela.
/// </summary>
public class RefreshTokenService
{
    /// <summary>Quanto dura a sessão longa sem nenhum uso.</summary>
    public static readonly TimeSpan Validade = TimeSpan.FromDays(30);

    public const string CookieName = "sherlock_refresh";

    private readonly SherlockDbContext _context;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(SherlockDbContext context, ILogger<RefreshTokenService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Cria um token novo para a pessoa. Devolve o valor em claro — é a única vez
    /// que ele existe fora do cookie; o banco guarda só o hash.
    /// </summary>
    public async Task<(string Token, DateTime ExpiresAt)> EmitirAsync(
        int userId, CancellationToken cancellationToken = default)
    {
        var valor = GerarValor();
        var expira = DateTime.UtcNow.Add(Validade);

        _context.Tokens.Add(new Token
        {
            UserId = userId,
            TokenHash = Hash(valor),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expira,
        });

        await _context.SaveChangesAsync(cancellationToken);
        return (valor, expira);
    }

    /// <summary>
    /// Gasta o token apresentado e emite o próximo. Qualquer recusa devolve a
    /// mesma mensagem: dizer "expirado" em vez de "inválido" conta a quem roubou
    /// que o valor um dia foi bom.
    /// </summary>
    public async Task<RefreshOutcome> RotacionarAsync(
        string valorApresentado, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(valorApresentado))
            return RefreshOutcome.Falha("Sessão inválida.");

        var hash = Hash(valorApresentado);

        var token = await _context.Tokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (token == null) return RefreshOutcome.Falha("Sessão inválida.");

        // Token já gasto apresentado de novo: existem duas cópias dele no mundo.
        // A única resposta segura é derrubar a cadeia inteira daquela pessoa —
        // um login a mais incomoda menos que uma sessão clonada viva.
        if (token.ReplacedByHash != null)
        {
            _logger.LogWarning(
                "[Auth] Refresh token reaproveitado do usuário {UserId}; sessões revogadas", token.UserId);
            await RevogarTudoAsync(token.UserId, cancellationToken);
            return RefreshOutcome.Falha("Sessão inválida.");
        }

        if (!token.Ativo) return RefreshOutcome.Falha("Sessão inválida.");

        if (token.User is not { Active: true }) return RefreshOutcome.Falha("Sessão inválida.");

        var novoValor = GerarValor();
        var novoHash = Hash(novoValor);
        var expira = DateTime.UtcNow.Add(Validade);

        token.RevokedAt = DateTime.UtcNow;
        token.ReplacedByHash = novoHash;

        _context.Tokens.Add(new Token
        {
            UserId = token.UserId,
            TokenHash = novoHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expira,
        });

        await _context.SaveChangesAsync(cancellationToken);

        return new RefreshOutcome(token.User, novoValor, expira, null);
    }

    /// <summary>Encerra uma sessão. Silencioso se o token não existe — sair sempre dá certo.</summary>
    public async Task RevogarAsync(string? valorApresentado, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(valorApresentado)) return;

        var hash = Hash(valorApresentado);
        var token = await _context.Tokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (token is not { RevokedAt: null }) return;

        token.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Sair de todos os aparelhos — e o que a exclusão de conta precisa chamar.</summary>
    public async Task RevogarTudoAsync(int userId, CancellationToken cancellationToken = default)
    {
        var agora = DateTime.UtcNow;

        await _context.Tokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, agora), cancellationToken);
    }

    /// <summary>
    /// 256 bits de aleatoriedade criptográfica, em base64url. Não é Guid: Guid
    /// carrega bits previsíveis e isto aqui é credencial de sessão.
    /// </summary>
    private static string GerarValor()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string Hash(string valor)
        => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(valor))).ToLowerInvariant();
}
