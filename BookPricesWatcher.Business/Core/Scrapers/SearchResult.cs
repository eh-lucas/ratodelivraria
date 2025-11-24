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
}
