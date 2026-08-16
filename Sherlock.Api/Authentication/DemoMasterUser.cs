namespace Sherlock.Api.Authentication;

/// <summary>
/// Singleton que guarda a identidade do usuário master do modo demo.
/// É populado no startup (após o seed no banco) e lido pelo
/// <see cref="DemoAuthenticationHandler"/> em cada requisição.
/// </summary>
public class DemoMasterUser
{
    public int UserId { get; set; }
    public string Role { get; set; } = "User";
    public string Email { get; set; } = string.Empty;
}
