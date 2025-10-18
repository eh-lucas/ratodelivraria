using Sherlock.Domain.Entities;

namespace Sherlock.Business.Core.Scrapers;
public class SearchParameter
{
    public string BookTitle { get; set; }
    public string Website { get; set; }
    public string AuthorName { get; set; }
    public string Isbn { get; set; }
    public bool IsExactSearch { get; set; } = true;
    public Source Source { get; set; }
}
