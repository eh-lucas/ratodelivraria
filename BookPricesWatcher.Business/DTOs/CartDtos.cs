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
    /// URLs dos providers específicos para buscar (null = todos ativos)
    /// </summary>
    public List<string>? ProviderUrls { get; set; }
}

/// <summary>
/// Item do carrinho (livro a ser buscado)
/// </summary>
public class CartBookItem
{
    /// <summary>
    /// ISBN do livro (obrigatório para busca)
    /// </summary>
    public string Isbn { get; set; } = string.Empty;

    /// <summary>
    /// Quantidade desejada
    /// </summary>
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
    /// Custo total otimizado
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Custo total dos livros
    /// </summary>
    public decimal BooksCost { get; set; }

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
    /// Tabela comparativa de todos os providers com seus totais
    /// Ordenada do mais barato ao mais caro
    /// </summary>
    public List<ProviderComparison> ProviderComparisons { get; set; } = new();

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

    /// <summary>
    /// Total de livros buscados
    /// </summary>
    public int TotalBooksRequested { get; set; }

    /// <summary>
    /// Total de queries executadas (livros × providers)
    /// </summary>
    public int TotalQueriesExecuted { get; set; }
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
    /// Total
    /// </summary>
    public decimal Total { get; set; }
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
    /// Menor custo total
    /// </summary>
    LowestTotal,

    /// <summary>
    /// Menor número de pedidos (menos sites)
    /// </summary>
    FewestOrders,

    /// <summary>
    /// Apenas um site (sem divisão)
    /// </summary>
    SingleProvider
}

/// <summary>
/// Preço de um livro em um provider específico
/// </summary>
public class BookPriceOption
{
    public string BookTitle { get; set; } = string.Empty;
    public string? Isbn { get; set; }
    public string? Author { get; set; }
    public int ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? Discount { get; set; }
    public string? ProductUrl { get; set; }
    public bool Available { get; set; } = true;
}

/// <summary>
/// Requisição para busca de carrinho com melhor provider único
/// </summary>
public class BestProviderCartRequest
{
    /// <summary>
    /// Lista de livros a serem buscados
    /// </summary>
    public List<CartBookItem> Books { get; set; } = new();

    /// <summary>
    /// URLs dos providers específicos para buscar (null = todos ativos)
    /// </summary>
    public List<string>? ProviderUrls { get; set; }
}

/// <summary>
/// Resultado simplificado: melhor provider único para comprar todos os livros
/// </summary>
public class BestProviderCartResult
{
    /// <summary>
    /// Indica se a busca foi bem-sucedida
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Mensagem de status
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Melhor provider para comprar todos os livros
    /// </summary>
    public ProviderCart? BestProvider { get; set; }

    /// <summary>
    /// Segundo melhor provider (alternativa)
    /// </summary>
    public ProviderCart? SecondBestProvider { get; set; }

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

    /// <summary>
    /// Total de providers consultados
    /// </summary>
    public int TotalProvidersSearched { get; set; }
}

/// <summary>
/// Comparação de um provider para a otimização de carrinho
/// </summary>
public class ProviderComparison
{
    /// <summary>
    /// ID do provider
    /// </summary>
    public int ProviderId { get; set; }

    /// <summary>
    /// Nome do provider
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// URL do provider
    /// </summary>
    public string ProviderUrl { get; set; } = string.Empty;

    /// <summary>
    /// Total de livros encontrados neste provider
    /// </summary>
    public int BooksFound { get; set; }

    /// <summary>
    /// Total de livros requisitados
    /// </summary>
    public int TotalBooksRequested { get; set; }

    /// <summary>
    /// Se o provider tem todos os livros requisitados
    /// </summary>
    public bool HasAllBooks => BooksFound == TotalBooksRequested;

    /// <summary>
    /// Preço total de todos os livros neste provider
    /// </summary>
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// Lista de preços por livro
    /// </summary>
    public List<BookPriceDetail> BookPrices { get; set; } = new();

    /// <summary>
    /// ISBNs não encontrados neste provider
    /// </summary>
    public List<string> MissingIsbns { get; set; } = new();
}

/// <summary>
/// Detalhe do preço de um livro em um provider
/// </summary>
public class BookPriceDetail
{
    public string Isbn { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal TotalPrice => Price * Quantity;
}
