using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sherlock.Business.Configuration;
using Sherlock.Domain.Interfaces;

namespace Sherlock.Business.Core.Watch;

/// <summary>
/// Varre de madrugada o preço de tudo que está sendo observado.
///
/// Duas escolhas explicam o desenho inteiro:
///
/// **O laço é por ISBN distinto, não por pessoa.** Se 500 pessoas observam *O
/// Hobbit*, ele é varrido uma vez e as outras 499 leem o mesmo resultado. O custo
/// da feature é o número de livros diferentes — por isso ela escala para milhares
/// de usuários sem mudar nada (§4.1).
///
/// **03h, não 09h.** Varrer na hora do e-mail seria pôr 67 × N requisições no
/// mesmo instante em que as pessoas começam a usar o site, e o e-mail só sairia ao
/// meio-dia. Varrendo de madrugada, espaçado, as lojas levam o trabalho enquanto
/// estão ociosas e o <see cref="WatchNotifyWorker"/> acha tudo pronto às 09h (§4.3).
/// </summary>
public class WatchSweepWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WatchSettings _settings;
    private readonly WatchSweepRunner _runner;
    private readonly ILogger<WatchSweepWorker> _logger;

    /// <summary>
    /// Chave do advisory lock. Número arbitrário e fixo — só precisa não colidir
    /// com outro lock da aplicação.
    /// </summary>
    private const long LockKey = 8_160_031;

    public WatchSweepWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<WatchSettings> settings,
        WatchSweepRunner runner,
        ILogger<WatchSweepWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings?.Value ?? new WatchSettings();
        _runner = runner;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.SweepEnabled)
        {
            _logger.LogInformation("[WatchSweep] Desligado por configuração");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var espera = DailyWindow.AteProxima(DateTime.UtcNow, _settings.SweepHourUtc);
            _logger.LogInformation("[WatchSweep] Próxima rodada em {Horas:F1}h", espera.TotalHours);

            try
            {
                await Task.Delay(espera, stoppingToken);
                await RodadaAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // Rodada que falha não derruba o worker: o vigia tenta de novo
                // amanhã, e um dia sem varredura é um dia sem aviso — não um
                // serviço fora do ar.
                _logger.LogError(ex, "[WatchSweep] Rodada falhou");
            }
        }
    }

    private async Task RodadaAsync(CancellationToken cancellationToken)
    {
        // Trava entre instâncias. Com uma réplica é inócua; no dia em que o
        // compose subir duas, evita varredura dobrada em cima das lojas — e é o
        // tipo de coisa que só se descobre que faltou em produção (§4.4).
        using var scope = _scopeFactory.CreateScope();
        var travas = scope.ServiceProvider.GetRequiredService<IAdvisoryLock>();

        await using var trava = await travas.TryAcquireAsync(LockKey, cancellationToken);
        if (trava == null)
        {
            _logger.LogInformation("[WatchSweep] Outra instância já está varrendo; esta rodada passa");
            return;
        }

        var repositorio = scope.ServiceProvider.GetRequiredService<IWatchedBookRepository>();
        var isbns = await OrdenarAsync(repositorio, cancellationToken);

        if (isbns.Count == 0)
        {
            _logger.LogInformation("[WatchSweep] Nada sendo observado");
            return;
        }

        _logger.LogInformation("[WatchSweep] Rodada começando: {Count} ISBNs distintos", isbns.Count);

        var vagas = new SemaphoreSlim(Math.Max(1, _settings.MaxConcurrentBooks));
        var tarefas = new List<Task>();
        var varridos = 0;

        foreach (var isbn in isbns)
        {
            if (cancellationToken.IsCancellationRequested) break;

            if (!DailyWindow.DentroDaJanela(DateTime.UtcNow, _settings.SweepHourUtc, _settings.SweepUntilHourUtc))
            {
                // A janela existe para as lojas levarem o trabalho enquanto estão
                // ociosas. Passou dela, o resto fica para amanhã — terminar a
                // rodada a qualquer custo seria justamente o que ela evita.
                _logger.LogWarning(
                    "[WatchSweep] Janela encerrada com {Restantes} ISBNs pendentes", isbns.Count - varridos);
                break;
            }

            await vagas.WaitAsync(cancellationToken);
            varridos++;

            tarefas.Add(Task.Run(async () =>
            {
                try
                {
                    await _runner.VarrerIsbnAsync(isbn, "WatchSweep", cancellationToken);
                }
                catch (Exception ex)
                {
                    // Falha de um ISBN não derruba a rodada.
                    _logger.LogError(ex, "[WatchSweep] ISBN {Isbn} falhou", isbn);
                }
                finally
                {
                    vagas.Release();
                }
            }, cancellationToken));

            await Task.Delay(TimeSpan.FromSeconds(_settings.DelayBetweenBooksSeconds), cancellationToken);
        }

        await Task.WhenAll(tarefas);
        _logger.LogInformation("[WatchSweep] Rodada terminada: {Count} ISBNs varridos", varridos);
    }

    /// <summary>
    /// Quem nunca foi varrido vai na frente.
    ///
    /// Livro recém-adicionado tem referência nula — o <c>WatchedBookService</c> só
    /// preenche a partir do histórico que já existia. Até a primeira varredura o
    /// cartão aparece sem preço nenhum, e é essa a espera que mais incomoda quem
    /// acabou de usar a tela.
    /// </summary>
    private static async Task<List<string>> OrdenarAsync(
        IWatchedBookRepository repositorio, CancellationToken cancellationToken)
    {
        var ativos = await repositorio.GetAllActiveWithUserAsync(cancellationToken);

        return ativos
            .GroupBy(w => w.Isbn)
            .OrderByDescending(g => g.Any(w => w.LastCheckedAt == null))
            .ThenBy(g => g.Min(w => w.LastCheckedAt ?? DateTime.MinValue))
            .Select(g => g.Key)
            .ToList();
    }
}
