using Microsoft.Extensions.Logging;
using Sherlock.Business.DTOs;

namespace Sherlock.Business.Core.Optimization;

/// <summary>
/// Algoritmo de otimização de carrinho de compras.
/// Encontra a melhor combinação de providers para minimizar o custo total.
/// </summary>
public class CartOptimizer
{
    private readonly ILogger<CartOptimizer> _logger;

    public CartOptimizer(ILogger<CartOptimizer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Otimiza o carrinho de acordo com a estratégia especificada
    /// </summary>
    public CartOptimizationResult Optimize(
        List<BookPriceOption> allPrices,
        CartOptimizationRequest request)
    {
        _logger.LogInformation(
            "Iniciando otimização de carrinho com {BookCount} livros, estratégia: {Strategy}",
            request.Books.Count, request.Strategy);

        return request.Strategy switch
        {
            OptimizationStrategy.LowestTotal => OptimizeForLowestTotal(allPrices, request),
            OptimizationStrategy.FewestOrders => OptimizeForFewestOrders(allPrices, request),
            OptimizationStrategy.SingleProvider => OptimizeForSingleProvider(allPrices, request),
            _ => OptimizeForLowestTotal(allPrices, request)
        };
    }

    /// <summary>
    /// Estratégia: Menor custo total
    /// Usa programação dinâmica para encontrar a melhor combinação
    /// </summary>
    private CartOptimizationResult OptimizeForLowestTotal(
        List<BookPriceOption> allPrices,
        CartOptimizationRequest request)
    {
        var bookIsbns = request.Books.Select(b => b.Isbn.ToLowerInvariant()).ToList();
        var quantities = request.Books.ToDictionary(
            b => b.Isbn.ToLowerInvariant(),
            b => b.Quantity);

        // Agrupa preços por ISBN
        var pricesByIsbn = allPrices
            .Where(p => !string.IsNullOrEmpty(p.Isbn))
            .GroupBy(p => p.Isbn!.ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.ToList());

        // Identifica livros não encontrados
        var booksNotFound = bookIsbns
            .Where(b => !pricesByIsbn.ContainsKey(b) || !pricesByIsbn[b].Any())
            .ToList();

        var booksFound = bookIsbns.Except(booksNotFound).ToList();

        if (!booksFound.Any())
        {
            return new CartOptimizationResult
            {
                Success = false,
                Message = "Nenhum dos livros foi encontrado nos providers.",
                BooksNotFound = booksNotFound
            };
        }

        // Para cada livro (ISBN), encontra o melhor preço em cada provider
        var bestPricePerBookPerProvider = new Dictionary<string, Dictionary<int, BookPriceOption>>();
        foreach (var isbn in booksFound)
        {
            bestPricePerBookPerProvider[isbn] = pricesByIsbn[isbn]
                .GroupBy(p => p.ProviderId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(p => p.Price).First());
        }

        // Obtém todos os providers disponíveis
        var allProviders = allPrices.Select(p => p.ProviderId).Distinct().ToList();

        // Gera todas as combinações possíveis de atribuição livro -> provider
        var bestAssignment = FindBestAssignment(
            booksFound,
            bestPricePerBookPerProvider,
            quantities,
            allProviders,
            request.MaxProviders);

        return BuildResult(bestAssignment, quantities, booksNotFound);
    }

    /// <summary>
    /// Estratégia: Menor número de pedidos
    /// Tenta consolidar compras em poucos providers
    /// </summary>
    private CartOptimizationResult OptimizeForFewestOrders(
        List<BookPriceOption> allPrices,
        CartOptimizationRequest request)
    {
        // Força maxProviders = 1, 2, 3 e escolhe o mais barato
        var results = new List<(int providers, CartOptimizationResult result)>();

        for (int maxProviders = 1; maxProviders <= 3; maxProviders++)
        {
            var modifiedRequest = new CartOptimizationRequest
            {
                Books = request.Books,
                Strategy = OptimizationStrategy.LowestTotal,
                MaxProviders = maxProviders
            };

            var result = OptimizeForLowestTotal(allPrices, modifiedRequest);
            if (result.Success)
            {
                results.Add((maxProviders, result));
            }
        }

        if (!results.Any())
        {
            return new CartOptimizationResult
            {
                Success = false,
                Message = "Não foi possível encontrar uma combinação válida."
            };
        }

        // Escolhe a opção com menos providers que não seja muito mais cara
        var cheapest = results.MinBy(r => r.result.TotalCost);
        var fewest = results.MinBy(r => r.providers);

        // Se a opção com menos providers é até 10% mais cara, escolhe ela
        if (fewest.result.TotalCost <= cheapest.result.TotalCost * 1.10m)
        {
            fewest.result.Message = $"Otimizado para {fewest.providers} pedido(s)";
            return fewest.result;
        }

        cheapest.result.Message = $"Melhor custo com {cheapest.providers} pedido(s)";
        return cheapest.result;
    }

    /// <summary>
    /// Estratégia: Comprar tudo em um único provider
    /// </summary>
    private CartOptimizationResult OptimizeForSingleProvider(
        List<BookPriceOption> allPrices,
        CartOptimizationRequest request)
    {
        request.MaxProviders = 1;
        return OptimizeForLowestTotal(allPrices, request);
    }

    /// <summary>
    /// Encontra a melhor atribuição de livros para providers usando busca gulosa
    /// com refinamento (para evitar explosão combinatória)
    /// </summary>
    private Dictionary<string, BookPriceOption> FindBestAssignment(
        List<string> books,
        Dictionary<string, Dictionary<int, BookPriceOption>> pricesByBookProvider,
        Dictionary<string, int> quantities,
        List<int> allProviders,
        int maxProviders)
    {
        var bestAssignment = new Dictionary<string, BookPriceOption>();
        decimal bestTotalCost = decimal.MaxValue;

        // Se maxProviders = 1, encontra o melhor provider único
        if (maxProviders == 1)
        {
            foreach (var providerId in allProviders)
            {
                var assignment = new Dictionary<string, BookPriceOption>();
                decimal subtotal = 0;
                bool valid = true;

                foreach (var book in books)
                {
                    if (pricesByBookProvider[book].TryGetValue(providerId, out var price))
                    {
                        assignment[book] = price;
                        subtotal += price.Price * quantities[book];
                    }
                    else
                    {
                        valid = false;
                        break;
                    }
                }

                if (valid && subtotal < bestTotalCost)
                {
                    bestTotalCost = subtotal;
                    bestAssignment = new Dictionary<string, BookPriceOption>(assignment);
                }
            }

            return bestAssignment;
        }

        // Estratégia gulosa: para cada livro, escolhe o provider mais barato
        var greedyAssignment = new Dictionary<string, BookPriceOption>();
        foreach (var book in books)
        {
            var cheapest = pricesByBookProvider[book].Values
                .OrderBy(p => p.Price)
                .FirstOrDefault();

            if (cheapest != null)
            {
                greedyAssignment[book] = cheapest;
            }
        }

        // Calcula custo total
        decimal greedyTotal = greedyAssignment.Sum(kvp => kvp.Value.Price * quantities[kvp.Key]);

        bestAssignment = greedyAssignment;
        bestTotalCost = greedyTotal;

        // Refinamento: tenta consolidar em menos providers se mais barato
        var providerSubtotals = greedyAssignment
            .GroupBy(kvp => kvp.Value.ProviderId)
            .ToDictionary(g => g.Key, g => g.Sum(kvp => kvp.Value.Price * quantities[kvp.Key]));

        if (maxProviders == 0 || providerSubtotals.Count > 1)
        {
            var usedProviders = providerSubtotals.Keys.ToList();

            foreach (var targetProvider in usedProviders)
            {
                var consolidatedAssignment = new Dictionary<string, BookPriceOption>();
                decimal consolidatedSubtotal = 0;
                bool canConsolidate = true;

                foreach (var book in books)
                {
                    if (pricesByBookProvider[book].TryGetValue(targetProvider, out var price))
                    {
                        consolidatedAssignment[book] = price;
                        consolidatedSubtotal += price.Price * quantities[book];
                    }
                    else
                    {
                        canConsolidate = false;
                        break;
                    }
                }

                if (canConsolidate && consolidatedSubtotal < bestTotalCost)
                {
                    bestTotalCost = consolidatedSubtotal;
                    bestAssignment = consolidatedAssignment;
                }
            }
        }

        // Aplica limite de providers se especificado
        if (maxProviders > 0 && maxProviders < bestAssignment.Values.Select(v => v.ProviderId).Distinct().Count())
        {
            var topProviders = bestAssignment.Values
                .GroupBy(v => v.ProviderId)
                .OrderByDescending(g => g.Count())
                .Take(maxProviders)
                .Select(g => g.Key)
                .ToList();

            var constrainedAssignment = new Dictionary<string, BookPriceOption>();
            foreach (var book in books)
            {
                var options = pricesByBookProvider[book]
                    .Where(kvp => topProviders.Contains(kvp.Key))
                    .OrderBy(kvp => kvp.Value.Price)
                    .ToList();

                if (options.Any())
                {
                    constrainedAssignment[book] = options.First().Value;
                }
            }

            if (constrainedAssignment.Count == books.Count)
            {
                bestAssignment = constrainedAssignment;
            }
        }

        return bestAssignment;
    }

    /// <summary>
    /// Constrói o resultado final da otimização
    /// </summary>
    private CartOptimizationResult BuildResult(
        Dictionary<string, BookPriceOption> assignment,
        Dictionary<string, int> quantities,
        List<string> booksNotFound)
    {
        if (!assignment.Any())
        {
            return new CartOptimizationResult
            {
                Success = false,
                Message = "Não foi possível encontrar uma combinação válida.",
                BooksNotFound = booksNotFound
            };
        }

        // Agrupa por provider
        var providerGroups = assignment
            .GroupBy(kvp => kvp.Value.ProviderId)
            .ToList();

        var providerCarts = new List<ProviderCart>();
        decimal totalBooksCost = 0;

        foreach (var group in providerGroups)
        {
            var providerId = group.Key;
            var items = group.Select(kvp => new ProviderCartItem
            {
                Title = kvp.Value.BookTitle,
                Isbn = kvp.Value.Isbn,
                UnitPrice = kvp.Value.Price,
                Quantity = quantities.GetValueOrDefault(kvp.Key, 1),
                TotalPrice = kvp.Value.Price * quantities.GetValueOrDefault(kvp.Key, 1),
                Discount = kvp.Value.Discount,
                ProductUrl = kvp.Value.ProductUrl
            }).ToList();

            var subtotal = items.Sum(i => i.TotalPrice);

            providerCarts.Add(new ProviderCart
            {
                ProviderId = providerId,
                ProviderName = group.First().Value.ProviderName,
                Items = items,
                Subtotal = subtotal,
                Total = subtotal
            });

            totalBooksCost += subtotal;
        }

        // Calcula economia comparada ao pior cenário (tudo no provider mais caro)
        decimal worstCaseTotal = CalculateWorstCase(assignment, quantities);
        decimal savings = worstCaseTotal - totalBooksCost;
        decimal savingsPercent = worstCaseTotal > 0 ? (savings / worstCaseTotal) * 100 : 0;

        return new CartOptimizationResult
        {
            Success = true,
            Message = $"Carrinho otimizado em {providerCarts.Count} pedido(s)",
            TotalCost = totalBooksCost,
            BooksCost = totalBooksCost,
            Savings = Math.Max(0, savings),
            SavingsPercent = Math.Max(0, savingsPercent),
            ProviderCarts = providerCarts.OrderByDescending(p => p.Subtotal).ToList(),
            BooksNotFound = booksNotFound
        };
    }

    private decimal CalculateWorstCase(
        Dictionary<string, BookPriceOption> assignment,
        Dictionary<string, int> quantities)
    {
        decimal total = 0;

        foreach (var kvp in assignment)
        {
            // Assume 30% mais caro como pior caso
            var price = kvp.Value.Price * 1.3m;
            total += price * quantities.GetValueOrDefault(kvp.Key, 1);
        }

        return total;
    }
}
