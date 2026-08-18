namespace Sherlock.Business.Core.Progress;

/// <summary>
/// Contador de lojas já respondidas na busca em andamento.
///
/// Propagado por <see cref="AsyncLocal{T}"/> para que o motor possa reportar progresso
/// sem que toda a cadeia de chamadas precise carregar um parâmetro extra. O valor flui
/// naturalmente para as tasks paralelas disparadas dentro do escopo.
/// </summary>
public class SearchProgress
{
    private int _completed;

    public int Total { get; private set; }
    public int Completed => Volatile.Read(ref _completed);
    public bool Done { get; private set; }
    public string? Error { get; private set; }
    public object? Result { get; private set; }
    public DateTime StartedAt { get; } = DateTime.UtcNow;

    public void SetTotal(int total) => Total = total;

    public void Increment() => Interlocked.Increment(ref _completed);

    public void Complete(object result)
    {
        Result = result;
        Done = true;
    }

    public void Fail(string error)
    {
        Error = error;
        Done = true;
    }
}

public static class SearchProgressScope
{
    private static readonly AsyncLocal<SearchProgress?> Value = new();

    public static SearchProgress? Current => Value.Value;

    /// <summary>Passa a reportar progresso neste fluxo assíncrono e nos filhos dele.</summary>
    public static void Begin(SearchProgress progress) => Value.Value = progress;

    public static void End() => Value.Value = null;
}
