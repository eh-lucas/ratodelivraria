using Sherlock.Business.Interfaces;
using Sherlock.Domain.Entities;

namespace Sherlock.Business.Core.Scrapers;
public class SearchResult
{
    public Book Book { get; set; }
    public Source Source { get; set; }
}
