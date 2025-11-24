using Sherlock.Data.Context;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Interfaces;

namespace Sherlock.Data.Repositories;
public class UserRepository : IUserRepository
{
    private SherlockDbContext _context;

    public UserRepository(SherlockDbContext context)
    {
        _context = context;
    }
    public IEnumerable<User> GetUsers()
    {
        return _context.Users;
    }
}
