using Sherlock.Domain.Entities;

namespace Sherlock.Business.Core.Scrapers;

public class SearchResult
{
    public DateTime InicioConsulta { get; set; }
    public DateTime FimConsulta { get; set; }
    public long TempoDecorrido { get; set; }
    public int CustoCreditos { get; set; }
    public ResultType ResultadoTransacao { get; set; } = new ResultType();
    public BookPriceResult BookPriceResult { get; set; } = new BookPriceResult();

    // Métricas de execução
    public int TotalSourcesQueried { get; set; }
    public int SuccessfulQueries { get; set; }
    public int FailedQueries { get; set; }
    public List<string> Errors { get; set; } = new List<string>();

    // Cache
    public bool FromCache { get; set; }

    // Todos os resultados encontrados (não apenas o melhor) - compatibilidade
    public List<BookPriceResult> AllResults { get; set; } = new List<BookPriceResult>();

    /// <summary>
    /// Todos os QueryResults com dados completos (inclui erros, tempos, etc)
    /// </summary>
    public List<QueryResult> AllQueryResults { get; set; } = new List<QueryResult>();

    /// <summary>
    /// Resumo de erros agregados por tipo
    /// </summary>
    public ErrorSummary? ErrorSummary => AllQueryResults.Count > 0 ? new ErrorSummary(AllQueryResults) : null;
}

/// <summary>
/// Resumo agregado de erros da transação
/// </summary>
public class ErrorSummary
{
    public int TotalErrors { get; }
    public int TimeoutCount { get; }
    public int NetworkCount { get; }
    public int HttpErrorCount { get; }
    public int ParseErrorCount { get; }
    public int BlockedCount { get; }
    public int UnknownCount { get; }

    /// <summary>
    /// Providers que falharam por timeout
    /// </summary>
    public List<string> TimeoutProviders { get; }

    /// <summary>
    /// Providers que falharam por erro de rede
    /// </summary>
    public List<string> NetworkErrorProviders { get; }

    /// <summary>
    /// Providers que retornaram erro HTTP (4xx, 5xx)
    /// </summary>
    public List<string> HttpErrorProviders { get; }

    /// <summary>
    /// Providers bloqueados (rate limit, etc)
    /// </summary>
    public List<string> BlockedProviders { get; }

    public ErrorSummary(IEnumerable<QueryResult> queryResults)
    {
        var failed = queryResults.Where(q => !q.Success && q.ErrorType.HasValue).ToList();

        TotalErrors = failed.Count;
        TimeoutCount = failed.Count(q => q.ErrorType == QueryErrorType.Timeout);
        NetworkCount = failed.Count(q => q.ErrorType == QueryErrorType.Network);
        HttpErrorCount = failed.Count(q => q.ErrorType == QueryErrorType.HttpError);
        ParseErrorCount = failed.Count(q => q.ErrorType == QueryErrorType.ParseError);
        BlockedCount = failed.Count(q => q.ErrorType == QueryErrorType.Blocked);
        UnknownCount = failed.Count(q => q.ErrorType == QueryErrorType.Unknown);

        TimeoutProviders = failed.Where(q => q.ErrorType == QueryErrorType.Timeout).Select(q => q.ProviderName).ToList();
        NetworkErrorProviders = failed.Where(q => q.ErrorType == QueryErrorType.Network).Select(q => q.ProviderName).ToList();
        HttpErrorProviders = failed.Where(q => q.ErrorType == QueryErrorType.HttpError).Select(q => q.ProviderName).ToList();
        BlockedProviders = failed.Where(q => q.ErrorType == QueryErrorType.Blocked).Select(q => q.ProviderName).ToList();
    }

    /// <summary>
    /// Tipo de erro mais frequente
    /// </summary>
    public QueryErrorType? MostCommonErrorType
    {
        get
        {
            if (TotalErrors == 0) return null;

            var counts = new Dictionary<QueryErrorType, int>
            {
                { QueryErrorType.Timeout, TimeoutCount },
                { QueryErrorType.Network, NetworkCount },
                { QueryErrorType.HttpError, HttpErrorCount },
                { QueryErrorType.ParseError, ParseErrorCount },
                { QueryErrorType.Blocked, BlockedCount },
                { QueryErrorType.Unknown, UnknownCount }
            };

            return counts.OrderByDescending(kv => kv.Value).First().Key;
        }
    }
}
