using API.Enums;

namespace Sherlock.Domain.Entities;

/// <summary>
/// Requisição de uma transação.
/// Deve conter todas as informações para poder executar uma consulta bem-sucedida.
/// </summary>
public class Requestor
{
    public InputParameters InputParameters { get; set; }
    public SearchTypeEnum SearchTypeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public Requestor(InputParameters inputParameters, SearchTypeEnum searchTypeId)
    {
        InputParameters = inputParameters;
        SearchTypeId = searchTypeId;
    }

    public Requestor()
    {
    }
}
