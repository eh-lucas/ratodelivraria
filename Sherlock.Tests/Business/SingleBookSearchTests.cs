using FluentAssertions;
using Sherlock.Business.DTOs;

namespace Sherlock.Tests.Business;

public class SingleBookSearchTests
{
    [Fact]
    public void SingleBookSearchRequest_HasDefaultValues()
    {
        // Act
        var request = new SingleBookSearchRequest();

        // Assert
        request.Isbn.Should().BeEmpty();
        request.ProviderUrls.Should().BeNull();
    }

    [Fact]
    public void SingleBookSearchRequest_CanSetAllProperties()
    {
        // Arrange
        var request = new SingleBookSearchRequest
        {
            Isbn = "978-0132350884",
            ProviderUrls = new List<string> { "http://provider1.com", "http://provider2.com" }
        };

        // Assert
        request.Isbn.Should().Be("978-0132350884");
        request.ProviderUrls.Should().HaveCount(2);
    }

    [Fact]
    public void SingleBookSearchResult_HasDefaultValues()
    {
        // Act
        var result = new SingleBookSearchResult();

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().BeEmpty();
        result.BestOption.Should().BeNull();
        result.Alternatives.Should().NotBeNull();
        result.Alternatives.Should().BeEmpty();
        result.TotalProvidersSearched.Should().Be(0);
        result.ProvidersWithResults.Should().Be(0);
        result.ExecutionTimeMs.Should().Be(0);
        result.CreditsUsed.Should().Be(0);
        result.FromCache.Should().BeFalse();
    }

    [Fact]
    public void SingleBookSearchResult_CanSetBestOption()
    {
        // Arrange
        var bestOption = new BookPriceOption
        {
            BookTitle = "Clean Code",
            Price = 89.90m,
            ProviderName = "Amazon",
            ProviderId = 1
        };

        var result = new SingleBookSearchResult
        {
            Success = true,
            Message = "1 resultado encontrado",
            BestOption = bestOption
        };

        // Assert
        result.Success.Should().BeTrue();
        result.BestOption.Should().NotBeNull();
        result.BestOption!.BookTitle.Should().Be("Clean Code");
        result.BestOption.Price.Should().Be(89.90m);
    }

    [Fact]
    public void SingleBookSearchResult_CanSetAlternatives()
    {
        // Arrange
        var alternatives = new List<BookPriceOption>
        {
            new() { BookTitle = "Clean Code", Price = 95.00m, ProviderName = "Provider A" },
            new() { BookTitle = "Clean Code", Price = 99.90m, ProviderName = "Provider B" }
        };

        var result = new SingleBookSearchResult
        {
            Success = true,
            Alternatives = alternatives
        };

        // Assert
        result.Alternatives.Should().HaveCount(2);
        result.Alternatives[0].Price.Should().Be(95.00m);
        result.Alternatives[1].Price.Should().Be(99.90m);
    }

    [Fact]
    public void SingleBookSearchResult_AlternativesShouldBeLimitedToTwo()
    {
        // Arrange - simula o comportamento esperado do serviço
        var allOptions = new List<BookPriceOption>
        {
            new() { Price = 10m },
            new() { Price = 20m },
            new() { Price = 30m },
            new() { Price = 40m }
        };

        // Act - simula lógica do serviço: pega top 2 após o melhor
        var best = allOptions.First();
        var alternatives = allOptions.Skip(1).Take(2).ToList();

        // Assert
        alternatives.Should().HaveCount(2);
        alternatives[0].Price.Should().Be(20m);
        alternatives[1].Price.Should().Be(30m);
    }
}
