using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sherlock.Data.Context;
using Sherlock.Domain.Interfaces;

namespace Sherlock.Data.Locking;

/// <summary>
/// `pg_try_advisory_lock` do Postgres.
///
/// A trava é da *sessão*, não da transação — então a conexão precisa continuar
/// aberta enquanto a rodada dura. Por isso esta classe abre uma conexão própria em
/// vez de usar a do DbContext injetado: o escopo de uma requisição HTTP não
/// sobrevive a uma varredura de cinco horas.
/// </summary>
public class PostgresAdvisoryLock : IAdvisoryLock
{
    private readonly IDbContextFactory<SherlockDbContext> _contextFactory;
    private readonly ILogger<PostgresAdvisoryLock> _logger;

    public PostgresAdvisoryLock(
        IDbContextFactory<SherlockDbContext> contextFactory,
        ILogger<PostgresAdvisoryLock> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<IAsyncDisposable?> TryAcquireAsync(
        long key, CancellationToken cancellationToken = default)
    {
        var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        try
        {
            var conexao = context.Database.GetDbConnection();
            await context.Database.OpenConnectionAsync(cancellationToken);

            await using var comando = conexao.CreateCommand();
            comando.CommandText = "SELECT pg_try_advisory_lock(@key)";

            var parametro = comando.CreateParameter();
            parametro.ParameterName = "key";
            parametro.Value = key;
            comando.Parameters.Add(parametro);

            var conseguiu = await comando.ExecuteScalarAsync(cancellationToken) is true;

            if (!conseguiu)
            {
                await context.DisposeAsync();
                return null;
            }

            return new Trava(context, key, _logger);
        }
        catch
        {
            await context.DisposeAsync();
            throw;
        }
    }

    private sealed class Trava : IAsyncDisposable
    {
        private readonly SherlockDbContext _context;
        private readonly long _key;
        private readonly ILogger _logger;

        public Trava(SherlockDbContext context, long key, ILogger logger)
        {
            _context = context;
            _key = key;
            _logger = logger;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "SELECT pg_advisory_unlock({0})", _key);
            }
            catch (Exception ex)
            {
                // Fechar a conexão já solta a trava; o unlock explícito é só
                // para não depender de quando o pool decide reciclar.
                _logger.LogWarning(ex, "[Lock] Falha ao soltar a trava {Key}", _key);
            }
            finally
            {
                await _context.DisposeAsync();
            }
        }
    }
}
