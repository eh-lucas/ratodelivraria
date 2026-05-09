using Microsoft.EntityFrameworkCore;
using Sherlock.Data.Context;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Interfaces;

namespace Sherlock.Data.Repositories;

public class BookRepository : RepositoryBase<Book>, IBookRepository
{
    public BookRepository(SherlockDbContext context) : base(context)
    {
    }

    public async Task<Book?> GetByIsbnAsync(string isbn)
    {
        return await _dbSet.FirstOrDefaultAsync(b => b.Isbn == isbn);
    }

    public async Task<Book?> GetByTitleAsync(string title)
    {
        return await _dbSet.FirstOrDefaultAsync(b =>
            b.Title.ToLower() == title.ToLower());
    }

    public async Task<IEnumerable<Book>> SearchAsync(string searchTerm)
    {
        var term = searchTerm.ToLower();
        return await _dbSet
            .Where(b => b.Title.ToLower().Contains(term) ||
                        b.Author.ToLower().Contains(term) ||
                        b.Isbn.Contains(term))
            .Take(50)
            .ToListAsync();
    }

    public async Task<Book> GetOrCreateAsync(string title, string? isbn = null, string? author = null)
    {
        // Primeiro tenta encontrar por ISBN se fornecido
        if (!string.IsNullOrEmpty(isbn))
        {
            var existingByIsbn = await GetByIsbnAsync(isbn);
            if (existingByIsbn != null)
                return existingByIsbn;
        }

        // Depois tenta por título
        var existingByTitle = await GetByTitleAsync(title);
        if (existingByTitle != null)
            return existingByTitle;

        // Se não existe, cria novo
        var newBook = new Book
        {
            Title = title,
            Isbn = isbn ?? string.Empty,
            Author = author ?? string.Empty,
            Editor = string.Empty,
            Language = "pt-BR"
        };

        return await AddAsync(newBook);
    }
}
