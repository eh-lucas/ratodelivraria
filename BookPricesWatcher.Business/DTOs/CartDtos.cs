namespace Sherlock.Business.DTOs;

/// <summary>
/// Requisição de otimização de carrinho
/// </summary>
public class CartOptimizationRequest
{
    /// <summary>
    /// Lista de livros a serem comprados
    /// </summary>
    public List<CartBookItem> Books { get; set; } = new();

    /// <summary>
    /// Estratégia de otimização
    /// </summary>
    public OptimizationStrategy Strategy { get; set; } = OptimizationStrategy.LowestTotal;

    /// <summary>
    /// Máximo de sites para dividir a compra (0 = sem limite)
    /// </summary>
    public int MaxProviders { get; set; } = 0;

    /// <summary>
    /// Considerar frete na otimização
    /// </summary>
    public bool IncludeShipping { get; set; } = true;

    /// <summary>
    /// URLs dos providers específicos para buscar (null = todos ativos)
    /// </summary>
    public List<string>? ProviderUrls { get; set; }
}

/// <summary>
/// Item do carrinho (livro a ser buscado)
/// </summary>
public class CartBookItem
{
    public string Title { get; set; } = string.Empty;
    public string? Isbn { get; set; }
    public string? Author { get; set; }
    public int Quantity { get; set; } = 1;
}

/// <summary>
/// Resultado da otimização do carrinho
/// </summary>
public class CartOptimizationResult
{
    /// <summary>
    /// Indica se a otimização foi bem-sucedida
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Mensagem de status
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Custo total otimizado (livros + frete)
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Custo total dos livros (sem frete)
    /// </summary>
    public decimal BooksCost { get; set; }

    /// <summary>
    /// Custo total de frete
    /// </summary>
    public decimal ShippingCost { get; set; }

    /// <summary>
    /// Economia comparada a comprar tudo no site mais caro
    /// </summary>
    public decimal Savings { get; set; }

    /// <summary>
    /// Percentual de economia
    /// </summary>
    public decimal SavingsPercent { get; set; }

    /// <summary>
    /// Carrinhos por provider (onde comprar cada livro)
    /// </summary>
    public List<ProviderCart> ProviderCarts { get; set; } = new();

    /// <summary>
    /// Livros não encontrados em nenhum provider
    /// </summary>
    public List<string> BooksNotFound { get; set; } = new();

    /// <summary>
    /// Tempo de execução em ms
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// Créditos consumidos
    /// </summary>
    public int CreditsUsed { get; set; }

    /// <summary>
    /// Se o resultado veio do cache
    /// </summary>
    public bool FromCache { get; set; }
}

/// <summary>
/// Carrinho de um provider específico
/// </summary>
public class ProviderCart
{
    public int ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderUrl { get; set; } = string.Empty;

    /// <summary>
    /// Livros a comprar neste provider
    /// </summary>
    public List<ProviderCartItem> Items { get; set; } = new();

    /// <summary>
    /// Subtotal dos livros
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Custo de frete estimado
    /// </summary>
    public decimal ShippingCost { get; set; }

    /// <summary>
    /// Total (subtotal + frete)
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// Valor mínimo para frete grátis (se aplicável)
    /// </summary>
    public decimal? FreeShippingThreshold { get; set; }

    /// <summary>
    /// Se atingiu frete grátis
    /// </summary>
    public bool HasFreeShipping { get; set; }
}

/// <summary>
/// Item no carrinho de um provider
/// </summary>
public class ProviderCartItem
{
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? Isbn { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal? Discount { get; set; }
    public string? ProductUrl { get; set; }
}

/// <summary>
/// Estratégias de otimização
/// </summary>
public enum OptimizationStrategy
{
    /// <summary>
    /// Menor custo total (livros + frete)
    /// </summary>
    LowestTotal,

    /// <summary>
    /// Menor número de pedidos (menos sites)
    /// </summary>
    FewestOrders,

    /// <summary>
    /// Prioriza frete grátis
    /// </summary>
    PrioritizeFreeShipping,

    /// <summary>
    /// Apenas um site (sem divisão)
    /// </summary>
    SingleProvider
}

/// <summary>
/// Preço de um livro em um provider específico (para uso interno)
/// </summary>
public class BookPriceOption
{
    public string BookTitle { get; set; } = string.Empty;
    public string? Isbn { get; set; }
    public int ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? Discount { get; set; }
    public string? ProductUrl { get; set; }
    public bool Available { get; set; } = true;
}
