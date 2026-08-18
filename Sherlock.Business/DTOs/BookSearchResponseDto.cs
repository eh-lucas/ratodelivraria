using Sherlock.Business.Core.Scrapers;

namespace Sherlock.Business.DTOs;

/// <summary>
/// DTO de resposta para busca de livros - formatado para exibição no frontend
/// </summary>
public class BookSearchResponseDto
{
    /// <summary>
    /// Melhor resultado (menor preço com ISBN validado)
    /// </summary>
    public QueryResultItemDto? BestResult { get; set; }

    /// <summary>
    /// Todos os resultados (para tabela)
    /// </summary>
    public List<QueryResultItemDto> AllResults { get; set; } = new();

    /// <summary>
    /// Total de providers consultados
    /// </summary>
    public int TotalProviders { get; set; }

    /// <summary>
    /// Quantidade de consultas com sucesso
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Quantidade de consultas com erro
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Total de créditos consumidos
    /// </summary>
    public int TotalCredits { get; set; }

    /// <summary>
    /// Tempo total de execução em milissegundos
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// ISBN buscado
    /// </summary>
    public string SearchedIsbn { get; set; } = string.Empty;

    /// <summary>
    /// Converte SearchResult para BookSearchResponseDto
    /// </summary>
    public static BookSearchResponseDto FromSearchResult(SearchResult result, string searchedIsbn)
    {
        var allResults = result.AllQueryResults
            .Select(QueryResultItemDto.FromQueryResult)
            .OrderBy(r => r.Success ? 0 : 1)  // Sucesso primeiro
            .ThenBy(r => r.Price ?? decimal.MaxValue)  // Menor preço
            .ToList();

        var bestResult = allResults
            .Where(r => r.Success && r.Price.HasValue && r.Price > 0)
            .OrderBy(r => r.Price)
            .FirstOrDefault();

        return new BookSearchResponseDto
        {
            BestResult = bestResult,
            AllResults = allResults,
            TotalProviders = result.TotalSourcesQueried,
            SuccessCount = result.SuccessfulQueries,
            ErrorCount = result.FailedQueries,
            TotalCredits = result.CustoCreditos,
            ExecutionTimeMs = result.TempoDecorrido,
            SearchedIsbn = searchedIsbn
        };
    }
}

/// <summary>
/// DTO para item individual de resultado
/// </summary>
public class QueryResultItemDto
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
    /// URL base do provider
    /// </summary>
    public string ProviderUrl { get; set; } = string.Empty;

    /// <summary>
    /// Título do livro encontrado (null se erro)
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Autor do livro (null se erro)
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Preço encontrado (null se erro)
    /// </summary>
    public decimal? Price { get; set; }

    /// <summary>
    /// Desconto em percentual
    /// </summary>
    public int? Discount { get; set; }

    /// <summary>
    /// Link direto para a página do produto
    /// </summary>
    public string? ProductUrl { get; set; }

    /// <summary>
    /// Capa do livro, quando a loja mandou
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Se a consulta foi bem-sucedida
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Mensagem de erro (se houve falha)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Tipo do erro
    /// </summary>
    public string? ErrorType { get; set; }

    /// <summary>
    /// Tempo de resposta em milissegundos
    /// </summary>
    public long ResponseTimeMs { get; set; }

    /// <summary>
    /// Custo em créditos (1 por consulta)
    /// </summary>
    public int Credits { get; set; } = 1;

    /// <summary>
    /// Converte QueryResult para QueryResultItemDto
    /// </summary>
    public static QueryResultItemDto FromQueryResult(QueryResult query)
    {
        return new QueryResultItemDto
        {
            ProviderId = query.ProviderId,
            ProviderName = query.ProviderName,
            ProviderUrl = query.ProviderUrl,
            Title = query.Title,
            Author = query.Author,
            Price = query.Price > 0 ? query.Price : null,
            Discount = query.Discount > 0 ? query.Discount : null,
            ProductUrl = query.ProductUrl,
            ImageUrl = query.ImageUrl,
            Success = query.Success && query.HasValidResult,
            ErrorMessage = query.ErrorMessage,
            ErrorType = query.ErrorType?.ToString(),
            ResponseTimeMs = query.ResponseTimeMs,
            Credits = 1
        };
    }
}
