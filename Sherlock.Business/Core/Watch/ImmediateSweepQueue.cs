using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Sherlock.Business.Core.Watch;

/// <summary>
/// ISBNs recém-observados esperando a primeira varredura.
///
/// Existe para resolver uma tensão real entre duas coisas certas:
///
/// 1. O cartão do livro precisa nascer com capa, título e preço. Sem isso a
///    pessoa adiciona e vê um número de 13 dígitos e nada mais — e, com o
///    `Watch:SweepEnabled` desligado, continuaria assim indefinidamente, não só
///    até a madrugada.
/// 2. Varrer na própria requisição não serve: uma varredura leva ~17s, e um lote
///    no teto de 30 livros deixaria a pessoa esperando mais de oito minutos
///    enquanto 2.010 requisições saem de uma vez — foi exatamente por isso que o
///    PLANO-OBSERVADOS.md §11 decidiu que "adicionar não dispara busca".
///
/// A fila desfaz a tensão: o `POST` responde na hora e a varredura acontece logo
/// atrás, com o cartão preenchendo em segundos.
///
/// Deduplica de propósito. Se dez pessoas observam *O Hobbit* no mesmo minuto,
/// o livro é varrido uma vez só — o custo da feature é o número de ISBNs
/// distintos, não o de pessoas (§4.1).
/// </summary>
public class ImmediateSweepQueue
{
    // Sem limite de tamanho: o teto real é o de livros por pessoa, e o produtor
    // (um POST) nunca pode ficar bloqueado esperando vaga na fila.
    private readonly Channel<string> _canal = Channel.CreateUnbounded<string>(
        new UnboundedChannelOptions { SingleReader = true });

    /// <summary>
    /// Quem já está na fila ou sendo varrido agora. Sai daqui só quando a
    /// varredura termina, senão dois pedidos seguidos do mesmo ISBN virariam
    /// duas varreduras.
    /// </summary>
    private readonly ConcurrentDictionary<string, byte> _naFila = new();

    public void Enfileirar(string isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn)) return;
        if (!_naFila.TryAdd(isbn, 0)) return;

        if (!_canal.Writer.TryWrite(isbn))
            _naFila.TryRemove(isbn, out _);
    }

    public IAsyncEnumerable<string> LerTudoAsync(CancellationToken cancellationToken)
        => _canal.Reader.ReadAllAsync(cancellationToken);

    /// <summary>Chamado pelo consumidor ao terminar, liberando o ISBN para um novo pedido.</summary>
    public void Concluir(string isbn) => _naFila.TryRemove(isbn, out _);
}
