using Sherlock.Business.DTOs;
using Sherlock.Business.Interfaces;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Interfaces;

namespace Sherlock.Business.Services;

public class BookService : IBookService
{
    private readonly IBookRepository _bookRepository;

    public BookService(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public async Task<BookDto?> GetByIdAsync(int id)
    {
        var book = await _bookRepository.GetByIdAsync(id);
        return book == null ? null : MapToDto(book);
    }

    public async Task<BookDto?> GetByIsbnAsync(string isbn)
    {
        var book = await _bookRepository.GetByIsbnAsync(isbn);
        return book == null ? null : MapToDto(book);
    }

    public async Task<IEnumerable<BookDto>> SearchAsync(string searchTerm)
    {
        var books = await _bookRepository.SearchAsync(searchTerm);
        return books.Select(MapToDto);
    }

    public async Task<BookDto> CreateAsync(BookDto bookDto)
    {
        var book = new Book
        {
            Title = bookDto.Title,
            Author = bookDto.Author,
            Isbn = bookDto.Isbn,
            Editor = bookDto.Editor,
            PageNumber = bookDto.PageNumber,
            Language = bookDto.Language
        };

        var created = await _bookRepository.AddAsync(book);
        return MapToDto(created);
    }

    public async Task<BookDto> GetOrCreateAsync(string title, string? isbn = null, string? author = null)
    {
        var book = await _bookRepository.GetOrCreateAsync(title, isbn, author);
        return MapToDto(book);
    }

    private static BookDto MapToDto(Book book)
    {
        return new BookDto
        {
            Id = book.Id,
            Title = book.Title,
            Author = book.Author,
            Isbn = book.Isbn,
            Editor = book.Editor,
            PageNumber = book.PageNumber,
            Language = book.Language
        };
    }
}
