using Sherlock.Domain.Entities;

namespace Sherlock.Business.Core.Scrapers;

/// <summary>
/// Resultado de uma consulta individual a um provider.
/// Contém todos os dados necessários para criar um Query no banco de dados.
/// </summary>
public class QueryResult
{
    /// <summary>
    /// Provider consultado
    /// </summary>
    public int ProviderId { get; set; }

    /// <summary>
    /// Nome do provider (para logging/exibição)
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// URL do provider
    /// </summary>
    public string ProviderUrl { get; set; } = string.Empty;

    /// <summary>
    /// Se a consulta foi bem-sucedida (retornou resultado válido)
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Tempo de resposta em milissegundos
    /// </summary>
    public long ResponseTimeMs { get; set; }

    /// <summary>
    /// Título encontrado
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Autor encontrado
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Preço encontrado
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Desconto encontrado (percentual)
    /// </summary>
    public int Discount { get; set; }

    /// <summary>
    /// URL do produto encontrado
    /// </summary>
    public string? ProductUrl { get; set; }

    /// <summary>
    /// Mensagem de erro (se houve falha)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Código do erro (timeout, network, parse, etc)
    /// </summary>
    public QueryErrorType? ErrorType { get; set; }

    /// <summary>
    /// HTTP Status Code da resposta (se aplicável)
    /// </summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>
    /// Momento da consulta
    /// </summary>
    public DateTime QueriedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Se o resultado veio do cache do banco (não executou scraping)
    /// </summary>
    public bool FromCache { get; set; }

    /// <summary>
    /// Verifica se o resultado é válido (tem título e preço > 0)
    /// </summary>
    public bool HasValidResult => Success && !string.IsNullOrEmpty(Title) && Price > 0;

    /// <summary>
    /// Cria um QueryResult de sucesso
    /// </summary>
    public static QueryResult CreateSuccess(
        Provider provider,
        string title,
        string? author,
        decimal price,
        int discount,
        long responseTimeMs,
        string? productUrl = null)
    {
        return new QueryResult
        {
            ProviderId = provider.Id,
            ProviderName = provider.Name,
            ProviderUrl = provider.Url,
            Success = true,
            Title = title,
            Author = author,
            Price = price,
            Discount = discount,
            ProductUrl = productUrl,
            ResponseTimeMs = responseTimeMs,
            QueriedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Cria um QueryResult de falha
    /// </summary>
    public static QueryResult CreateFailure(
        Provider provider,
        QueryErrorType errorType,
        string errorMessage,
        long responseTimeMs,
        int? httpStatusCode = null)
    {
        return new QueryResult
        {
            ProviderId = provider.Id,
            ProviderName = provider.Name,
            ProviderUrl = provider.Url,
            Success = false,
            ErrorType = errorType,
            ErrorMessage = errorMessage,
            HttpStatusCode = httpStatusCode,
            ResponseTimeMs = responseTimeMs,
            QueriedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Cria um QueryResult vazio (sem resultado mas sem erro)
    /// </summary>
    public static QueryResult CreateNoResult(Provider provider, long responseTimeMs)
    {
        return new QueryResult
        {
            ProviderId = provider.Id,
            ProviderName = provider.Name,
            ProviderUrl = provider.Url,
            Success = true,
            ResponseTimeMs = responseTimeMs,
            QueriedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Converte para entidade Query (para persistência)
    /// </summary>
    /// <param name="transactionId">ID da transação</param>
    /// <param name="searchIsbn">ISBN usado na busca (para cache por ISBN)</param>
    public Query ToEntity(int transactionId, string? searchIsbn = null)
    {
        return new Query
        {
            TransactionId = transactionId,
            ProviderId = ProviderId,
            QueriedAt = QueriedAt,
            ResponseTimeMs = ResponseTimeMs,
            Success = Success,
            Title = Title,
            Author = Author,
            Price = Price > 0 ? Price : null,
            Discount = Discount > 0 ? Discount : null,
            ProductUrl = ProductUrl,
            ErrorMessage = ErrorMessage,
            SearchIsbn = searchIsbn,
            FromCache = FromCache
        };
    }
}

/// <summary>
/// Tipos de erro que podem ocorrer durante uma consulta
/// </summary>
public enum QueryErrorType
{
    /// <summary>
    /// Timeout na requisição
    /// </summary>
    Timeout,

    /// <summary>
    /// Erro de rede (DNS, conexão, etc)
    /// </summary>
    Network,

    /// <summary>
    /// Erro HTTP (4xx, 5xx)
    /// </summary>
    HttpError,

    /// <summary>
    /// Erro ao parsear HTML
    /// </summary>
    ParseError,

    /// <summary>
    /// Provider bloqueou a requisição
    /// </summary>
    Blocked,

    /// <summary>
    /// Erro desconhecido
    /// </summary>
    Unknown
}
