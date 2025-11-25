using Sherlock.Domain.Entities;

namespace Sherlock.Business.Core.Scrapers;
public class SearchParameter
{
    public string BookTitle { get; set; } = string.Empty;
    public string? AuthorName { get; set; }
    public string? Isbn { get; set; }
    public bool IsExactSearch { get; set; } = true;
    public Provider? Source { get; set; }
}
