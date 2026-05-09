using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Sherlock.Api.Services;
using Sherlock.Tests.Integration.Infrastructure;

namespace Sherlock.Tests.Integration;

// Paths negativos de auth contra GET /api/User/me. Garante que o middleware JWT
// rejeita tokens malformados, expirados, com signature alterada, scheme errado e vazios.
[Collection(nameof(IntegrationTestCollection))]
public class AuthNegativeTests(SherlockApiFactory factory)
{
    [Fact]
    public async Task MalformedToken_Returns401()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "banana");

        var response = await client.GetAsync("/api/User/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExpiredToken_Returns401()
    {
        // Gera um JWT assinado com a mesma chave da app, mas com Expires no passado
        // alem do ClockSkew default (5 min). Signature passa, lifetime falha.
        var token = GenerateJwtWithExpiry(DateTime.UtcNow.AddMinutes(-10));

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/User/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TamperedSignature_Returns401()
    {
        // Gera token valido e altera o ultimo caractere (parte da signature em base64).
        var (_, user) = await AuthHelper.CreateAuthenticatedClientAsync(factory);
        var tampered = user.Token[..^1] + (user.Token[^1] == 'A' ? 'B' : 'A');

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tampered);

        var response = await client.GetAsync("/api/User/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WrongScheme_Returns401()
    {
        // Token valido, scheme errado (Basic em vez de Bearer).
        var (_, user) = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", user.Token);

        var response = await client.GetAsync("/api/User/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task EmptyBearerToken_Returns401()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer ");

        var response = await client.GetAsync("/api/User/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // Constroi JWT manualmente com mesma SecretKey da app mas Expires customizado.
    private string GenerateJwtWithExpiry(DateTime expiresUtc)
    {
        using var scope = factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var secretKey = config["JwtSettings:SecretKey"]!;
        var key = Encoding.UTF8.GetBytes(secretKey);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "1"),
                new Claim(ClaimTypes.Role, "User"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            }),
            // NotBefore precisa preceder Expires na lib JWT — usamos -1h pra cobrir
            // qualquer Expires no passado solicitado pelo teste.
            NotBefore = expiresUtc.AddHours(-1),
            Expires = expiresUtc,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }
}
