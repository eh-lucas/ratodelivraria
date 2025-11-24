using Microsoft.AspNetCore.Mvc;
using SherlockAPI.Services;
using Sherlock.Data.Context;
using Sherlock.Domain.Entities;

namespace SherlockAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly SherlockDbContext _context;

    public AuthController(SherlockDbContext context)
    {
        _context = context;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] User request)
    {
        if (_context.Users.Any(u => u.Username == request.Username))
            return BadRequest("Usuário já existe.");

        var user = new User()
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.PasswordHash)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok("Usuario registrado com sucesso.");
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] User request)
    {
        var user = _context.Users.FirstOrDefault(u => u.Username == request.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.PasswordHash, user.PasswordHash))
            return Unauthorized("Usuario ou senha inválidos.");

        return Ok("Login efetuado com sucesso.");
    }

    [HttpPost("reset-password")]
    public IActionResult ResetPassword([FromBody] User request)
    {
        var user = _context.Users.FirstOrDefault(u => u.Username == request.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.PasswordHash, user.PasswordHash))
            return Unauthorized("Usuario ou senha inválidos.");

        return Ok("Login efetuado com sucesso.");
    }
}
