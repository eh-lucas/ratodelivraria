using Microsoft.Extensions.Logging;
using Sherlock.Business.Interfaces;

namespace Sherlock.Infrastructure.Email;

/// <summary>
/// Não envia nada: escreve a mensagem inteira no log. É o que roda quando
/// <c>Email:Enabled</c> é falso, que é o padrão.
///
/// Serve para desenvolver sem container nenhum: o código de login aparece no console da
/// API e dá para entrar no site. Quando quiser ver o e-mail montado de verdade, com HTML
/// e tudo, suba o Mailpit (<c>docker compose -f docker-compose.dev.yml up -d mailpit</c>)
/// e ligue o <c>Email:Enabled</c>.
///
/// Este é o único lugar do sistema que registra endereço de e-mail em log, e é
/// deliberado — contraria o docs/PLANO-OBSERVADOS.md §2.3 porque sem o destinatário a
/// classe não serve para depurar. Por isso ele avisa alto na inicialização: se este log
/// aparecer em produção, está errado, e a pessoa que tentou entrar não recebeu nada.
/// </summary>
public class LogEmailSender : IEmailSender
{
    private readonly ILogger<LogEmailSender> _logger;

    public LogEmailSender(ILogger<LogEmailSender> logger)
    {
        _logger = logger;
        _logger.LogWarning(
            "[Email] DESLIGADO (Email:Enabled=false). Nenhuma mensagem sai da máquina — " +
            "elas são escritas neste log. Em produção isto significa que ninguém consegue entrar.");
    }

    public Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        string? textBody = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[Email] (não enviado)\n  Para: {To}\n  Assunto: {Subject}\n  Corpo:\n{Body}",
            to, subject, textBody ?? htmlBody);

        return Task.CompletedTask;
    }
}
