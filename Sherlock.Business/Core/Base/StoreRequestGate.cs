using Microsoft.Extensions.Options;
using Sherlock.Business.Configuration;

namespace Sherlock.Business.Core.Base;

/// <summary>
/// Teto de requisições simultâneas às livrarias, somando TODAS as buscas em
/// andamento.
///
/// O semáforo do motor sempre nasceu dentro de cada busca: uma busca abria 20
/// conexões, cinco buscas simultâneas abriam 100, e ninguém governava isso. Só
/// que do outro lado não há 67 servidores — há 2 IPs, e toda concorrência que
/// abrimos cai na mesma fila.
///
/// Com o teto compartilhado, o servidor deles vê a mesma carga com 1 ou com 20
/// pessoas buscando. O que muda é o tempo de cada busca, que degrada junto em
/// vez de recusar alguém: o visitante não fez nada de errado para levar um "volte
/// mais tarde" porque outra pessoa está buscando.
///
/// Isto NÃO é o limite anti-abuso — esse é o rate limiter, por visitante, e
/// devolve 429. Aqui a espera é represamento, não recusa.
/// </summary>
public class StoreRequestGate
{
    private readonly SemaphoreSlim _slots;

    public StoreRequestGate(IOptions<SearchSettings>? settings = null)
    {
        var configurado = settings?.Value.MaxGlobalParallelism ?? 0;
        Limite = configurado > 0 ? configurado : SearchSettings.PadraoGlobal;
        _slots = new SemaphoreSlim(Limite);
    }

    /// <summary>Quantas requisições podem estar em voo ao mesmo tempo, no total.</summary>
    public int Limite { get; }

    /// <summary>Vagas livres agora — só para métrica e log.</summary>
    public int Disponiveis => _slots.CurrentCount;

    public async Task<IDisposable> EntrarAsync(CancellationToken cancellationToken)
    {
        await _slots.WaitAsync(cancellationToken);
        return new Saida(_slots);
    }

    private sealed class Saida : IDisposable
    {
        private readonly SemaphoreSlim _slots;
        private int _liberado;

        public Saida(SemaphoreSlim slots) => _slots = slots;

        public void Dispose()
        {
            // Release duplo estoura o semáforo e afrouxaria o teto justamente
            // sob carga, que é quando ele importa.
            if (Interlocked.Exchange(ref _liberado, 1) == 0)
                _slots.Release();
        }
    }
}
