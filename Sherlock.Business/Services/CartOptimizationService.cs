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
    private readonly IQueryHistoryService _queryHistoryService;
    private readonly ILogger<CartOptimizationService> _logger;

    public CartOptimizationService(
        W16Engine engine,
        CartOptimizer optimizer,
        IQueryHistoryService queryHistoryService,
        ILogger<CartOptimizationService> logger)
    {
        _engine = engine;
        _optimizer = optimizer;
        _queryHistoryService = queryHistoryService;
        _logger = logger;
    }

    public async Task<CartOptimizationResult> OptimizeCartAsync(
        CartOptimizationRequest request,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Iniciando otimização de carrinho com {BookCount} livros para usuário {UserId}",
            request.Books.Count, userId);

        // Nota: Transactions NÃO são cacheadas. Apenas as Queries individuais são cacheadas pelo W16Engine.
        // Uma transação de carrinho com 3 livros em 10 providers = 30 queries (cada uma pode ser cacheada)

        // Busca preços para todos os livros
        var allPrices = new List<BookPriceOption>();
        var searchTasks = new List<Task<(CartBookItem book, SearchResult result)>>();
        int creditsUsed = 0;

        // Determina quais providers usar
        var sourcesToSearch = GetSourcesToSearch(request.ProviderUrls);

        // Mais de 1 livro = carrinho; 1 livro = consulta unitária (mesmo serviço atende ambos)
        var isCart = request.Books.Count > 1;

        foreach (var book in request.Books)
        {
            searchTasks.Add(SearchBookPricesAsync(book, sourcesToSearch, userId, isCart, cancellationToken));
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
                        BookTitle = priceResult.Title ?? book.Isbn,
                        Isbn = book.Isbn,
                        Author = priceResult.Author,
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
                    p.Isbn == book.Isbn &&
                    p.ProviderName == result.BookPriceResult.Website);

                if (existing == null)
                {
                    allPrices.Add(new BookPriceOption
                    {
                        BookTitle = result.BookPriceResult.Title ?? book.Isbn,
                        Isbn = book.Isbn,
                        Author = result.BookPriceResult.Author,
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

        // Conta total de queries executadas
        int totalQueries = searchResults.Sum(r => r.result.TotalSourcesQueried);

        _logger.LogInformation(
            "Encontrados {PriceCount} preços para {BookCount} livros ({TotalQueries} queries executadas)",
            allPrices.Count, request.Books.Count, totalQueries);

        // Executa otimização
        var optimizationResult = _optimizer.Optimize(allPrices, request);
        optimizationResult.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
        optimizationResult.CreditsUsed = creditsUsed;
        optimizationResult.TotalQueriesExecuted = totalQueries;

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
        int userId,
        bool isCart,
        CancellationToken cancellationToken)
    {
        var requestor = new Requestor
        {
            SearchParameters = new SearchParameter
            {
                Isbn = book.Isbn,
                IsCart = isCart
            },
            SourcesToSearch = sources
        };

        var result = await _engine.ExecuteTransaction(requestor, userId, cancellationToken);
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

}
