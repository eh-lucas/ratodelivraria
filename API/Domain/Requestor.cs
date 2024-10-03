using System;
using API.Enums;

namespace API.Domain;
public class Requestor
{
    public InputParameters InputParameters { get; set; }
    public SearchTypeEnum SearchTypeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Requestor()
    {
    }

    public Requestor(InputParameters inputParameters, SearchTypeEnum searchTypeId)
    {
        InputParameters = inputParameters;
        SearchTypeId = searchTypeId;
    }
}
