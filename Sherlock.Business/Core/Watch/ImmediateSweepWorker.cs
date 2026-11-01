using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sherlock.Business.Configuration;

namespace Sherlock.Business.Core.Watch;

/// <summary>
/// Dá a primeira varredura nos livros que acabaram de ser observados.
///
/// Um de cada vez, de propósito. A varredura já abre até
/// <c>Search:MaxDegreeOfParallelism</c> conexões por ISBN, e o gargalo são os 2
/// IPs que as 67 lojas dividem: processar dois ISBNs em paralelo aqui não
/// entregaria antes, só pressionaria mais o servidor delas.
///
/// Independente do <see cref="WatchSettings.SweepEnabled"/>. São coisas
/// diferentes: a rodada da madrugada é trabalho de robô, que pode estar
/// desligado por escolha de carga; esta varredura é consequência direta de
/// alguém ter clicado agora — custa o mesmo que a busca que a pessoa acabou de
/// fazer, e observar é de graça como buscar (§3.4).
/// </summary>
public class ImmediateSweepWorker : BackgroundService
{
    private readonly ImmediateSweepQueue _fila;
    private readonly WatchSweepRunner _runner;
    private readonly WatchSettings _settings;
    private readonly ILogger<ImmediateSweepWorker> _logger;

    public ImmediateSweepWorker(
        ImmediateSweepQueue fila,
        WatchSweepRunner runner,
        IOptions<WatchSettings> settings,
        ILogger<ImmediateSweepWorker> logger)
    {
        _fila = fila;
        _runner = runner;
        _settings = settings?.Value ?? new WatchSettings();
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.SweepOnAddEnabled)
        {
            _logger.LogInformation("[WatchNow] Desligado por configuração");
            return;
        }

        await foreach (var isbn in _fila.LerTudoAsync(stoppingToken))
        {
            try
            {
                await _runner.VarrerIsbnAsync(isbn, "WatchNow", stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // Falhar aqui custa um cartão sem preço até a rodada da
                // madrugada — nunca a requisição de quem adicionou, que ja
                // respondeu com sucesso lá atrás.
                _logger.LogError(ex, "[WatchNow] Falha ao varrer ISBN {Isbn}", isbn);
            }
            finally
            {
                _fila.Concluir(isbn);
            }
        }
    }
}
