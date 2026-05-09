using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Sherlock.Data.Context;
using Sherlock.Domain.Entities;
using Sherlock.Api.Controllers;
using Sherlock.Api.DTOs;
using Sherlock.Api.Services;

namespace Sherlock.Tests.API;

public class AuthControllerTests : IDisposable
{
    private readonly SherlockDbContext _context;
    private readonly TokenService _tokenService;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly IConfiguration _configuration;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        var options = new DbContextOptionsBuilder<SherlockDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SherlockDbContext(options);

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
        _loggerMock = new Mock<ILogger<AuthController>>();

        _controller = new AuthController(_context, _tokenService, _loggerMock.Object, _configuration);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region Register Tests

    [Fact]
    public async Task Register_WithValidData_ReturnsCreatedWithToken()
    {
        // Arrange
        var request = new RegisterRequestDTO
        {
            Email = "test@example.com",
            Password = "SecurePassword123"
        };

        // Act
        var result = await _controller.Register(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var response = createdResult.Value.Should().BeOfType<LoginResponseDTO>().Subject;

        response.Token.Should().NotBeNullOrEmpty();
        response.Email.Should().Be("test@example.com");
        response.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsConflict()
    {
        // Arrange
        var existingUser = new User
        {
            Email = "existing@example.com",
            Username = "existing",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = "User",
            Active = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var request = new RegisterRequestDTO
        {
            Email = "existing@example.com",
            Password = "SecurePassword123"
        };

        // Act
        var result = await _controller.Register(request);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Register_NormalizesEmail_ToLowerCase()
    {
        // Arrange
        var request = new RegisterRequestDTO
        {
            Email = "TEST@EXAMPLE.COM",
            Password = "SecurePassword123"
        };

        // Act
        await _controller.Register(request);

        // Assert
        var user = await _context.Users.FirstOrDefaultAsync();
        user.Should().NotBeNull();
        user!.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task Register_WithoutUsername_UsesEmailPrefix()
    {
        // Arrange
        var request = new RegisterRequestDTO
        {
            Email = "johndoe@example.com",
            Password = "SecurePassword123"
        };

        // Act
        await _controller.Register(request);

        // Assert
        var user = await _context.Users.FirstOrDefaultAsync();
        user.Should().NotBeNull();
        user!.Username.Should().Be("johndoe");
    }

    [Fact]
    public async Task Register_WithCustomUsername_UsesProvidedUsername()
    {
        // Arrange
        var request = new RegisterRequestDTO
        {
            Email = "test@example.com",
            Password = "SecurePassword123",
            Username = "CustomUser"
        };

        // Act
        await _controller.Register(request);

        // Assert
        var user = await _context.Users.FirstOrDefaultAsync();
        user.Should().NotBeNull();
        user!.Username.Should().Be("CustomUser");
    }

    [Fact]
    public async Task Register_HashesPassword_NotStoredPlainText()
    {
        // Arrange
        var request = new RegisterRequestDTO
        {
            Email = "test@example.com",
            Password = "SecurePassword123"
        };

        // Act
        await _controller.Register(request);

        // Assert
        var user = await _context.Users.FirstOrDefaultAsync();
        user.Should().NotBeNull();
        user!.PasswordHash.Should().NotBe("SecurePassword123");
        user.PasswordHash.Should().StartWith("$2"); // BCrypt prefix
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var password = "SecurePassword123";
        var user = new User
        {
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "User",
            Active = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequestDTO
        {
            Email = "test@example.com",
            Password = password
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LoginResponseDTO>().Subject;

        response.Token.Should().NotBeNullOrEmpty();
        response.Email.Should().Be("test@example.com");
        response.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task Login_WithInvalidEmail_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequestDTO
        {
            Email = "nonexistent@example.com",
            Password = "SomePassword"
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword"),
            Role = "User",
            Active = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequestDTO
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_WithInactiveUser_ReturnsUnauthorized()
    {
        // Arrange
        var user = new User
        {
            Email = "inactive@example.com",
            Username = "inactive",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123"),
            Role = "User",
            Active = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequestDTO
        {
            Email = "inactive@example.com",
            Password = "Password123"
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_IsCaseInsensitive_ForEmail()
    {
        // Arrange
        var password = "SecurePassword123";
        var user = new User
        {
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "User",
            Active = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequestDTO
        {
            Email = "TEST@EXAMPLE.COM",
            Password = password
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region Validate Tests

    [Fact]
    public void ValidateToken_ReturnsOk()
    {
        // Act
        var result = _controller.ValidateToken();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion
}
