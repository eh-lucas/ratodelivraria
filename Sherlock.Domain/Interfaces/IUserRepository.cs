using Sherlock.Domain.Entities;

namespace Sherlock.Domain.Interfaces;
public interface IUserRepository
{
    IEnumerable<User> GetUsers();
}
