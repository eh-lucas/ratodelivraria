using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Sherlock.Business.DTOs;
using Sherlock.Business.Services;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Interfaces;

namespace Sherlock.Tests.Business;

public class CreditServiceTests
{
    private readonly Mock<ICreditRepository> _repositoryMock;
    private readonly Mock<ILogger<CreditService>> _loggerMock;
    private readonly CreditService _service;

    public CreditServiceTests()
    {
        _repositoryMock = new Mock<ICreditRepository>();
        _loggerMock = new Mock<ILogger<CreditService>>();
        _service = new CreditService(_repositoryMock.Object, _loggerMock.Object);
    }

    #region GetUserCreditsAsync Tests

    [Fact]
    public async Task GetUserCreditsAsync_WithValidUser_ReturnsCredits()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@test.com",
            Role = "User",
            AvailableCredits = 100,
            TotalCreditsUsed = 50
        };

        _repositoryMock.Setup(r => r.GetUserByIdAsync(1))
            .ReturnsAsync(user);

        // Act
        var result = await _service.GetUserCreditsAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(1);
        result.Username.Should().Be("testuser");
        result.Email.Should().Be("test@test.com");
        result.AvailableCredits.Should().Be(100);
        result.TotalCreditsUsed.Should().Be(50);
        result.EstimatedCostPerSearch.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetUserCreditsAsync_WithInvalidUser_ThrowsException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetUserByIdAsync(999))
            .ReturnsAsync((User?)null);

        // Act
        var act = () => _service.GetUserCreditsAsync(999);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*999*não encontrado*");
    }

    #endregion

    #region HasSufficientCreditsAsync Tests

    [Fact]
    public async Task HasSufficientCreditsAsync_WithEnoughCredits_ReturnsTrue()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetUserCreditsAsync(1))
            .ReturnsAsync(100);

        // Act
        var result = await _service.HasSufficientCreditsAsync(1, 50);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasSufficientCreditsAsync_WithExactCredits_ReturnsTrue()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetUserCreditsAsync(1))
            .ReturnsAsync(50);

        // Act
        var result = await _service.HasSufficientCreditsAsync(1, 50);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasSufficientCreditsAsync_WithInsufficientCredits_ReturnsFalse()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetUserCreditsAsync(1))
            .ReturnsAsync(30);

        // Act
        var result = await _service.HasSufficientCreditsAsync(1, 50);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ConsumeCreditsAsync Tests

    [Fact]
    public async Task ConsumeCreditsAsync_WithZeroAmount_ReturnsSuccessWithoutChanges()
    {
        // Arrange & Act
        var result = await _service.ConsumeCreditsAsync(1, 0);

        // Assert
        result.Success.Should().BeTrue();
        result.Amount.Should().Be(0);
        result.NewBalance.Should().Be(0);
        _repositoryMock.Verify(r => r.UpdateUserCreditsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ConsumeCreditsAsync_WithValidAmount_ConsumesCredits()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@test.com",
            Role = "User",
            AvailableCredits = 100,
            TotalCreditsUsed = 0
        };

        var savedTransaction = new CreditTransaction { Id = 1 };

        _repositoryMock.Setup(r => r.GetUserByIdAsync(1))
            .ReturnsAsync(user);
        _repositoryMock.Setup(r => r.UpdateUserCreditsAsync(1, 90, 10))
            .ReturnsAsync(true);
        _repositoryMock.Setup(r => r.AddCreditTransactionAsync(It.IsAny<CreditTransaction>()))
            .ReturnsAsync(savedTransaction);

        // Act
        var result = await _service.ConsumeCreditsAsync(1, 10, description: "Test consumption");

        // Assert
        result.Success.Should().BeTrue();
        result.Amount.Should().Be(10);
        result.NewBalance.Should().Be(90);
        result.TransactionId.Should().Be(1);
    }

    [Fact]
    public async Task ConsumeCreditsAsync_WithInsufficientCredits_ReturnsFailed()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@test.com",
            Role = "User",
            AvailableCredits = 5,
            TotalCreditsUsed = 0
        };

        _repositoryMock.Setup(r => r.GetUserByIdAsync(1))
            .ReturnsAsync(user);

        // Act
        var result = await _service.ConsumeCreditsAsync(1, 10);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("insuficientes");
    }

    [Fact]
    public async Task ConsumeCreditsAsync_WithInvalidUser_ReturnsFailed()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetUserByIdAsync(999))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.ConsumeCreditsAsync(999, 10);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("não encontrado");
    }

    #endregion

    #region AddCreditsAsync Tests

    [Fact]
    public async Task AddCreditsAsync_WithValidPackage_AddsCredits()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@test.com",
            Role = "User",
            AvailableCredits = 50,
            TotalCreditsUsed = 10
        };

        var package = new CreditPackage
        {
            Id = 1,
            Name = "Starter",
            Credits = 50,
            BonusCredits = 0,
            PriceInCents = 490
        };

        var savedTransaction = new CreditTransaction { Id = 1 };

        _repositoryMock.Setup(r => r.GetPackageByIdAsync(1))
            .ReturnsAsync(package);
        _repositoryMock.Setup(r => r.GetUserByIdAsync(1))
            .ReturnsAsync(user);
        _repositoryMock.Setup(r => r.UpdateUserCreditsAsync(1, 100, 10))
            .ReturnsAsync(true);
        _repositoryMock.Setup(r => r.AddCreditTransactionAsync(It.IsAny<CreditTransaction>()))
            .ReturnsAsync(savedTransaction);

        // Act
        var result = await _service.AddCreditsAsync(1, 1, "PAY-123");

        // Assert
        result.Success.Should().BeTrue();
        result.Amount.Should().Be(50);
        result.NewBalance.Should().Be(100);
    }

    [Fact]
    public async Task AddCreditsAsync_WithInvalidPackage_ReturnsFailed()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetPackageByIdAsync(999))
            .ReturnsAsync((CreditPackage?)null);

        // Act
        var result = await _service.AddCreditsAsync(1, 999);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("não encontrado");
    }

    [Fact]
    public async Task AddCreditsAsync_WithInvalidUser_ReturnsFailed()
    {
        // Arrange
        var package = new CreditPackage
        {
            Id = 1,
            Name = "Starter",
            Credits = 50
        };

        _repositoryMock.Setup(r => r.GetPackageByIdAsync(1))
            .ReturnsAsync(package);
        _repositoryMock.Setup(r => r.GetUserByIdAsync(999))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.AddCreditsAsync(999, 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("não encontrado");
    }

    #endregion

    #region AddBonusCreditsAsync Tests

    [Fact]
    public async Task AddBonusCreditsAsync_WithValidAmount_AddsBonus()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@test.com",
            Role = "User",
            AvailableCredits = 50,
            TotalCreditsUsed = 10
        };

        var savedTransaction = new CreditTransaction { Id = 1 };

        _repositoryMock.Setup(r => r.GetUserByIdAsync(1))
            .ReturnsAsync(user);
        _repositoryMock.Setup(r => r.UpdateUserCreditsAsync(1, 70, 10))
            .ReturnsAsync(true);
        _repositoryMock.Setup(r => r.AddCreditTransactionAsync(It.IsAny<CreditTransaction>()))
            .ReturnsAsync(savedTransaction);

        // Act
        var result = await _service.AddBonusCreditsAsync(1, 20, "Welcome bonus");

        // Assert
        result.Success.Should().BeTrue();
        result.Amount.Should().Be(20);
        result.NewBalance.Should().Be(70);
    }

    [Fact]
    public async Task AddBonusCreditsAsync_WithZeroAmount_ReturnsFailed()
    {
        // Act
        var result = await _service.AddBonusCreditsAsync(1, 0, "Test");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("positiva");
    }

    [Fact]
    public async Task AddBonusCreditsAsync_WithNegativeAmount_ReturnsFailed()
    {
        // Act
        var result = await _service.AddBonusCreditsAsync(1, -10, "Test");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("positiva");
    }

    #endregion

    #region GetCreditHistoryAsync Tests

    [Fact]
    public async Task GetCreditHistoryAsync_ReturnsPagedResult()
    {
        // Arrange
        var transactions = new List<CreditTransaction>
        {
            new()
            {
                Id = 1,
                UserId = 1,
                Type = CreditTransactionType.Purchase,
                Amount = 100,
                BalanceAfter = 100,
                Description = "Compra",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                UserId = 1,
                Type = CreditTransactionType.Consumption,
                Amount = -10,
                BalanceAfter = 90,
                Description = "Busca",
                CreatedAt = DateTime.UtcNow
            }
        };

        _repositoryMock.Setup(r => r.GetCreditHistoryAsync(1, 1, 20))
            .ReturnsAsync((transactions, 2));

        // Act
        var result = await _service.GetCreditHistoryAsync(1, 1, 20);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task GetCreditHistoryAsync_MapsTypeDescriptionCorrectly()
    {
        // Arrange
        var transactions = new List<CreditTransaction>
        {
            new()
            {
                Id = 1,
                Type = CreditTransactionType.Purchase,
                Amount = 100,
                BalanceAfter = 100,
                Description = "Test"
            }
        };

        _repositoryMock.Setup(r => r.GetCreditHistoryAsync(1, 1, 20))
            .ReturnsAsync((transactions, 1));

        // Act
        var result = await _service.GetCreditHistoryAsync(1, 1, 20);

        // Assert
        result.Items.First().TypeDescription.Should().Be("Compra de créditos");
    }

    #endregion

    #region GetAvailablePackagesAsync Tests

    [Fact]
    public async Task GetAvailablePackagesAsync_ReturnsPackages()
    {
        // Arrange
        var packages = new List<CreditPackage>
        {
            new()
            {
                Id = 1,
                Name = "Starter",
                Description = "50 creditos",
                Credits = 50,
                BonusCredits = 0,
                PriceInCents = 490,
                IsActive = true
            },
            new()
            {
                Id = 2,
                Name = "Popular",
                Description = "300 creditos",
                Credits = 300,
                BonusCredits = 50,
                PriceInCents = 1990,
                IsActive = true,
                IsPopular = true
            }
        };

        _repositoryMock.Setup(r => r.GetActivePackagesAsync())
            .ReturnsAsync(packages);

        // Act
        var result = await _service.GetAvailablePackagesAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Starter");
        result[0].TotalCredits.Should().Be(50); // 50 + 0 bonus
        result[1].Name.Should().Be("Popular");
        result[1].TotalCredits.Should().Be(350); // 300 + 50 bonus
        result[1].IsPopular.Should().BeTrue();
        result[1].SavingsPercent.Should().BeGreaterThan(0);
    }

    #endregion

    #region EstimateSearchCost Tests

    [Fact]
    public void EstimateSearchCost_WithZeroProviders_ReturnsBaseCost()
    {
        // Act
        var result = _service.EstimateSearchCost(0);

        // Assert
        result.Should().BeGreaterThan(0);
    }

    [Fact]
    public void EstimateSearchCost_WithTenProviders_ReturnsExpectedCost()
    {
        // Act
        var result = _service.EstimateSearchCost(10);

        // Assert
        // Base (1) + 5 providers (50% success) * 1 = 6
        result.Should().Be(6);
    }

    [Fact]
    public void EstimateSearchCost_IncreasesWithMoreProviders()
    {
        // Act
        var costFor10 = _service.EstimateSearchCost(10);
        var costFor20 = _service.EstimateSearchCost(20);

        // Assert
        costFor20.Should().BeGreaterThan(costFor10);
    }

    #endregion
}
