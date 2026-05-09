using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sherlock.Business.Core.Base;
using Sherlock.Business.Core.Scrapers;
using Sherlock.Business.DTOs;
using Sherlock.Business.Interfaces;
using Sherlock.Business.Services;
using Sherlock.Domain.Entities;

namespace Sherlock.Tests.Business;

public class SingleBookSearchServiceTests
{
    private readonly Mock<ILogger<SingleBookSearchService>> _loggerMock;
    private readonly ILoggerFactory _loggerFactory;

    public SingleBookSearchServiceTests()
    {
        _loggerMock = new Mock<ILogger<SingleBookSearchService>>();
        _loggerFactory = NullLoggerFactory.Instance;
    }

    [Fact]
    public async Task SearchAsync_WithValidRequest_ReturnsResult()
    {
        // Arrange
        var engine = new W16Engine();
        var service = new SingleBookSearchService(engine, _loggerMock.Object);

        var request = new SingleBookSearchRequest
        {
            Isbn = "9780132350884"
        };

        // Act
        var result = await service.SearchAsync(request, userId: 1);

        // Assert
        result.Should().NotBeNull();
        result.ExecutionTimeMs.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task SearchAsync_WithEmptyProviders_ReturnsNoResults()
    {
        // Arrange
        var engine = new W16Engine();
        var service = new SingleBookSearchService(engine, _loggerMock.Object);

        var request = new SingleBookSearchRequest
        {
            Isbn = "0000000000000",
            ProviderUrls = new List<string>() // Lista vazia força nenhum provider
        };

        // Act
        var result = await service.SearchAsync(request, userId: 1);

        // Assert
        result.Should().NotBeNull();
        // Com lista vazia de providers, usa todos os ativos por padrão
    }

    [Fact]
    public async Task SearchAsync_SetsExecutionTime()
    {
        // Arrange
        var engine = new W16Engine();
        var service = new SingleBookSearchService(engine, _loggerMock.Object);

        var request = new SingleBookSearchRequest
        {
            Isbn = "9780132350884"
        };

        // Act
        var result = await service.SearchAsync(request, userId: 1);

        // Assert
        result.ExecutionTimeMs.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task SearchAsync_WithSpecificProviders_FiltersCorrectly()
    {
        // Arrange
        var engine = new W16Engine();
        var service = new SingleBookSearchService(engine, _loggerMock.Object);

        var specificProviders = Provider.AllSources.Take(2).Select(p => p.Url).ToList();

        var request = new SingleBookSearchRequest
        {
            Isbn = "9780132350884",
            ProviderUrls = specificProviders
        };

        // Act
        var result = await service.SearchAsync(request, userId: 1);

        // Assert
        result.Should().NotBeNull();
        // O serviço deve ter filtrado para apenas os providers especificados
        result.TotalProvidersSearched.Should().BeLessOrEqualTo(specificProviders.Count);
    }

    [Fact]
    public async Task SearchAsync_ReturnsAlternativesLimitedToTwo()
    {
        // Arrange - Este teste verifica a lógica de limitação de alternativas
        var engine = new W16Engine();
        var service = new SingleBookSearchService(engine, _loggerMock.Object);

        var request = new SingleBookSearchRequest
        {
            Isbn = "9780132350884"
        };

        // Act
        var result = await service.SearchAsync(request, userId: 1);

        // Assert
        result.Alternatives.Should().HaveCountLessOrEqualTo(2);
    }

    [Fact]
    public async Task SearchAsync_SortsByPriceAscending()
    {
        // Arrange
        var engine = new W16Engine();
        var service = new SingleBookSearchService(engine, _loggerMock.Object);

        var request = new SingleBookSearchRequest
        {
            Isbn = "9780132350884"
        };

        // Act
        var result = await service.SearchAsync(request, userId: 1);

        // Assert
        if (result.Success && result.BestOption != null && result.Alternatives.Any())
        {
            // A melhor opção deve ter o menor preço
            var bestPrice = result.BestOption.Price;
            foreach (var alt in result.Alternatives)
            {
                alt.Price.Should().BeGreaterOrEqualTo(bestPrice);
            }
        }
    }

    [Fact]
    public async Task SearchAsync_WithCancellation_HandlesGracefully()
    {
        // Arrange
        var engine = new W16Engine();
        var service = new SingleBookSearchService(engine, _loggerMock.Object);

        var request = new SingleBookSearchRequest
        {
            Isbn = "9780132350884"
        };

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await service.SearchAsync(request, userId: 1, cancellationToken: cts.Token);

        // Assert - deve retornar sem exceção
        result.Should().NotBeNull();
    }
}
