using Microsoft.AspNetCore.Mvc;
using SherlockAPI.Services;
using SherlockAPI.DTOs;
using Sherlock.Data.Context;
using Sherlock.Domain.Entities;

namespace SherlockAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly SherlockDbContext _context;
    private readonly TokenService _tokenService;

    public AuthController(SherlockDbContext context, TokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] LoginRequestDTO request)
    {
        if (_context.Users.Any(u => u.Username == request.Username))
            return BadRequest("Usuário já existe.");

        var user = new User()
        {
            Username = request.Username,
            Email = "",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok("Usuario registrado com sucesso.");
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequestDTO request)
    {
        var user = _context.Users.FirstOrDefault(u => u.Username == request.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized("Usuario ou senha inválidos.");

        var token = _tokenService.GenerateToken(user.Id.ToString(), "User");

        return Ok(new LoginResponseDTO
        {
            Token = token,
            Username = user.Username
        });
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
