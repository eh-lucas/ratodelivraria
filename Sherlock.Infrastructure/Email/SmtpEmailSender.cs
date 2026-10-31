using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Sherlock.Business.Interfaces;

namespace Sherlock.Infrastructure.Email;

/// <summary>
/// Envio por SMTP puro, via MailKit.
///
/// SMTP em vez do SDK HTTP do provedor é escolha deliberada: é o mesmo protocolo em
/// Mailpit, Brevo, Resend ou SES, então trocar de provedor é mudar quatro variáveis de
/// ambiente. O preço é abrir mão dos extras de cada painel; o ganho é que a decisão de
/// provedor nunca fica presa no código.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailSettings> settings, ILogger<SmtpEmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        string? textBody = null,
        CancellationToken cancellationToken = default)
    {
        var message = Montar(to, subject, htmlBody, textBody);
        var tentativas = Math.Max(1, _settings.MaxAttempts);
        Exception? ultimaFalha = null;

        for (var tentativa = 1; tentativa <= tentativas; tentativa++)
        {
            try
            {
                await EntregarAsync(message, cancellationToken);

                // O destinatário NÃO entra no log — ver docs/PLANO-OBSERVADOS.md §2.3.
                // O log serve para saber que o envio funcionou, não para reconstruir
                // quem usa o site.
                _logger.LogInformation(
                    "[Email] Enviado: assunto {Subject}, tentativa {Tentativa}", subject, tentativa);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                ultimaFalha = ex;
                _logger.LogWarning(
                    "[Email] Tentativa {Tentativa}/{Total} falhou: {Erro}",
                    tentativa, tentativas, ex.Message);

                if (tentativa < tentativas)
                {
                    // 1s, depois 3s. Curto porque do outro lado pode ter alguém esperando
                    // o código de login na tela.
                    var espera = tentativa == 1 ? 1 : 3;
                    await Task.Delay(TimeSpan.FromSeconds(espera), cancellationToken);
                }
            }
        }

        _logger.LogError(ultimaFalha, "[Email] Desisti depois de {Total} tentativas", tentativas);
        throw new EmailSendException(
            $"Não foi possível enviar o e-mail depois de {tentativas} tentativas.", ultimaFalha);
    }

    private MimeMessage Montar(string to, string subject, string htmlBody, string? textBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        if (!string.IsNullOrWhiteSpace(_settings.ReplyTo))
            message.ReplyTo.Add(MailboxAddress.Parse(_settings.ReplyTo));

        var corpo = new BodyBuilder { HtmlBody = htmlBody };

        if (!string.IsNullOrWhiteSpace(textBody))
            corpo.TextBody = textBody;

        message.Body = corpo.ToMessageBody();
        return message;
    }

    private async Task EntregarAsync(MimeMessage message, CancellationToken cancellationToken)
    {
        using var client = new SmtpClient
        {
            Timeout = _settings.TimeoutSeconds * 1000
        };

        var seguranca = _settings.UseStartTls
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.None;

        await client.ConnectAsync(_settings.Host, _settings.Port, seguranca, cancellationToken);

        // Mailpit aceita conexão sem autenticação; provedor nenhum aceita.
        if (!string.IsNullOrWhiteSpace(_settings.Username))
        {
            await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
    }
}
