using Microsoft.EntityFrameworkCore;
using Sherlock.Data.Context;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Interfaces;

namespace Sherlock.Data.Repositories;

public class WatchedBookRepository : RepositoryBase<WatchedBook>, IWatchedBookRepository
{
    public WatchedBookRepository(SherlockDbContext context) : base(context) { }

    public async Task<IReadOnlyList<WatchedBook>> GetByUserAsync(
        int userId, CancellationToken cancellationToken = default)
        => await _dbSet
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<WatchedBook?> GetByUserAndIsbnAsync(
        string isbn, int userId, CancellationToken cancellationToken = default)
        => await _dbSet.FirstOrDefaultAsync(
            w => w.UserId == userId && w.Isbn == isbn, cancellationToken);

    public async Task<int> CountByUserAsync(int userId, CancellationToken cancellationToken = default)
        => await _dbSet.CountAsync(w => w.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<string>> GetDistinctActiveIsbnsAsync(
        CancellationToken cancellationToken = default)
        => await _dbSet
            .Where(w => w.Active)
            .Select(w => w.Isbn)
            .Distinct()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WatchedBook>> GetActiveByIsbnAsync(
        string isbn, CancellationToken cancellationToken = default)
        => await _dbSet
            .Where(w => w.Active && w.Isbn == isbn)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WatchedBook>> GetAllActiveWithUserAsync(
        CancellationToken cancellationToken = default)
        => await _dbSet
            .Include(w => w.User)
            .Where(w => w.Active)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<string, decimal>> GetLowestPricesAsync(
        IEnumerable<string> isbns, int days, CancellationToken cancellationToken = default)
    {
        var lista = isbns.Distinct().ToList();
        if (lista.Count == 0) return new Dictionary<string, decimal>();

        var desde = DateTime.UtcNow.AddDays(-days);

        // `success && price > 0` não é zelo: loja sem o livro grava a consulta com
        // preço nulo ou zero, e um zero no MIN viraria "o menor preço do mês é
        // R$0,00" — o aviso mais inútil que o produto poderia mandar.
        var linhas = await _context.Queries
            .Where(q => q.SearchIsbn != null
                        && lista.Contains(q.SearchIsbn)
                        && q.Success
                        && q.Price != null
                        && q.Price > 0
                        && q.QueriedAt >= desde)
            .GroupBy(q => q.SearchIsbn!)
            .Select(g => new { Isbn = g.Key, Min = g.Min(q => q.Price!.Value) })
            .ToListAsync(cancellationToken);

        return linhas.ToDictionary(l => l.Isbn, l => l.Min);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
