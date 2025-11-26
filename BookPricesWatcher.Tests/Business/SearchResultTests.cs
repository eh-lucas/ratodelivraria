using FluentAssertions;
using Sherlock.Business.Core.Scrapers;
using Sherlock.Domain.Entities;

namespace Sherlock.Tests.Business;

public class SearchResultTests
{
    [Fact]
    public void NewSearchResult_HasDefaultValues()
    {
        // Act
        var result = new SearchResult();

        // Assert
        result.TempoDecorrido.Should().Be(0);
        result.CustoCreditos.Should().Be(0);
        result.TotalSourcesQueried.Should().Be(0);
        result.SuccessfulQueries.Should().Be(0);
        result.FailedQueries.Should().Be(0);
        result.FromCache.Should().BeFalse();
    }

    [Fact]
    public void NewSearchResult_HasEmptyErrorsList()
    {
        // Act
        var result = new SearchResult();

        // Assert
        result.Errors.Should().NotBeNull();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void NewSearchResult_HasEmptyAllResultsList()
    {
        // Act
        var result = new SearchResult();

        // Assert
        result.AllResults.Should().NotBeNull();
        result.AllResults.Should().BeEmpty();
    }

    [Fact]
    public void NewSearchResult_HasDefaultResultType()
    {
        // Act
        var result = new SearchResult();

        // Assert
        result.ResultadoTransacao.Should().NotBeNull();
    }

    [Fact]
    public void NewSearchResult_HasDefaultBookPriceResult()
    {
        // Act
        var result = new SearchResult();

        // Assert
        result.BookPriceResult.Should().NotBeNull();
    }

    [Fact]
    public void SearchResult_CanSetAllProperties()
    {
        // Arrange
        var result = new SearchResult
        {
            InicioConsulta = DateTime.Now.AddSeconds(-5),
            FimConsulta = DateTime.Now,
            TempoDecorrido = 5000,
            CustoCreditos = 10,
            TotalSourcesQueried = 50,
            SuccessfulQueries = 45,
            FailedQueries = 5,
            FromCache = true,
            ResultadoTransacao = TransactionResult.Success,
            BookPriceResult = new BookPriceResult { Title = "Test", Price = 29.99m }
        };

        // Assert
        result.TempoDecorrido.Should().Be(5000);
        result.CustoCreditos.Should().Be(10);
        result.TotalSourcesQueried.Should().Be(50);
        result.SuccessfulQueries.Should().Be(45);
        result.FailedQueries.Should().Be(5);
        result.FromCache.Should().BeTrue();
        result.ResultadoTransacao.Name.Should().Be("Success");
        result.BookPriceResult.Title.Should().Be("Test");
    }

    [Fact]
    public void SearchResult_CanAddErrors()
    {
        // Arrange
        var result = new SearchResult();

        // Act
        result.Errors.Add("Error 1");
        result.Errors.Add("Error 2");

        // Assert
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain("Error 1");
        result.Errors.Should().Contain("Error 2");
    }

    [Fact]
    public void SearchResult_CanAddAllResults()
    {
        // Arrange
        var result = new SearchResult();
        var bookResults = new List<BookPriceResult>
        {
            new() { Title = "Book 1", Price = 10m },
            new() { Title = "Book 2", Price = 20m },
            new() { Title = "Book 3", Price = 30m }
        };

        // Act
        result.AllResults.AddRange(bookResults);

        // Assert
        result.AllResults.Should().HaveCount(3);
    }
}
