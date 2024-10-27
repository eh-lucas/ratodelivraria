using Sherlock.Domain.Enums;

namespace Sherlock.Domain.Entities;

/// <summary>
/// Requisição de uma transação.
/// Deve conter todas as informações para poder executar uma consulta bem-sucedida.
/// </summary>
public class Requestor
//public class Requestor<TParams> where TParams : SearchParameters
{
    public SearchParameters SearchParameters { get; set; }
    public SearchTypeEnum SearchTypeId { get; set; }
    //public ConsultaBase ConsultaBase { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public Requestor(SearchParameters searchParameters, SearchTypeEnum searchTypeId)
    {
        SearchParameters = searchParameters;
        SearchTypeId = searchTypeId;
    }

    public Requestor()
    {
    }
}
