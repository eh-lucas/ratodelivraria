using Sherlock.Business.DTOs;
using Sherlock.Business.Interfaces;
using Sherlock.Domain.Interfaces;
using System.Runtime.CompilerServices;

namespace Sherlock.Business.Services;
public class UserService : IUserService
{
    private IUserRepository _userRepository;
    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }
    public IEnumerable<UserDto> GetUsers()
    {
        var users = _userRepository.GetUsers();
        var usersDto = new List<UserDto>();

        return usersDto;
    }
}
