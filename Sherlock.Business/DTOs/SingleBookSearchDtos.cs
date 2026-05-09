namespace Sherlock.Business.DTOs;

/// <summary>
/// Requisição para busca de livro único
/// </summary>
public class SingleBookSearchRequest
{
    /// <summary>
    /// ISBN do livro (obrigatório)
    /// </summary>
    public string Isbn { get; set; } = string.Empty;

    /// <summary>
    /// URLs dos providers específicos para buscar (null = todos ativos)
    /// </summary>
    public List<string>? ProviderUrls { get; set; }
}

/// <summary>
/// Resultado da busca de livro único com melhor opção e alternativas
/// </summary>
public class SingleBookSearchResult
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
    /// Melhor opção encontrada (menor preço)
    /// </summary>
    public BookPriceOption? BestOption { get; set; }

    /// <summary>
    /// Opções alternativas (até 2)
    /// </summary>
    public List<BookPriceOption> Alternatives { get; set; } = new();

    /// <summary>
    /// Total de providers consultados
    /// </summary>
    public int TotalProvidersSearched { get; set; }

    /// <summary>
    /// Providers que retornaram resultado
    /// </summary>
    public int ProvidersWithResults { get; set; }

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
