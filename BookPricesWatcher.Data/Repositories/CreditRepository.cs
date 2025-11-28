using Microsoft.EntityFrameworkCore;
using Sherlock.Data.Context;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Interfaces;

namespace Sherlock.Data.Repositories;

public class CreditRepository : ICreditRepository
{
    private readonly SherlockDbContext _context;

    public CreditRepository(SherlockDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetUserCreditsAsync(int userId)
    {
        return await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.AvailableCredits)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateUserCreditsAsync(int userId, int newBalance, int totalUsed)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        user.AvailableCredits = newBalance;
        user.TotalCreditsUsed = totalUsed;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<CreditTransaction> AddCreditTransactionAsync(CreditTransaction transaction)
    {
        _context.CreditTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<(List<CreditTransaction> Items, int TotalCount)> GetCreditHistoryAsync(
        int userId, int page, int pageSize)
    {
        var query = _context.CreditTransactions
            .Include(ct => ct.CreditPackage)
            .Where(ct => ct.UserId == userId)
            .OrderByDescending(ct => ct.CreatedAt);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<List<CreditPackage>> GetActivePackagesAsync()
    {
        return await _context.CreditPackages
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync();
    }

    public async Task<CreditPackage?> GetPackageByIdAsync(int packageId)
    {
        return await _context.CreditPackages
            .FirstOrDefaultAsync(p => p.Id == packageId && p.IsActive);
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users.FindAsync(userId);
    }
}
