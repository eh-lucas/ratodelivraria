using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Sherlock.Api.Authentication;

/// <summary>
/// Esquema de autenticação do modo demo: autentica TODA requisição como o
/// usuário master, sem exigir token. Registrado como esquema default apenas
/// quando <c>DemoMode:Enabled = true</c>, mantendo a produção (JWT) intacta.
/// </summary>
public class DemoAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Demo";

    private readonly DemoMasterUser _master;

    public DemoAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        DemoMasterUser master)
        : base(options, logger, encoder)
    {
        _master = master;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var id = _master.UserId.ToString();

        // Popula as duas claims que os controllers usam (NameIdentifier e sub),
        // para que GetUserId() resolva o master em qualquer endpoint [Authorize].
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id),
            new Claim(JwtRegisteredClaimNames.Sub, id),
            new Claim(ClaimTypes.Role, _master.Role),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
