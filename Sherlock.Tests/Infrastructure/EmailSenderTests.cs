using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sherlock.Business.Interfaces;
using Sherlock.Infrastructure.Email;

namespace Sherlock.Tests.Infrastructure;

public class SmtpEmailSenderTests
{
    private static SmtpEmailSender Remetente(int porta, int tentativas = 3) =>
        new(
            Options.Create(new EmailSettings
            {
                Enabled = true,
                Host = "127.0.0.1",
                Port = porta,
                UseStartTls = false,
                FromAddress = "nao-responda@ratodelivraria.com.br",
                FromName = "ratodelivraria",
                MaxAttempts = tentativas,
                TimeoutSeconds = 5
            }),
            NullLogger<SmtpEmailSender>.Instance);

    [Fact]
    public async Task Entrega_a_mensagem_com_destinatario_assunto_e_corpo()
    {
        using var servidor = new FakeSmtpServer();

        await Remetente(servidor.Port).SendAsync(
            to: "pessoa@exemplo.com",
            subject: "Seu codigo de acesso",
            htmlBody: "<p>Seu codigo e <b>123456</b></p>",
            textBody: "Seu codigo e 123456");

        servidor.Mensagens.Should().ContainSingle();

        var mensagem = servidor.Mensagens[0];
        mensagem.Should().Contain("pessoa@exemplo.com");
        mensagem.Should().Contain("nao-responda@ratodelivraria.com.br");
        mensagem.Should().Contain("codigo de acesso");
    }

    [Fact]
    public async Task Manda_as_duas_versoes_do_corpo_quando_ha_texto_puro()
    {
        using var servidor = new FakeSmtpServer();

        await Remetente(servidor.Port).SendAsync(
            "pessoa@exemplo.com", "Assunto", "<p>oi</p>", "oi");

        // multipart/alternative é o que faz a mensagem não ser só-HTML — ver o comentário
        // em IEmailSender sobre pontuação de spam.
        servidor.Mensagens[0].Should().Contain("multipart/alternative");
    }

    [Fact]
    public async Task Tenta_de_novo_quando_a_conexao_cai()
    {
        // A primeira conexão é derrubada; a segunda funciona.
        using var servidor = new FakeSmtpServer(conexoesQueFalham: 1);

        await Remetente(servidor.Port, tentativas: 2).SendAsync(
            "pessoa@exemplo.com", "Assunto", "<p>oi</p>");

        servidor.Conexoes.Should().Be(2);
        servidor.Mensagens.Should().ContainSingle();
    }

    [Fact]
    public async Task Lanca_EmailSendException_quando_esgota_as_tentativas()
    {
        using var servidor = new FakeSmtpServer(conexoesQueFalham: 99);

        var acao = () => Remetente(servidor.Port, tentativas: 2).SendAsync(
            "pessoa@exemplo.com", "Assunto", "<p>oi</p>");

        // Quem chama precisa conseguir distinguir "não entreguei" de qualquer outro erro:
        // no código de login isso vira mensagem na tela, no aviso diário vira pular para
        // o próximo destinatário.
        await acao.Should().ThrowAsync<EmailSendException>();
        servidor.Conexoes.Should().Be(2);
    }
}

public class LogEmailSenderTests
{
    [Fact]
    public async Task Nao_envia_nada_e_nao_quebra()
    {
        var remetente = new LogEmailSender(NullLogger<LogEmailSender>.Instance);

        var acao = () => remetente.SendAsync("pessoa@exemplo.com", "Assunto", "<p>oi</p>");

        await acao.Should().NotThrowAsync();
    }
}
