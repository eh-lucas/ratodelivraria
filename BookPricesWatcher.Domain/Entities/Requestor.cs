
namespace Sherlock.Domain.Entities;

/// <summary>
/// Requisição de uma transação.
/// Deve conter todas as informações para poder executar uma consulta bem-sucedida.
/// </summary>
public class Requestor
//public class Requestor<TParams> where TParams : SearchParameters
{
    public SearchParameters SearchParameters { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public TimeSpan CacheTimeSpan { get; set; }
    public List<Source> SourcesToSearch { get; set; }

    public Requestor(SearchParameters searchParameters, List<Source> sourcesToSearch, TimeSpan cacheTimeSpan)
    {
        SearchParameters = searchParameters;
        SourcesToSearch = sourcesToSearch;
        CacheTimeSpan = cacheTimeSpan;
    }

    public Requestor(SearchParameters searchParameters)
    {
        SearchParameters = searchParameters;
        SourcesToSearch = Source.AllSources.Where(s => s.SourceCategory == SourceCategory.Cedet).ToList();
        CacheTimeSpan = TimeSpan.FromDays(1);
    }

    public Requestor()
    {
    }
}
