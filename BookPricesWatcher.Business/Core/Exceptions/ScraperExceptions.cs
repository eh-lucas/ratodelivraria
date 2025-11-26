namespace Sherlock.Business.Core.Exceptions;

/// <summary>
/// Exceção base para erros de scraping
/// </summary>
public abstract class ScraperException : Exception
{
    public string ProviderName { get; }
    public string? ProviderUrl { get; }

    protected ScraperException(string message, string providerName, string? providerUrl = null, Exception? innerException = null)
        : base(message, innerException)
    {
        ProviderName = providerName;
        ProviderUrl = providerUrl;
    }
}

/// <summary>
/// Exceção para timeout de requisições
/// </summary>
public class ScraperTimeoutException : ScraperException
{
    public TimeSpan Timeout { get; }

    public ScraperTimeoutException(string providerName, TimeSpan timeout, string? providerUrl = null)
        : base($"Timeout após {timeout.TotalSeconds:F1}s", providerName, providerUrl)
    {
        Timeout = timeout;
    }
}

/// <summary>
/// Exceção para erros de rede (DNS, conexão, etc)
/// </summary>
public class ScraperNetworkException : ScraperException
{
    public string NetworkError { get; }

    public ScraperNetworkException(string providerName, string networkError, string? providerUrl = null, Exception? innerException = null)
        : base($"Erro de rede: {networkError}", providerName, providerUrl, innerException)
    {
        NetworkError = networkError;
    }
}

/// <summary>
/// Exceção para erros HTTP (4xx, 5xx)
/// </summary>
public class ScraperHttpException : ScraperException
{
    public int StatusCode { get; }
    public bool IsClientError => StatusCode >= 400 && StatusCode < 500;
    public bool IsServerError => StatusCode >= 500;
    public bool IsRetryable => IsServerError || StatusCode == 429;

    public ScraperHttpException(string providerName, int statusCode, string? providerUrl = null)
        : base($"HTTP {statusCode}", providerName, providerUrl)
    {
        StatusCode = statusCode;
    }
}

/// <summary>
/// Exceção para erros de parsing HTML
/// </summary>
public class ScraperParseException : ScraperException
{
    public string Selector { get; }

    public ScraperParseException(string providerName, string selector, string? providerUrl = null, Exception? innerException = null)
        : base($"Falha ao parsear HTML com seletor: {selector}", providerName, providerUrl, innerException)
    {
        Selector = selector;
    }
}

/// <summary>
/// Exceção para quando o provider bloqueia a requisição
/// </summary>
public class ScraperBlockedException : ScraperException
{
    public string BlockReason { get; }

    public ScraperBlockedException(string providerName, string blockReason, string? providerUrl = null)
        : base($"Requisição bloqueada: {blockReason}", providerName, providerUrl)
    {
        BlockReason = blockReason;
    }
}

/// <summary>
/// Exceção para rate limiting
/// </summary>
public class ScraperRateLimitException : ScraperException
{
    public TimeSpan? RetryAfter { get; }

    public ScraperRateLimitException(string providerName, TimeSpan? retryAfter = null, string? providerUrl = null)
        : base($"Rate limit excedido{(retryAfter.HasValue ? $", retry após {retryAfter.Value.TotalSeconds}s" : "")}", providerName, providerUrl)
    {
        RetryAfter = retryAfter;
    }
}
