using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sherlock.Business.Configuration;
using Sherlock.Business.Core.Base;
using Sherlock.Business.Core.Scrapers;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Interfaces;

namespace Sherlock.Business.Core.Prefetch;

/// <summary>
/// Mantém quentes no cache os livros que as pessoas mais procuram.
///
/// A busca fria leva ~17s e esse teto é do servidor das livrarias, não nosso:
/// 67 lojas dividem 2 IPs, e abrir mais concorrência só converte espera em
/// espera. O que dá para fazer é chegar antes — quando o livro já está no cache
/// de 60 minutos, a mesma busca responde em ~0,3s.
///
/// Duas escolhas de projeto que existem para não pesar no servidor deles:
///
/// 1. Um livro por vez, espaçado. Aquecer dez de enfiada seriam 680 requisições
///    na mesma fila em que estão as buscas de gente de verdade.
/// 2. Sem forçar nada. O motor já pula a loja cujo resultado ainda está fresco,
///    então uma passada logo depois da outra custa quase nenhuma requisição —
///    quem paga o custo é só o que realmente esfriou.
/// </summary>
public class PopularBooksPrefetcher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PrefetchSettings _settings;
    private readonly RankingSettings _rankingSettings;
    private readonly SearchPriority _priority;
    private readonly ILogger<PopularBooksPrefetcher> _logger;

    public PopularBooksPrefetcher(
        IServiceScopeFactory scopeFactory,
        IOptions<PrefetchSettings> settings,
        IOptions<RankingSettings> rankingSettings,
        SearchPriority priority,
        ILogger<PopularBooksPrefetcher> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings?.Value ?? new PrefetchSettings();
        _rankingSettings = rankingSettings?.Value ?? new RankingSettings();
        _priority = priority;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("[Prefetch] Desligado por configuração");
            return;
        }

        await Task.Delay(TimeSpan.FromSeconds(_settings.StartupDelaySeconds), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await AquecerAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // Aquecimento é conveniência: se falhar, as buscas continuam
                // funcionando do jeito frio.
                _logger.LogError(ex, "[Prefetch] Rodada falhou");
            }

            await Task.Delay(TimeSpan.FromMinutes(_settings.IntervalMinutes), stoppingToken);
        }
    }

    private async Task AquecerAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var queryRepository = scope.ServiceProvider.GetRequiredService<IQueryRepository>();

        // Mesma lista que a tela mostra, inclusive o marco de reset: aquecer um
        // ranking diferente do exibido seria aquecer o livro errado.
        var populares = await queryRepository.GetMostSearchedAsync(
            _settings.TopBooks, _rankingSettings.ResetAt, cancellationToken);

        if (populares.Count == 0)
        {
            _logger.LogInformation("[Prefetch] Ranking vazio, nada a aquecer");
            return;
        }

        _logger.LogInformation("[Prefetch] Aquecendo {Count} livros", populares.Count);

        var lojas = Provider.AllSources.Where(p => p.IsActive).ToList();

        foreach (var livro in populares)
        {
            if (cancellationToken.IsCancellationRequested) return;

            if (_priority.HaBuscaDeGente)
            {
                // Não entra na fila atrás de alguém que está olhando a tela. O
                // livro espera a próxima rodada; o aquecimento é conveniência.
                _logger.LogInformation(
                    "[Prefetch] ISBN {Isbn} adiado: busca de usuário em curso", livro.Isbn);
                await Task.Delay(TimeSpan.FromSeconds(_settings.DelayBetweenBooksSeconds), cancellationToken);
                continue;
            }

            await AquecerLivroAsync(livro.Isbn, lojas, cancellationToken);

            await Task.Delay(TimeSpan.FromSeconds(_settings.DelayBetweenBooksSeconds), cancellationToken);
        }
    }

    /// <summary>
    /// Cada livro roda no seu próprio escopo: o motor é scoped e segura um
    /// DbContext, que não deve viver por toda a rodada.
    /// </summary>
    private async Task AquecerLivroAsync(string isbn, List<Provider> lojas, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var engine = scope.ServiceProvider.GetRequiredService<W16Engine>();

        var requestor = new Requestor
        {
            SearchParameters = new SearchParameter { Isbn = isbn, IsPrefetch = true },
            SourcesToSearch = lojas,
        };

        // Uma varredura leva mais de um minuto. Se alguém buscar no meio dela, o
        // token cai e o aquecimento abandona o livro na hora — foi a varredura
        // ainda no ar que, em 31/10/2026, levou um carrinho de 34s para 136s.
        var cts = _priority.AbrirAquecimento(cancellationToken);
        try
        {
            var resultado = await engine.ExecuteTransaction(requestor, PrefetchUserId, cts.Token);

            var doCache = resultado.AllQueryResults?.Count(q => q.FromCache) ?? 0;
            var total = resultado.AllQueryResults?.Count ?? 0;

            _logger.LogInformation(
                "[Prefetch] ISBN {Isbn}: {Total} lojas, {Cache} ainda no cache, {Ms}ms",
                isbn, total, doCache, resultado.TempoDecorrido);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("[Prefetch] ISBN {Isbn} interrompido: a vez é de quem está na tela", isbn);
        }
        finally
        {
            _priority.FecharAquecimento(cts);
            cts.Dispose();
        }
    }

    /// <summary>
    /// Usuário a quem as transações do aquecedor são creditadas. É o mesmo id do
    /// usuário master; o que separa aquecimento de busca de gente é a marca
    /// isPrefetch, não o usuário.
    /// </summary>
    private const int PrefetchUserId = 1;
}
