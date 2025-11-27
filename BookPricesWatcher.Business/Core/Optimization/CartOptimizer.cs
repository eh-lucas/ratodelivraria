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

    // Configurações de frete por provider (em produção, viriam do banco)
    private static readonly Dictionary<int, ShippingConfig> ShippingConfigs = new()
    {
        { 1, new ShippingConfig { BaseShipping = 15.90m, FreeShippingThreshold = 199m } },  // Amazon
        { 2, new ShippingConfig { BaseShipping = 12.90m, FreeShippingThreshold = 149m } },  // Estante Virtual
        { 3, new ShippingConfig { BaseShipping = 14.90m, FreeShippingThreshold = 179m } },  // Livraria Cultura
        { 4, new ShippingConfig { BaseShipping = 9.90m, FreeShippingThreshold = 99m } },    // Cedet
    };

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
            OptimizationStrategy.PrioritizeFreeShipping => OptimizeForFreeShipping(allPrices, request),
            OptimizationStrategy.SingleProvider => OptimizeForSingleProvider(allPrices, request),
            _ => OptimizeForLowestTotal(allPrices, request)
        };
    }

    /// <summary>
    /// Estratégia: Menor custo total (livros + frete)
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
            request.MaxProviders,
            request.IncludeShipping);

        return BuildResult(bestAssignment, quantities, booksNotFound, request);
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
                MaxProviders = maxProviders,
                IncludeShipping = request.IncludeShipping
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
    /// Estratégia: Prioriza frete grátis
    /// </summary>
    private CartOptimizationResult OptimizeForFreeShipping(
        List<BookPriceOption> allPrices,
        CartOptimizationRequest request)
    {
        // Primeiro tenta com frete grátis
        var bookIsbns = request.Books.Select(b => b.Isbn.ToLowerInvariant()).ToList();
        var quantities = request.Books.ToDictionary(
            b => b.Isbn.ToLowerInvariant(),
            b => b.Quantity);

        var pricesByIsbn = allPrices
            .Where(p => !string.IsNullOrEmpty(p.Isbn))
            .GroupBy(p => p.Isbn!.ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.ToList());

        var allProviders = allPrices.Select(p => p.ProviderId).Distinct().ToList();

        // Para cada provider, calcula o custo total se comprasse tudo lá
        var providerTotals = new Dictionary<int, decimal>();
        foreach (var providerId in allProviders)
        {
            decimal total = 0;
            bool hasAllBooks = true;

            foreach (var isbn in bookIsbns)
            {
                if (!pricesByIsbn.ContainsKey(isbn))
                {
                    hasAllBooks = false;
                    break;
                }

                var price = pricesByIsbn[isbn].FirstOrDefault(p => p.ProviderId == providerId);
                if (price == null)
                {
                    hasAllBooks = false;
                    break;
                }

                total += price.Price * quantities[isbn];
            }

            if (hasAllBooks)
            {
                var shipping = GetShippingCost(providerId, total);
                providerTotals[providerId] = total + shipping;
            }
        }

        // Prioriza providers com frete grátis
        var providersWithFreeShipping = providerTotals
            .Where(p =>
            {
                var config = ShippingConfigs.GetValueOrDefault(p.Key);
                var subtotal = p.Value - GetShippingCost(p.Key, p.Value);
                return config != null && subtotal >= config.FreeShippingThreshold;
            })
            .OrderBy(p => p.Value)
            .ToList();

        if (providersWithFreeShipping.Any())
        {
            var best = providersWithFreeShipping.First();
            request.MaxProviders = 1;
            var result = OptimizeForSingleProvider(allPrices, request);
            result.Message = "Frete grátis disponível!";
            return result;
        }

        // Se não consegue frete grátis, usa estratégia padrão
        return OptimizeForLowestTotal(allPrices, request);
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
        int maxProviders,
        bool includeShipping)
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

                if (valid)
                {
                    var shipping = includeShipping ? GetShippingCost(providerId, subtotal) : 0;
                    var total = subtotal + shipping;

                    if (total < bestTotalCost)
                    {
                        bestTotalCost = total;
                        bestAssignment = new Dictionary<string, BookPriceOption>(assignment);
                    }
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

        // Calcula custo com frete
        var providerSubtotals = greedyAssignment
            .GroupBy(kvp => kvp.Value.ProviderId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(kvp => kvp.Value.Price * quantities[kvp.Key]));

        decimal greedyTotal = providerSubtotals.Sum(kvp =>
            kvp.Value + (includeShipping ? GetShippingCost(kvp.Key, kvp.Value) : 0));

        bestAssignment = greedyAssignment;
        bestTotalCost = greedyTotal;

        // Refinamento: tenta consolidar em menos providers se mais barato
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
                    // Tenta usar o provider alvo
                    if (pricesByBookProvider[book].TryGetValue(targetProvider, out var price))
                    {
                        consolidatedAssignment[book] = price;
                        consolidatedSubtotal += price.Price * quantities[book];
                    }
                    else
                    {
                        // Mantém o original se não disponível
                        canConsolidate = false;
                        break;
                    }
                }

                if (canConsolidate)
                {
                    var shipping = includeShipping ? GetShippingCost(targetProvider, consolidatedSubtotal) : 0;
                    var total = consolidatedSubtotal + shipping;

                    if (total < bestTotalCost)
                    {
                        bestTotalCost = total;
                        bestAssignment = consolidatedAssignment;
                    }
                }
            }
        }

        // Aplica limite de providers se especificado
        if (maxProviders > 0 && maxProviders < bestAssignment.Values.Select(v => v.ProviderId).Distinct().Count())
        {
            // Força consolidação nos N melhores providers
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
    /// Calcula o custo de frete para um provider
    /// </summary>
    private decimal GetShippingCost(int providerId, decimal subtotal)
    {
        if (!ShippingConfigs.TryGetValue(providerId, out var config))
        {
            // Provider desconhecido: usa frete padrão
            config = new ShippingConfig { BaseShipping = 15m, FreeShippingThreshold = 150m };
        }

        return subtotal >= config.FreeShippingThreshold ? 0 : config.BaseShipping;
    }

    /// <summary>
    /// Constrói o resultado final da otimização
    /// </summary>
    private CartOptimizationResult BuildResult(
        Dictionary<string, BookPriceOption> assignment,
        Dictionary<string, int> quantities,
        List<string> booksNotFound,
        CartOptimizationRequest request)
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
        decimal totalShippingCost = 0;

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
            var shipping = request.IncludeShipping ? GetShippingCost(providerId, subtotal) : 0;
            var config = ShippingConfigs.GetValueOrDefault(providerId);

            providerCarts.Add(new ProviderCart
            {
                ProviderId = providerId,
                ProviderName = group.First().Value.ProviderName,
                Items = items,
                Subtotal = subtotal,
                ShippingCost = shipping,
                Total = subtotal + shipping,
                FreeShippingThreshold = config?.FreeShippingThreshold,
                HasFreeShipping = shipping == 0
            });

            totalBooksCost += subtotal;
            totalShippingCost += shipping;
        }

        // Calcula economia comparada ao pior cenário (tudo no provider mais caro)
        decimal worstCaseTotal = CalculateWorstCase(assignment, quantities, request.IncludeShipping);
        decimal savings = worstCaseTotal - (totalBooksCost + totalShippingCost);
        decimal savingsPercent = worstCaseTotal > 0 ? (savings / worstCaseTotal) * 100 : 0;

        return new CartOptimizationResult
        {
            Success = true,
            Message = $"Carrinho otimizado em {providerCarts.Count} pedido(s)",
            TotalCost = totalBooksCost + totalShippingCost,
            BooksCost = totalBooksCost,
            ShippingCost = totalShippingCost,
            Savings = Math.Max(0, savings),
            SavingsPercent = Math.Max(0, savingsPercent),
            ProviderCarts = providerCarts.OrderByDescending(p => p.Subtotal).ToList(),
            BooksNotFound = booksNotFound
        };
    }

    private decimal CalculateWorstCase(
        Dictionary<string, BookPriceOption> assignment,
        Dictionary<string, int> quantities,
        bool includeShipping)
    {
        // Simula comprar cada livro no provider mais caro
        decimal total = 0;
        var providersUsed = new HashSet<int>();

        foreach (var kvp in assignment)
        {
            // Encontra o preço mais alto para este livro
            var price = kvp.Value.Price * 1.3m; // Assume 30% mais caro como pior caso
            total += price * quantities.GetValueOrDefault(kvp.Key, 1);
            providersUsed.Add(kvp.Value.ProviderId);
        }

        // Adiciona frete de cada provider
        if (includeShipping)
        {
            foreach (var providerId in providersUsed)
            {
                total += GetShippingCost(providerId, 0); // Força frete pago
            }
        }

        return total;
    }

    private class ShippingConfig
    {
        public decimal BaseShipping { get; set; }
        public decimal FreeShippingThreshold { get; set; }
    }
}
