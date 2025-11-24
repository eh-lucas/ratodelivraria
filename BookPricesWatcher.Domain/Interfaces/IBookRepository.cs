using Sherlock.Domain.Entities;

namespace Sherlock.Domain.Interfaces;

public interface IBookRepository : IRepository<Book>
{
    Task<Book?> GetByIsbnAsync(string isbn);
    Task<Book?> GetByTitleAsync(string title);
    Task<IEnumerable<Book>> SearchAsync(string searchTerm);
    Task<Book> GetOrCreateAsync(string title, string? isbn = null, string? author = null);
}
