using FluentAssertions;
using Sherlock.Business.Core.Scrapers;
using Sherlock.Domain.Entities;

namespace Sherlock.Tests.Business;

public class QueryResultTests
{
    private readonly Provider _testProvider;

    public QueryResultTests()
    {
        _testProvider = new Provider
        {
            Id = 1,
            Name = "Test Provider",
            Url = "https://test.com"
        };
    }

    [Fact]
    public void CreateSuccess_SetsAllPropertiesCorrectly()
    {
        // Act
        var result = QueryResult.CreateSuccess(
            _testProvider,
            "Test Book",
            "Test Author",
            29.99m,
            10,
            500,
            "https://test.com/book");

        // Assert
        result.ProviderId.Should().Be(1);
        result.ProviderName.Should().Be("Test Provider");
        result.ProviderUrl.Should().Be("https://test.com");
        result.Success.Should().BeTrue();
        result.Title.Should().Be("Test Book");
        result.Author.Should().Be("Test Author");
        result.Price.Should().Be(29.99m);
        result.Discount.Should().Be(10);
        result.ResponseTimeMs.Should().Be(500);
        result.ProductUrl.Should().Be("https://test.com/book");
        result.ErrorMessage.Should().BeNull();
        result.ErrorType.Should().BeNull();
    }

    [Fact]
    public void CreateFailure_SetsErrorProperties()
    {
        // Act
        var result = QueryResult.CreateFailure(
            _testProvider,
            QueryErrorType.Timeout,
            "Request timeout",
            15000);

        // Assert
        result.ProviderId.Should().Be(1);
        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(QueryErrorType.Timeout);
        result.ErrorMessage.Should().Be("Request timeout");
        result.ResponseTimeMs.Should().Be(15000);
        result.Price.Should().Be(0);
        result.Title.Should().BeNull();
    }

    [Fact]
    public void CreateFailure_WithHttpStatusCode_SetsStatusCode()
    {
        // Act
        var result = QueryResult.CreateFailure(
            _testProvider,
            QueryErrorType.HttpError,
            "HTTP 500",
            1000,
            500);

        // Assert
        result.HttpStatusCode.Should().Be(500);
        result.ErrorType.Should().Be(QueryErrorType.HttpError);
    }

    [Fact]
    public void CreateNoResult_ReturnsSuccessButNoData()
    {
        // Act
        var result = QueryResult.CreateNoResult(_testProvider, 800);

        // Assert
        result.Success.Should().BeTrue();
        result.Price.Should().Be(0);
        result.Title.Should().BeNull();
        result.ResponseTimeMs.Should().Be(800);
        result.HasValidResult.Should().BeFalse();
    }

    [Fact]
    public void HasValidResult_ReturnsTrueForValidData()
    {
        // Arrange
        var result = QueryResult.CreateSuccess(
            _testProvider,
            "Book",
            null,
            19.99m,
            0,
            100);

        // Assert
        result.HasValidResult.Should().BeTrue();
    }

    [Fact]
    public void HasValidResult_ReturnsFalseForNoPrice()
    {
        // Arrange
        var result = new QueryResult
        {
            Success = true,
            Title = "Book",
            Price = 0
        };

        // Assert
        result.HasValidResult.Should().BeFalse();
    }

    [Fact]
    public void HasValidResult_ReturnsFalseForEmptyTitle()
    {
        // Arrange
        var result = new QueryResult
        {
            Success = true,
            Title = "",
            Price = 19.99m
        };

        // Assert
        result.HasValidResult.Should().BeFalse();
    }

    [Fact]
    public void HasValidResult_ReturnsFalseWhenNotSuccess()
    {
        // Arrange
        var result = new QueryResult
        {
            Success = false,
            Title = "Book",
            Price = 19.99m
        };

        // Assert
        result.HasValidResult.Should().BeFalse();
    }

    [Fact]
    public void ToEntity_ConvertsToQueryCorrectly()
    {
        // Arrange
        var queryResult = QueryResult.CreateSuccess(
            _testProvider,
            "Test Book",
            "Test Author",
            29.99m,
            15,
            500,
            "https://test.com/book");

        // Act
        var entity = queryResult.ToEntity(transactionId: 123);

        // Assert
        entity.TransactionId.Should().Be(123);
        entity.ProviderId.Should().Be(1);
        entity.Success.Should().BeTrue();
        entity.Title.Should().Be("Test Book");
        entity.Author.Should().Be("Test Author");
        entity.Price.Should().Be(29.99m);
        entity.Discount.Should().Be(15);
        entity.ProductUrl.Should().Be("https://test.com/book");
        entity.ResponseTimeMs.Should().Be(500);
    }

    [Fact]
    public void ToEntity_SetsNullPriceForZero()
    {
        // Arrange
        var queryResult = QueryResult.CreateNoResult(_testProvider, 100);

        // Act
        var entity = queryResult.ToEntity(transactionId: 1);

        // Assert
        entity.Price.Should().BeNull();
        entity.Discount.Should().BeNull();
    }

    [Fact]
    public void ToEntity_IncludesErrorMessage()
    {
        // Arrange
        var queryResult = QueryResult.CreateFailure(
            _testProvider,
            QueryErrorType.Network,
            "Connection refused",
            0);

        // Act
        var entity = queryResult.ToEntity(transactionId: 1);

        // Assert
        entity.Success.Should().BeFalse();
        entity.ErrorMessage.Should().Be("Connection refused");
    }

    [Theory]
    [InlineData(QueryErrorType.Timeout)]
    [InlineData(QueryErrorType.Network)]
    [InlineData(QueryErrorType.HttpError)]
    [InlineData(QueryErrorType.ParseError)]
    [InlineData(QueryErrorType.Blocked)]
    [InlineData(QueryErrorType.Unknown)]
    public void CreateFailure_SupportsAllErrorTypes(QueryErrorType errorType)
    {
        // Act
        var result = QueryResult.CreateFailure(_testProvider, errorType, "Error", 100);

        // Assert
        result.ErrorType.Should().Be(errorType);
    }
}
