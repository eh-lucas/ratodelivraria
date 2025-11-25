using Sherlock.Domain.Entities;

namespace Sherlock.Domain.Interfaces;

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<IEnumerable<Transaction>> GetByUserIdAsync(int userId, int limit = 20);
    Task<IEnumerable<Transaction>> GetRecentAsync(int count = 10);
    Task<Transaction?> GetWithQueriesAsync(int transactionId);
    Task<Transaction> CreateTransactionAsync(Transaction transaction);
    Task UpdateBestQueryAsync(int transactionId, int bestQueryId);
}
