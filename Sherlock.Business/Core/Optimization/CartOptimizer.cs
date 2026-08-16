using Microsoft.Extensions.Logging;
using Sherlock.Business.DTOs;

namespace Sherlock.Business.Core.Optimization;

/// <summary>
/// Algoritmo de otimização de carrinho de compras.
/// Encontra o melhor provider para comprar todos os livros (ou o maior número possível).
/// </summary>
public class CartOptimizer
{
    private readonly ILogger<CartOptimizer> _logger;

    public CartOptimizer(ILogger<CartOptimizer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Otimiza o carrinho: encontra o melhor provider único que tem todos os livros
    /// pelo menor preço total
    /// </summary>
    public CartOptimizationResult Optimize(
        List<BookPriceOption> allPrices,
        CartOptimizationRequest request)
    {
        var totalBooks = request.Books.Count;
        var requestedIsbns = request.Books
            .Select(b => b.Isbn.ToLowerInvariant())
            .Distinct()
            .ToList();

        var quantities = request.Books
            .GroupBy(b => b.Isbn.ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.Sum(b => b.Quantity));

        _logger.LogInformation(
            "Iniciando otimização de carrinho com {BookCount} livros únicos, estratégia: {Strategy}",
            requestedIsbns.Count, request.Strategy);

        // Agrupa preços por provider
        var pricesByProvider = allPrices
            .Where(p => !string.IsNullOrEmpty(p.Isbn) && p.Price > 0)
            .GroupBy(p => p.ProviderName)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Calcula comparação de cada provider
        var providerComparisons = new List<ProviderComparison>();

        foreach (var (providerName, prices) in pricesByProvider)
        {
            var comparison = CalculateProviderComparison(
                providerName,
                prices,
                requestedIsbns,
                quantities);

            providerComparisons.Add(comparison);
        }

        // Ordena: primeiro os que têm todos os livros (pelo menor total), depois os parciais
        providerComparisons = providerComparisons
            .OrderByDescending(p => p.HasAllBooks)
            .ThenBy(p => p.TotalPrice)
            .ThenByDescending(p => p.BooksFound)
            .ToList();

        // Identifica livros não encontrados em NENHUM provider
        var allFoundIsbns = allPrices
            .Where(p => !string.IsNullOrEmpty(p.Isbn))
            .Select(p => p.Isbn!.ToLowerInvariant())
            .Distinct()
            .ToHashSet();

        var booksNotFound = requestedIsbns
            .Where(isbn => !allFoundIsbns.Contains(isbn))
            .ToList();

        // Seleciona o melhor provider
        var bestProvider = providerComparisons.FirstOrDefault();

        if (bestProvider == null)
        {
            return new CartOptimizationResult
            {
                Success = false,
                Message = "Nenhum provider encontrado com os livros solicitados.",
                BooksNotFound = requestedIsbns,
                ProviderComparisons = new List<ProviderComparison>(),
                TotalBooksRequested = requestedIsbns.Count
            };
        }

        // Constrói o resultado
        var result = BuildResult(bestProvider, quantities, booksNotFound, requestedIsbns.Count);
        result.ProviderComparisons = providerComparisons;
        result.TotalBooksRequested = requestedIsbns.Count;

        // Calcula economia
        if (providerComparisons.Count > 1)
        {
            var providersWithAllBooks = providerComparisons.Where(p => p.HasAllBooks).ToList();
            if (providersWithAllBooks.Count > 1)
            {
                var worstPrice = providersWithAllBooks.Max(p => p.TotalPrice);
                result.Savings = worstPrice - bestProvider.TotalPrice;
                result.SavingsPercent = worstPrice > 0
                    ? (result.Savings / worstPrice) * 100
                    : 0;
            }
        }

        return result;
    }

    private ProviderComparison CalculateProviderComparison(
        string providerName,
        List<BookPriceOption> prices,
        List<string> requestedIsbns,
        Dictionary<string, int> quantities)
    {
        var firstPrice = prices.First();
        var comparison = new ProviderComparison
        {
            ProviderId = firstPrice.ProviderId,
            ProviderName = providerName,
            ProviderUrl = firstPrice.ProviderUrl,
            TotalBooksRequested = requestedIsbns.Count
        };

        // Para cada ISBN requisitado, encontra o melhor preço neste provider
        var foundIsbns = new HashSet<string>();
        decimal totalPrice = 0;

        foreach (var isbn in requestedIsbns)
        {
            var bookPrice = prices
                .Where(p => p.Isbn?.ToLowerInvariant() == isbn)
                .OrderBy(p => p.Price)
                .FirstOrDefault();

            if (bookPrice != null)
            {
                foundIsbns.Add(isbn);
                var qty = quantities.GetValueOrDefault(isbn, 1);
                var itemTotal = bookPrice.Price * qty;
                totalPrice += itemTotal;

                comparison.BookPrices.Add(new BookPriceDetail
                {
                    Isbn = isbn,
                    Title = bookPrice.BookTitle,
                    Price = bookPrice.Price,
                    Quantity = qty,
                    ProductUrl = bookPrice.ProductUrl
                });
            }
        }

        comparison.BooksFound = foundIsbns.Count;
        comparison.TotalPrice = totalPrice;
        comparison.MissingIsbns = requestedIsbns
            .Where(isbn => !foundIsbns.Contains(isbn))
            .ToList();

        return comparison;
    }

    private CartOptimizationResult BuildResult(
        ProviderComparison bestProvider,
        Dictionary<string, int> quantities,
        List<string> booksNotFound,
        int totalBooksRequested)
    {
        // Constrói ProviderCart do melhor provider
        var providerCart = new ProviderCart
        {
            ProviderId = bestProvider.ProviderId,
            ProviderName = bestProvider.ProviderName,
            ProviderUrl = bestProvider.ProviderUrl,
            Items = bestProvider.BookPrices.Select(bp => new ProviderCartItem
            {
                Title = bp.Title,
                Isbn = bp.Isbn,
                UnitPrice = bp.Price,
                Quantity = bp.Quantity,
                TotalPrice = bp.TotalPrice,
                ProductUrl = bp.ProductUrl
            }).ToList(),
            Subtotal = bestProvider.TotalPrice,
            Total = bestProvider.TotalPrice
        };

        var allBooksFound = bestProvider.HasAllBooks && booksNotFound.Count == 0;

        return new CartOptimizationResult
        {
            Success = true,
            Message = allBooksFound
                ? $"Melhor opção: {bestProvider.ProviderName} com todos os {totalBooksRequested} livros"
                : $"Melhor opção: {bestProvider.ProviderName} com {bestProvider.BooksFound}/{totalBooksRequested} livros",
            TotalCost = bestProvider.TotalPrice,
            BooksCost = bestProvider.TotalPrice,
            ProviderCarts = new List<ProviderCart> { providerCart },
            BooksNotFound = booksNotFound.Concat(bestProvider.MissingIsbns).Distinct().ToList(),
            TotalBooksRequested = totalBooksRequested
        };
    }
}
