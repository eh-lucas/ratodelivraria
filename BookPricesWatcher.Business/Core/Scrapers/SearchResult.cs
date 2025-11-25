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
}
