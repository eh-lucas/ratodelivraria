using FluentAssertions;
using Sherlock.Business.Core.Base;
using Sherlock.Business.Core.Scrapers;
using Sherlock.Domain.Entities;

namespace Sherlock.Tests.Business;

public class RequestorTests
{
    [Fact]
    public void Constructor_WithSearchParameterOnly_UsesCedetProviders()
    {
        // Arrange
        var searchParams = new SearchParameter { BookTitle = "Test Book" };

        // Act
        var requestor = new Requestor(searchParams);

        // Assert
        requestor.SourcesToSearch.Should().NotBeEmpty();
        requestor.SourcesToSearch.Should().AllSatisfy(p =>
            p.ProviderCategoryEnum.Should().Be(ProviderCategoryEnum.Cedet));
    }

    [Fact]
    public void Constructor_WithSearchParameterOnly_SetsNullCacheTimeMinutes()
    {
        // Arrange
        var searchParams = new SearchParameter { BookTitle = "Test Book" };

        // Act
        var requestor = new Requestor(searchParams);

        // Assert - CacheTimeMinutes é null por default (usa valor do config)
        requestor.CacheTimeMinutes.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithCustomProviders_UsesProvidedList()
    {
        // Arrange
        var searchParams = new SearchParameter { BookTitle = "Test Book" };
        var customProviders = new List<Provider>
        {
            new() { Id = 1, Name = "Custom Provider", Url = "https://custom.com" }
        };

        // Act
        var requestor = new Requestor(searchParams, customProviders);

        // Assert
        requestor.SourcesToSearch.Should().HaveCount(1);
        requestor.SourcesToSearch.First().Name.Should().Be("Custom Provider");
    }

    [Fact]
    public void Constructor_WithCustomCacheTimeMinutes_UsesProvidedValue()
    {
        // Arrange
        var searchParams = new SearchParameter { BookTitle = "Test Book" };
        var providers = new List<Provider>();
        var cacheTimeMinutes = 60;

        // Act
        var requestor = new Requestor(searchParams, providers, cacheTimeMinutes);

        // Assert
        requestor.CacheTimeMinutes.Should().Be(cacheTimeMinutes);
    }

    [Fact]
    public void SearchParameters_ArePreserved()
    {
        // Arrange
        var searchParams = new SearchParameter
        {
            BookTitle = "Clean Code",
            AuthorName = "Robert Martin",
            Isbn = "9780132350884",
            IsExactSearch = true
        };

        // Act
        var requestor = new Requestor(searchParams);

        // Assert
        requestor.SearchParameters.BookTitle.Should().Be("Clean Code");
        requestor.SearchParameters.AuthorName.Should().Be("Robert Martin");
        requestor.SearchParameters.Isbn.Should().Be("9780132350884");
        requestor.SearchParameters.IsExactSearch.Should().BeTrue();
    }

    [Fact]
    public void Constructor_Default_HasNullProperties()
    {
        // Act
        var requestor = new Requestor();

        // Assert
        requestor.SearchParameters.Should().BeNull();
        requestor.SourcesToSearch.Should().BeNull();
    }
}
