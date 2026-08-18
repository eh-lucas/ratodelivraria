using System.Collections.Concurrent;

namespace Sherlock.Business.Core.Progress;

/// <summary>
/// Guarda o andamento das buscas para o front consultar por polling.
///
/// Fica em memória de propósito: são poucos registros, vivem segundos e a API roda em
/// instância única. Se o processo reiniciar, a busca é refeita — nada a preservar.
/// </summary>
public class SearchProgressStore
{
    private readonly ConcurrentDictionary<string, SearchProgress> _jobs = new();

    /// <summary>Registros mais velhos que isso são descartados na próxima limpeza.</summary>
    private static readonly TimeSpan Retention = TimeSpan.FromMinutes(15);

    public SearchProgress Create(string jobId)
    {
        CleanupExpired();
        var progress = new SearchProgress();
        _jobs[jobId] = progress;
        return progress;
    }

    public SearchProgress? Get(string jobId) =>
        _jobs.TryGetValue(jobId, out var progress) ? progress : null;

    private void CleanupExpired()
    {
        var cutoff = DateTime.UtcNow - Retention;

        foreach (var (id, progress) in _jobs)
        {
            if (progress.StartedAt < cutoff)
                _jobs.TryRemove(id, out _);
        }
    }
}
