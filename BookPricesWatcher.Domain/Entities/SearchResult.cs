using System.ComponentModel;

namespace Sherlock.Domain.Entities;
public abstract class SearchResult
{
    [DisplayName("Livro")]
    public Book Book { get; set; }

    public Source Source { get; set; }
}
