using FluentAssertions;
using Sherlock.Business.Core.Base;
using Sherlock.Business.Core.Scrapers;

namespace Sherlock.Tests.Business;

public class ComparatorTests
{
    private readonly Comparator _comparator;

    public ComparatorTests()
    {
        _comparator = new Comparator();
    }

    [Fact]
    public void Compare_WithEmptyList_ReturnsEmptyBookPriceResult()
    {
        // Arrange
        var emptyList = new List<BookPriceResult>();

        // Act
        var result = _comparator.Compare(emptyList);

        // Assert
        result.Should().NotBeNull();
        result.Price.Should().Be(0);
    }

    [Fact]
    public void Compare_WithSingleResult_ReturnsThatResult()
    {
        // Arrange
        var results = new List<BookPriceResult>
        {
            new() { Title = "Test Book", Price = 29.90m, Website = "store.com" }
        };

        // Act
        var result = _comparator.Compare(results);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Test Book");
        result.Price.Should().Be(29.90m);
    }

    [Fact]
    public void Compare_WithMultipleResults_ReturnsLowestPrice()
    {
        // Arrange
        var results = new List<BookPriceResult>
        {
            new() { Title = "Book A", Price = 50.00m, Website = "expensive.com" },
            new() { Title = "Book B", Price = 25.00m, Website = "cheap.com" },
            new() { Title = "Book C", Price = 35.00m, Website = "medium.com" }
        };

        // Act
        var result = _comparator.Compare(results);

        // Assert
        result.Price.Should().Be(25.00m);
        result.Website.Should().Be("cheap.com");
    }

    [Fact]
    public void Compare_FiltersOutResultsWithEmptyTitle()
    {
        // Arrange
        var results = new List<BookPriceResult>
        {
            new() { Title = "", Price = 10.00m, Website = "notitle.com" },
            new() { Title = "Valid Book", Price = 30.00m, Website = "valid.com" }
        };

        // Act
        var result = _comparator.Compare(results);

        // Assert
        result.Title.Should().Be("Valid Book");
        result.Price.Should().Be(30.00m);
    }

    [Fact]
    public void Compare_WithAllEmptyTitles_ThrowsOrReturnsEmpty()
    {
        // Arrange
        var results = new List<BookPriceResult>
        {
            new() { Title = "", Price = 10.00m, Website = "a.com" },
            new() { Title = "", Price = 20.00m, Website = "b.com" }
        };

        // Act & Assert
        var action = () => _comparator.Compare(results);
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Compare_WithSamePrices_ReturnsFirst()
    {
        // Arrange
        var results = new List<BookPriceResult>
        {
            new() { Title = "Book A", Price = 25.00m, Website = "first.com" },
            new() { Title = "Book B", Price = 25.00m, Website = "second.com" }
        };

        // Act
        var result = _comparator.Compare(results);

        // Assert
        result.Price.Should().Be(25.00m);
        result.Website.Should().Be("first.com");
    }

    [Fact]
    public void Compare_WithDecimalPrices_HandlesCorrectly()
    {
        // Arrange
        var results = new List<BookPriceResult>
        {
            new() { Title = "Book A", Price = 29.99m, Website = "a.com" },
            new() { Title = "Book B", Price = 29.98m, Website = "b.com" },
            new() { Title = "Book C", Price = 30.00m, Website = "c.com" }
        };

        // Act
        var result = _comparator.Compare(results);

        // Assert
        result.Price.Should().Be(29.98m);
    }
}
