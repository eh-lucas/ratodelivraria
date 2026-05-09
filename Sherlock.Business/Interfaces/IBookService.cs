using Sherlock.Business.DTOs;

namespace Sherlock.Business.Interfaces;

public interface IBookService
{
    Task<BookDto?> GetByIdAsync(int id);
    Task<BookDto?> GetByIsbnAsync(string isbn);
    Task<IEnumerable<BookDto>> SearchAsync(string searchTerm);
    Task<BookDto> CreateAsync(BookDto book);
    Task<BookDto> GetOrCreateAsync(string title, string? isbn = null, string? author = null);
}
