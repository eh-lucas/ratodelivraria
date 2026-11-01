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
        IEnumerable<string> isbns, int days, bool ateOntem = false,
        CancellationToken cancellationToken = default)
    {
        var lista = isbns.Distinct().ToList();
        if (lista.Count == 0) return new Dictionary<string, decimal>();

        var desde = DateTime.UtcNow.AddDays(-days);

        // Corte no início do dia UTC, não "agora menos 24h": a varredura roda às
        // 03h e o aviso às 09h, então subtrair 24h ainda deixaria a varredura de
        // hoje dentro da janela — que é exatamente o que precisa sair.
        var ate = ateOntem
            ? DateTime.UtcNow.Date
            : (DateTime?)null;

        // `success && price > 0` não é zelo: loja sem o livro grava a consulta com
        // preço nulo ou zero, e um zero no MIN viraria "o menor preço do mês é
        // R$0,00" — o aviso mais inútil que o produto poderia mandar.
        var linhas = await _context.Queries
            .Where(q => q.SearchIsbn != null
                        && lista.Contains(q.SearchIsbn)
                        && q.Success
                        && q.Price != null
                        && q.Price > 0
                        && q.QueriedAt >= desde
                        && (ate == null || q.QueriedAt < ate))
            .GroupBy(q => q.SearchIsbn!)
            .Select(g => new { Isbn = g.Key, Min = g.Min(q => q.Price!.Value) })
            .ToListAsync(cancellationToken);

        return linhas.ToDictionary(l => l.Isbn, l => l.Min);
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<PricePoint>>> GetPriceHistoryAsync(
        IEnumerable<string> isbns, int days, CancellationToken cancellationToken = default)
    {
        var lista = isbns.Distinct().ToList();
        if (lista.Count == 0) return new Dictionary<string, IReadOnlyList<PricePoint>>();

        var desde = DateTime.UtcNow.AddDays(-days);

        // Mesmo filtro do GetLowestPricesAsync: loja sem o livro grava preço nulo
        // ou zero, e um zero no MIN desenharia o gráfico caindo até o chão.
        var linhas = await _context.Queries
            .Where(q => q.SearchIsbn != null
                        && lista.Contains(q.SearchIsbn)
                        && q.Success
                        && q.Price != null
                        && q.Price > 0
                        && q.QueriedAt >= desde)
            .GroupBy(q => new { Isbn = q.SearchIsbn!, Dia = q.QueriedAt.Date })
            .Select(g => new { g.Key.Isbn, g.Key.Dia, Menor = g.Min(q => q.Price!.Value) })
            .ToListAsync(cancellationToken);

        return linhas
            .GroupBy(l => l.Isbn)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<PricePoint>)g
                    .OrderBy(l => l.Dia)
                    .Select(l => new PricePoint(DateOnly.FromDateTime(l.Dia), l.Menor))
                    .ToList());
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
