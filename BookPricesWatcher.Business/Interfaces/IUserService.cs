using System;
using Sherlock.Business.DTOs;

namespace Sherlock.Business.Interfaces
{
    public interface IUserService
    {
        IEnumerable<UserDto> GetUsers();
    }
}
