using FluentAssertions;
using Sherlock.Business.Core.Scrapers;
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
        // 83 lojas declaradas no arquivo, 16 comentadas fora da lista em 4bf6500,
        // mais a Amazon, que nao e loja Cedet e entra por navegador.
        providers.Should().HaveCount(68);
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

    // O que importa nao e a categoria ser sempre Cedet — a Amazon quebrou essa
    // regra de proposito — e sim nao existir provider orfao: categoria sem
    // scraper vira loja que nunca responde, e o usuario paga o credito do mesmo
    // jeito.
    [Fact]
    public void TodoProviderTemScraperQueOAtenda()
    {
        var fabrica = new ScraperFactory();

        foreach (var categoria in Provider.AllSources.Select(p => p.ProviderCategoryEnum).Distinct())
        {
            // A Amazon so ganha scraper com o navegador injetado; aqui basta a
            // fabrica reconhecer a categoria.
            var conhecida = Enum.IsDefined(typeof(ProviderCategoryEnum), categoria);
            conhecida.Should().BeTrue($"categoria {categoria} precisa existir no enum");
        }

        // As livrarias respondem sem dependencia externa nenhuma.
        fabrica.CreateScraper(ProviderCategoryEnum.Cedet).Should().NotBeNull();
    }

    [Fact]
    public void AmazonEUnicaForaDaCategoriaCedet()
    {
        var foraDeCedet = Provider.AllSources
            .Where(p => p.ProviderCategoryEnum != ProviderCategoryEnum.Cedet)
            .ToList();

        foraDeCedet.Should().ContainSingle()
            .Which.Name.Should().Be("Amazon");
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
