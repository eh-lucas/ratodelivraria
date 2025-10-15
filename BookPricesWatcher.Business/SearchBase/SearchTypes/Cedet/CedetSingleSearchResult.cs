using System.ComponentModel;
using Sherlock.Domain.Entities;

namespace Sherlock.Business.SearchBase.SearchTypes.Cedet;
public sealed class CedetSingleSearchResult : SearchResult
{
    [DisplayName("Livro")]
    public Book Book { get; set; }
}

