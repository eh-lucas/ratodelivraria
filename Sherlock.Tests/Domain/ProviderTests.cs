using FluentAssertions;
using Sherlock.Domain.Entities;

namespace Sherlock.Tests.Domain;

public class ProviderTests
{
    [Fact]
    public void AllSources_ContainsProviders()
    {
        // Act
        var providers = Provider.AllSources;

        // Assert
        providers.Should().NotBeEmpty();
        providers.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void AllSources_ContainsExpectedProviderCount()
    {
        // Act
        var providers = Provider.AllSources;

        // Assert
        providers.Should().HaveCount(93);
    }

    [Fact]
    public void AllProviders_HaveRequiredProperties()
    {
        // Act
        var providers = Provider.AllSources;

        // Assert
        foreach (var provider in providers)
        {
            provider.Id.Should().BeGreaterThan(0);
            provider.Name.Should().NotBeNullOrEmpty();
            provider.Url.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public void AllProviders_AreCedetCategory()
    {
        // Act
        var providers = Provider.AllSources;

        // Assert
        providers.Should().AllSatisfy(p =>
            p.ProviderCategoryEnum.Should().Be(ProviderCategoryEnum.Cedet));
    }

    [Fact]
    public void AllProviders_HaveUniqueIds()
    {
        // Act
        var providers = Provider.AllSources;
        var ids = providers.Select(p => p.Id).ToList();

        // Assert
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AllProviders_HaveValidUrls()
    {
        // Act
        var providers = Provider.AllSources;

        // Assert
        foreach (var provider in providers)
        {
            provider.Url.Should().StartWith("https://");
        }
    }

}
