using Microsoft.EntityFrameworkCore;
using Sherlock.Data.Context;

namespace Sherlock.Api.Services;

/// <summary>
/// Gera um `username` que não colide.
///
/// A coluna é única no banco e sempre foi derivada do começo do e-mail — só que
/// prefixo de e-mail não é único: lucas@gmail.com e lucas@outlook.com dão os dois
/// "lucas". Antes isso virava um `DbUpdateException` e um 500 no cadastro; agora
/// que achar-ou-criar roda a cada login pelo Google, viraria falha no login de quem
/// não fez nada de errado.
///
/// O campo hoje serve só de rótulo interno — quem aparece nas telas é o
/// `first_name`. Por isso o sufixo numérico é resposta suficiente: ninguém digita
/// nem vê este valor.
/// </summary>
public class UsernameService
{
    private readonly SherlockDbContext _context;

    public UsernameService(SherlockDbContext context) => _context = context;

    public async Task<string> GerarAPartirDoEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var baseName = email.Split('@')[0];

        // A coluna aceita 100; sobra espaço para o sufixo.
        if (baseName.Length > 90) baseName = baseName[..90];
        if (string.IsNullOrWhiteSpace(baseName)) baseName = "usuario";

        var candidato = baseName;
        var sufixo = 1;

        while (await _context.Users.AnyAsync(u => u.Username == candidato, cancellationToken))
        {
            candidato = $"{baseName}{++sufixo}";
        }

        return candidato;
    }
}
