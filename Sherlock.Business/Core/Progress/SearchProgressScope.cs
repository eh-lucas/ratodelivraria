namespace Sherlock.Business.Core.Progress;

/// <summary>
/// Uma oferta que já chegou, enquanto as outras lojas ainda respondem.
///
/// A busca inteira leva ~17s porque 67 livrarias dividem 2 IPs — esse teto é do
/// servidor deles, não nosso. Mas a primeira loja responde em 1,5s: mostrar o
/// que já chegou tira a espera da frente do usuário sem gerar uma requisição a
/// mais.
/// </summary>
public record PartialOffer(
    int ProviderId,
    string ProviderName,
    string ProviderUrl,
    string? Title,
    string? Author,
    decimal Price,
    int Discount,
    string? ProductUrl,
    string? ImageUrl,
    long ResponseTimeMs,
    bool FromCache);

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
    private readonly System.Collections.Concurrent.ConcurrentBag<PartialOffer> _offers = new();

    public int Total { get; private set; }
    public int Completed => Volatile.Read(ref _completed);
    public bool Done { get; private set; }
    public string? Error { get; private set; }
    public object? Result { get; private set; }
    public DateTime StartedAt { get; } = DateTime.UtcNow;

    public void SetTotal(int total) => Total = total;

    public void Increment() => Interlocked.Increment(ref _completed);

    /// <summary>
    /// Fecha várias lojas de uma vez. Usado quando uma categoria inteira não tem
    /// scraper: sem isso a barra ficaria parada em 67 de 68 até o fim.
    /// </summary>
    public void Increment(int count) => Interlocked.Add(ref _completed, count);

    /// <summary>
    /// Registra uma oferta já respondida. Não conta progresso: quem conta é o
    /// <see cref="Increment()"/>, chamado para toda loja, com ou sem resultado.
    /// </summary>
    public void AddOffer(PartialOffer offer) => _offers.Add(offer);

    /// <summary>Ofertas que chegaram até agora, da mais barata para a mais cara.</summary>
    public IReadOnlyList<PartialOffer> Offers =>
        _offers.OrderBy(o => o.Price).ToList();

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
