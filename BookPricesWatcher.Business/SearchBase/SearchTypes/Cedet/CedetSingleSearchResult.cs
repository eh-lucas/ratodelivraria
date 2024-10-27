using Sherlock.Domain.Entities;

namespace Sherlock.Business.SearchBase.SearchTypes.Cedet;
public class CedetSingleSearchResult : SearchResult
{
    public Book Book { get; set; }

    public CedetSingleSearchResult(Book book)
    {
        Book = book;
    }
}

