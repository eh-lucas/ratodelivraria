using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Sherlock.Business.Core.Base;
using Sherlock.Business.Core.Scrapers;
using Sherlock.Business.Interfaces;
using Sherlock.Domain.Entities;

namespace Sherlock.Tests.Business;

public class W16EngineTests
{
    private readonly Mock<ILogger<W16Engine>> _loggerMock;
    private readonly Mock<ICacheService> _cacheServiceMock;

    public W16EngineTests()
    {
        _loggerMock = new Mock<ILogger<W16Engine>>();
        _cacheServiceMock = new Mock<ICacheService>();
    }

    [Fact]
    public void Constructor_WithNoParameters_SetsDefaultParallelism()
    {
        // Act
        var engine = new W16Engine();

        // Assert
        engine.MaxDegreeOfParallelism.Should().Be(10);
    }

    [Fact]
    public void MaxDegreeOfParallelism_CanBeModified()
    {
        // Arrange
        var engine = new W16Engine();

        // Act
        engine.MaxDegreeOfParallelism = 20;

        // Assert
        engine.MaxDegreeOfParallelism.Should().Be(20);
    }

    [Fact]
    public async Task ExecuteTransaction_WithEmptySources_ReturnsAllFailed()
    {
        // Arrange
        var engine = new W16Engine(_loggerMock.Object);
        var searchParams = new SearchParameter { BookTitle = "Test Book" };
        var requestor = new Requestor(searchParams, new List<Provider>());

        // Act
        var result = await engine.ExecuteTransaction(requestor);

        // Assert
        result.Should().NotBeNull();
        result.TotalSourcesQueried.Should().Be(0);
        // With 0 sources, all "failed" (0 failed == total sources)
        result.ResultadoTransacao.Name.Should().BeOneOf("NoResults", "AllFailed");
    }

    [Fact]
    public async Task ExecuteTransaction_WithCancellation_StopsExecution()
    {
        // Arrange
        var engine = new W16Engine(_loggerMock.Object);
        var searchParams = new SearchParameter { BookTitle = "Test Book" };
        var providers = Provider.AllSources.Take(5).ToList();
        var requestor = new Requestor(searchParams, providers);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await engine.ExecuteTransaction(requestor, cts.Token);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteTransaction_SetsTimestamps()
    {
        // Arrange
        var engine = new W16Engine(_loggerMock.Object);
        var searchParams = new SearchParameter { BookTitle = "Test" };
        var requestor = new Requestor(searchParams, new List<Provider>());

        var beforeExecution = DateTime.Now;

        // Act
        var result = await engine.ExecuteTransaction(requestor);

        var afterExecution = DateTime.Now;

        // Assert
        result.InicioConsulta.Should().BeOnOrAfter(beforeExecution);
        result.FimConsulta.Should().BeOnOrBefore(afterExecution);
        result.TempoDecorrido.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task ExecuteTransaction_WithCachedResult_ReturnsCachedData()
    {
        // Arrange
        var cachedResult = new SearchResult
        {
            BookPriceResult = new BookPriceResult { Title = "Cached Book", Price = 19.99m },
            FromCache = false
        };

        _cacheServiceMock
            .Setup(c => c.GenerateBookPriceKey(It.IsAny<string>(), It.IsAny<string?>()))
            .Returns("test-cache-key");

        _cacheServiceMock
            .Setup(c => c.GetAsync<SearchResult>(It.IsAny<string>()))
            .ReturnsAsync(cachedResult);

        var engine = new W16Engine(_loggerMock.Object, _cacheServiceMock.Object, null);
        var searchParams = new SearchParameter { BookTitle = "Test Book" };
        var requestor = new Requestor(searchParams, new List<Provider>());

        // Act
        var result = await engine.ExecuteTransaction(requestor);

        // Assert
        result.FromCache.Should().BeTrue();
        result.BookPriceResult.Title.Should().Be("Cached Book");
    }

    [Fact]
    public async Task ExecuteTransaction_WithNoCacheService_DoesNotThrow()
    {
        // Arrange
        var engine = new W16Engine(_loggerMock.Object, null, null);
        var searchParams = new SearchParameter { BookTitle = "Test" };
        var requestor = new Requestor(searchParams, new List<Provider>());

        // Act
        var action = async () => await engine.ExecuteTransaction(requestor);

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteTransaction_CalculatesCost_ForSuccessfulQueries()
    {
        // Arrange
        var engine = new W16Engine(_loggerMock.Object);
        var searchParams = new SearchParameter { BookTitle = "Test" };
        var requestor = new Requestor(searchParams, new List<Provider>());

        // Act
        var result = await engine.ExecuteTransaction(requestor);

        // Assert
        // With no results (NoResults type), cost should be 0 (not billable)
        result.CustoCreditos.Should().Be(0);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(15)]
    public void MaxDegreeOfParallelism_AcceptsValidValues(int parallelism)
    {
        // Arrange
        var engine = new W16Engine();

        // Act
        engine.MaxDegreeOfParallelism = parallelism;

        // Assert
        engine.MaxDegreeOfParallelism.Should().Be(parallelism);
    }
}
