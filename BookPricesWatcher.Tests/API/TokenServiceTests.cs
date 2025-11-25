using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using SherlockAPI.Services;

namespace Sherlock.Tests.API;

public class TokenServiceTests
{
    private readonly TokenService _tokenService;
    private readonly IConfiguration _configuration;

    public TokenServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "JwtSettings:SecretKey", "ThisIsAVeryLongSecretKeyForTestingPurposesOnly12345" },
            { "JwtSettings:Issuer", "TestIssuer" },
            { "JwtSettings:Audience", "TestAudience" },
            { "JwtSettings:ExpiryInMinutes", "30" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _tokenService = new TokenService(_configuration);
    }

    [Fact]
    public void GenerateToken_ReturnsValidJwtToken()
    {
        // Act
        var token = _tokenService.GenerateToken("123", "User");

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Split('.').Should().HaveCount(3); // JWT has 3 parts
    }

    [Fact]
    public void GenerateToken_ContainsCorrectUserId()
    {
        // Arrange
        var userId = "456";

        // Act
        var token = _tokenService.GenerateToken(userId, "User");
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        jwtToken.Subject.Should().Be(userId);
    }

    [Fact]
    public void GenerateToken_ContainsCorrectRole()
    {
        // Arrange
        var role = "Admin";

        // Act
        var token = _tokenService.GenerateToken("123", role);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        var roleClaim = jwtToken.Claims.FirstOrDefault(c =>
            c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" ||
            c.Type == "role");
        roleClaim.Should().NotBeNull();
        roleClaim!.Value.Should().Be(role);
    }

    [Fact]
    public void GenerateToken_HasCorrectIssuer()
    {
        // Act
        var token = _tokenService.GenerateToken("123", "User");
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        jwtToken.Issuer.Should().Be("TestIssuer");
    }

    [Fact]
    public void GenerateToken_HasCorrectAudience()
    {
        // Act
        var token = _tokenService.GenerateToken("123", "User");
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        jwtToken.Audiences.Should().Contain("TestAudience");
    }

    [Fact]
    public void GenerateToken_HasFutureExpiration()
    {
        // Act
        var token = _tokenService.GenerateToken("123", "User");
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        jwtToken.ValidTo.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void GenerateToken_ExpiresInConfiguredTime()
    {
        // Arrange
        var expectedMinutes = 30;
        var tolerance = TimeSpan.FromMinutes(1);

        // Act
        var token = _tokenService.GenerateToken("123", "User");
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        var expectedExpiry = DateTime.UtcNow.AddMinutes(expectedMinutes);
        jwtToken.ValidTo.Should().BeCloseTo(expectedExpiry, tolerance);
    }

    [Fact]
    public void GenerateToken_HasUniqueJti()
    {
        // Act
        var token1 = _tokenService.GenerateToken("123", "User");
        var token2 = _tokenService.GenerateToken("123", "User");

        var handler = new JwtSecurityTokenHandler();
        var jti1 = handler.ReadJwtToken(token1).Claims.First(c => c.Type == "jti").Value;
        var jti2 = handler.ReadJwtToken(token2).Claims.First(c => c.Type == "jti").Value;

        // Assert
        jti1.Should().NotBe(jti2);
    }

    [Theory]
    [InlineData("1", "User")]
    [InlineData("999", "Admin")]
    [InlineData("abc-123", "Moderator")]
    public void GenerateToken_WorksWithVariousInputs(string userId, string role)
    {
        // Act
        var token = _tokenService.GenerateToken(userId, role);

        // Assert
        token.Should().NotBeNullOrEmpty();
    }
}
