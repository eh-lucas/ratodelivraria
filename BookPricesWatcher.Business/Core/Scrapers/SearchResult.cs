using Sherlock.Domain.Entities;

namespace Sherlock.Business.Core.Scrapers;
public class SearchResult
{
    public DateTime InicioConsulta { get; set; }
    public DateTime FimConsulta { get; set; }
    public long TempoDecorrido { get; set; }
    public int CustoCreditos { get; set; }
    public ResultType ResultadoTransacao { get; set; }
    public BookPriceResult BookPriceResult { get; set; }
}
