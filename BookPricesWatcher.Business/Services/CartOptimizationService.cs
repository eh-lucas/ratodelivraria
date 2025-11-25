using Microsoft.Extensions.Logging;
using Sherlock.Business.Core.Base;
using Sherlock.Business.Core.Optimization;
using Sherlock.Business.Core.Scrapers;
using Sherlock.Business.DTOs;
using Sherlock.Business.Interfaces;
using Sherlock.Domain.Entities;
using System.Diagnostics;

namespace Sherlock.Business.Services;

public class CartOptimizationService : ICartOptimizationService
{
    private readonly W16Engine _engine;
    private readonly CartOptimizer _optimizer;
    private readonly ICacheService _cacheService;
    private readonly IQueryHistoryService _queryHistoryService;
    private readonly ILogger<CartOptimizationService> _logger;

    private static readonly TimeSpan CartCacheDuration = TimeSpan.FromHours(1);

    public CartOptimizationService(
        W16Engine engine,
        CartOptimizer optimizer,
        ICacheService cacheService,
        IQueryHistoryService queryHistoryService,
        ILogger<CartOptimizationService> logger)
    {
        _engine = engine;
        _optimizer = optimizer;
        _cacheService = cacheService;
        _queryHistoryService = queryHistoryService;
        _logger = logger;
    }

    public async Task<CartOptimizationResult> OptimizeCartAsync(
        CartOptimizationRequest request,
        int? userId = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Iniciando otimização de carrinho com {BookCount} livros para usuário {UserId}",
            request.Books.Count, userId ?? 0);

        // Verifica cache do carrinho completo
        var cacheKey = GenerateCartCacheKey(request);
        var cachedResult = await _cacheService.GetAsync<CartOptimizationResult>(cacheKey);
        if (cachedResult != null)
        {
            cachedResult.FromCache = true;
            cachedResult.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("Resultado de carrinho obtido do cache em {Elapsed}ms",
                stopwatch.ElapsedMilliseconds);

            return cachedResult;
        }

        // Busca preços para todos os livros
        var allPrices = new List<BookPriceOption>();
        var searchTasks = new List<Task<(CartBookItem book, SearchResult result)>>();
        int creditsUsed = 0;

        // Determina quais providers usar
        var sourcesToSearch = GetSourcesToSearch(request.ProviderUrls);

        foreach (var book in request.Books)
        {
            searchTasks.Add(SearchBookPricesAsync(book, sourcesToSearch, cancellationToken));
        }

        var searchResults = await Task.WhenAll(searchTasks);

        foreach (var (book, result) in searchResults)
        {
            creditsUsed += result.CustoCreditos;

            if (result.AllResults != null && result.AllResults.Any())
            {
                foreach (var priceResult in result.AllResults)
                {
                    allPrices.Add(new BookPriceOption
                    {
                        BookTitle = book.Title,
                        Isbn = book.Isbn,
                        ProviderId = GetProviderId(priceResult.Website),
                        ProviderName = priceResult.Website,
                        Price = priceResult.Price,
                        Discount = priceResult.Discount,
                        ProductUrl = null,
                        Available = true
                    });
                }
            }

            // Também adiciona o melhor resultado se AllResults estiver vazio
            if (result.BookPriceResult != null &&
                !string.IsNullOrEmpty(result.BookPriceResult.Title) &&
                result.BookPriceResult.Price > 0)
            {
                var existing = allPrices.FirstOrDefault(p =>
                    p.BookTitle.Equals(book.Title, StringComparison.OrdinalIgnoreCase) &&
                    p.ProviderName == result.BookPriceResult.Website);

                if (existing == null)
                {
                    allPrices.Add(new BookPriceOption
                    {
                        BookTitle = book.Title,
                        Isbn = book.Isbn,
                        ProviderId = GetProviderId(result.BookPriceResult.Website),
                        ProviderName = result.BookPriceResult.Website,
                        Price = result.BookPriceResult.Price,
                        Discount = result.BookPriceResult.Discount,
                        ProductUrl = null,
                        Available = true
                    });
                }
            }
        }

        _logger.LogInformation(
            "Encontrados {PriceCount} preços para {BookCount} livros",
            allPrices.Count, request.Books.Count);

        // Executa otimização
        var optimizationResult = _optimizer.Optimize(allPrices, request);
        optimizationResult.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
        optimizationResult.CreditsUsed = creditsUsed;

        // Cacheia resultado
        if (optimizationResult.Success)
        {
            await _cacheService.SetAsync(cacheKey, optimizationResult, CartCacheDuration);
        }

        stopwatch.Stop();

        _logger.LogInformation(
            "Otimização concluída em {Elapsed}ms. Total: R${Total:F2}, Economia: R${Savings:F2} ({SavingsPercent:F1}%)",
            stopwatch.ElapsedMilliseconds,
            optimizationResult.TotalCost,
            optimizationResult.Savings,
            optimizationResult.SavingsPercent);

        return optimizationResult;
    }

    private async Task<(CartBookItem book, SearchResult result)> SearchBookPricesAsync(
        CartBookItem book,
        List<Provider> sources,
        CancellationToken cancellationToken)
    {
        var requestor = new Requestor
        {
            SearchParameters = new SearchParameter
            {
                BookTitle = book.Title,
                Isbn = book.Isbn,
                AuthorName = book.Author
            },
            SourcesToSearch = sources
        };

        var result = await _engine.ExecuteTransaction(requestor, cancellationToken);
        return (book, result);
    }

    private List<Provider> GetSourcesToSearch(List<string>? providerUrls)
    {
        if (providerUrls != null && providerUrls.Count > 0)
        {
            var urls = providerUrls.ToHashSet();
            var filtered = Provider.AllSources
                .Where(p => urls.Contains(p.Url))
                .ToList();

            if (filtered.Count > 0)
            {
                return filtered;
            }
        }

        // Fallback: usa todos os providers ativos
        return Provider.AllSources
            .Where(s => s.IsActive)
            .Take(10) // Limita para não sobrecarregar
            .ToList();
    }

    private int GetProviderId(string providerName)
    {
        // Mapeamento simples - em produção, viria do banco
        return providerName?.ToLowerInvariant() switch
        {
            "amazon" => 1,
            "estante virtual" => 2,
            "livraria cultura" => 3,
            "cedet" => 4,
            _ => 0
        };
    }

    private string GenerateCartCacheKey(CartOptimizationRequest request)
    {
        var bookKeys = request.Books
            .OrderBy(b => b.Title)
            .Select(b => $"{b.Title.ToLowerInvariant()}:{b.Quantity}")
            .ToList();

        var providersKey = request.ProviderUrls != null && request.ProviderUrls.Count > 0
            ? string.Join(",", request.ProviderUrls.OrderBy(u => u))
            : "all";

        var hash = string.Join("|", bookKeys).GetHashCode();
        return $"cart:optimization:{hash}:{request.Strategy}:{request.MaxProviders}:{providersKey.GetHashCode()}";
    }
}
