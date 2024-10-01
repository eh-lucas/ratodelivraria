using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using API.Enums;

namespace API.Domain;
class Requestor
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
