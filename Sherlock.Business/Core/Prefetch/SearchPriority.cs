using Microsoft.Extensions.Logging;

namespace Sherlock.Business.Core.Prefetch;

/// <summary>
/// Dá a vez para quem está esperando na tela.
///
/// O aquecedor e a busca de gente de verdade disputam a mesma fila: 67 lojas
/// atrás de 2 IPs. Enquanto os dois correm juntos, ninguém anda. Em 31/10/2026
/// um aquecimento de 77,8s ainda estava no ar quando um carrinho disparou — o
/// carrinho foi de ~34s para 131-136s e 23 das 25 falhas foram timeout. Uma
/// delas custou a melhor oferta do carrinho.
///
/// A regra é uma só: busca de pessoa entra, aquecimento sai. O aquecimento é
/// conveniência e pode esperar a próxima rodada; a pessoa na tela, não.
/// </summary>
public class SearchPriority
{
    private readonly ILogger<SearchPriority> _logger;
    private readonly object _trava = new();

    private int _buscasDeGente;
    private CancellationTokenSource? _aquecimentoAtual;

    public SearchPriority(ILogger<SearchPriority> logger)
    {
        _logger = logger;
    }

    /// <summary>Tem alguém esperando resposta na tela agora.</summary>
    public bool HaBuscaDeGente
    {
        get { lock (_trava) return _buscasDeGente > 0; }
    }

    /// <summary>
    /// Marca o começo de uma busca pedida por uma pessoa e derruba o
    /// aquecimento que estiver no ar. Descartar o retorno encerra a marcação.
    /// </summary>
    public IDisposable EntrarBuscaDeGente()
    {
        CancellationTokenSource? aDerrubar = null;

        lock (_trava)
        {
            _buscasDeGente++;
            if (_buscasDeGente == 1 && _aquecimentoAtual is { IsCancellationRequested: false })
            {
                aDerrubar = _aquecimentoAtual;
            }
        }

        if (aDerrubar is not null)
        {
            _logger.LogInformation("[Prioridade] Busca de usuário chegou, cedendo o aquecimento em curso");
            try { aDerrubar.Cancel(); } catch (ObjectDisposedException) { }
        }

        return new Saida(this);
    }

    /// <summary>
    /// Token do aquecimento, atrelado ao <paramref name="externo"/> e cancelado
    /// assim que uma busca de gente começar. O aquecedor usa o token no lugar do
    /// seu próprio e desiste do livro quando ele cair.
    /// </summary>
    public CancellationTokenSource AbrirAquecimento(CancellationToken externo)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(externo);

        lock (_trava)
        {
            _aquecimentoAtual = cts;
            if (_buscasDeGente > 0)
            {
                // Já tem gente esperando: nem começa.
                cts.Cancel();
            }
        }

        return cts;
    }

    public void FecharAquecimento(CancellationTokenSource cts)
    {
        lock (_trava)
        {
            if (ReferenceEquals(_aquecimentoAtual, cts))
                _aquecimentoAtual = null;
        }
    }

    private sealed class Saida : IDisposable
    {
        private readonly SearchPriority _dono;
        private int _jaSaiu;

        public Saida(SearchPriority dono) => _dono = dono;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _jaSaiu, 1) != 0) return;
            lock (_dono._trava) _dono._buscasDeGente--;
        }
    }
}
