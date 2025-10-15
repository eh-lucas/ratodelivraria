using Sherlock.Domain.Entities;

namespace Sherlock.Business.SearchBase.SearchTypes.Cedet;
public class CedetSingleSearchParams : SearchParameters
{
    public string BookTitle { get; set; }
    public string Website { get; set; }
    public string AuthorName { get; set; }
    public string Isbn { get; set; }
    public bool IsExactSearch { get; set; } = true;
}
