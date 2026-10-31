using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sherlock.Api.Configurations;
using Sherlock.Data.Context;
using Sherlock.Domain.Entities;

namespace Sherlock.Api.Services;

/// <summary>Resultado de uma tentativa de entrar pelo Google.</summary>
public record GoogleLoginOutcome(bool Success, User? User, string? Error)
{
    public static GoogleLoginOutcome Ok(User user) => new(true, user, null);
    public static GoogleLoginOutcome Fail(string error) => new(false, null, error);
}

/// <summary>
/// Troca o ID token do Google por um usuário nosso.
///
/// Todo o valor desta classe está na validação: o token chega pelo navegador, que é
/// território do visitante. Sem conferir a assinatura, qualquer um monta um JSON
/// dizendo ser o e-mail de outra pessoa e entra como ela.
/// </summary>
public class GoogleLoginService
{
    private readonly SherlockDbContext _context;
    private readonly GoogleAuthSettings _settings;
    private readonly UsernameService _usernames;
    private readonly ILogger<GoogleLoginService> _logger;

    public GoogleLoginService(
        SherlockDbContext context,
        IOptions<GoogleAuthSettings> settings,
        UsernameService usernames,
        ILogger<GoogleLoginService> logger)
    {
        _context = context;
        _settings = settings.Value;
        _usernames = usernames;
        _logger = logger;
    }

    public bool Enabled => _settings.Enabled;

    public async Task<GoogleLoginOutcome> SignInAsync(string credential, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
            return GoogleLoginOutcome.Fail("Entrada pelo Google não está configurada.");

        GoogleJsonWebSignature.Payload payload;
        try
        {
            // Audience não é detalhe de configuração: sem ele, um ID token legítimo
            // obtido em QUALQUER outro site que use Google entraria aqui. A validação
            // exige que o token tenha sido emitido para o nosso client id.
            payload = await GoogleJsonWebSignature.ValidateAsync(credential,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _settings.ClientId }
                });
        }
        catch (InvalidJwtException ex)
        {
            // Assinatura errada, expirado ou audience de outro site. Não dizemos qual:
            // para quem tentou forjar, o detalhe é informação.
            _logger.LogWarning("ID token do Google recusado: {Motivo}", ex.Message);
            return GoogleLoginOutcome.Fail("Não foi possível validar sua conta Google.");
        }

        // O Google emite token para conta cujo e-mail ele ainda não confirmou. Aceitar
        // isso permitiria assumir a conta de outra pessoa só declarando o e-mail dela
        // — é a trava que faz o encontro por e-mail (§1.4) ser seguro.
        if (!payload.EmailVerified || string.IsNullOrWhiteSpace(payload.Email))
        {
            _logger.LogWarning("ID token do Google sem e-mail verificado");
            return GoogleLoginOutcome.Fail("Sua conta Google precisa ter o e-mail verificado.");
        }

        var email = payload.Email.ToLowerInvariant().Trim();

        // Procura pelo 'sub' primeiro: é o identificador que não muda, mesmo se a
        // pessoa trocar o e-mail da conta Google.
        var user = await _context.Users.FirstOrDefaultAsync(u => u.GoogleId == payload.Subject, cancellationToken)
                   ?? await _context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user == null)
        {
            user = new User
            {
                Email = email,
                FirstName = PrimeiroNome(payload),
                GoogleId = payload.Subject,
                Username = await _usernames.GerarAPartirDoEmailAsync(email, cancellationToken),
                Role = "User",
                Active = true,
                CreatedAt = DateTime.UtcNow,
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Conta criada pelo Google: {UserId}", user.Id);

            return GoogleLoginOutcome.Ok(user);
        }

        if (!user.Active)
            return GoogleLoginOutcome.Fail("Esta conta está desativada.");

        // Conta que já existia por e-mail e senha: anexa o Google em vez de criar uma
        // segunda. A partir daqui a pessoa entra pelos dois caminhos.
        var mudou = false;

        if (user.GoogleId == null)
        {
            user.GoogleId = payload.Subject;
            mudou = true;
        }

        if (string.IsNullOrWhiteSpace(user.FirstName))
        {
            user.FirstName = PrimeiroNome(payload);
            mudou = true;
        }

        if (mudou)
        {
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Login pelo Google: {UserId}", user.Id);
        return GoogleLoginOutcome.Ok(user);
    }

    private static string PrimeiroNome(GoogleJsonWebSignature.Payload payload)
    {
        if (!string.IsNullOrWhiteSpace(payload.GivenName))
            return payload.GivenName.Trim();

        // Sem given_name, a primeira palavra do nome completo. Sem nenhum dos dois,
        // o começo do e-mail — melhor que deixar vazio e cumprimentar ninguém.
        if (!string.IsNullOrWhiteSpace(payload.Name))
            return payload.Name.Trim().Split(' ')[0];

        return payload.Email.Split('@')[0];
    }
}
