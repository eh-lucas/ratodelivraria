namespace Sherlock.Business.Interfaces;

/// <summary>
/// Envio de e-mail. A interface é nossa de propósito: quem entrega do outro lado é
/// configuração (Mailpit em desenvolvimento, relay de um provedor em produção), então
/// trocar de provedor não toca em nenhuma linha de C#.
///
/// Não existe método "enviar em massa". O produto manda um e-mail por pessoa — código de
/// login e o resumo diário dos livros observados — e uma chamada por destinatário é mais
/// simples de acertar do que uma lista com entrega parcial.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Entrega a mensagem. Lança <see cref="EmailSendException"/> se não conseguir.
    ///
    /// Quem chama precisa tratar a falha: para o código de login, o certo é devolver erro
    /// à pessoa na hora (ela tenta de novo); para o aviso diário, registrar e seguir para
    /// o próximo destinatário — um e-mail perdido não pode derrubar a rodada inteira.
    /// </summary>
    /// <param name="to">Destinatário.</param>
    /// <param name="subject">Assunto.</param>
    /// <param name="htmlBody">Corpo em HTML.</param>
    /// <param name="textBody">
    /// Alternativa em texto puro. Vale a pena preencher: mensagem só-HTML pontua pior nos
    /// filtros de spam, e é o código de login que não pode cair lá.
    /// </param>
    Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        string? textBody = null,
        CancellationToken cancellationToken = default);
}

/// <summary>Falha ao entregar a mensagem, depois de esgotadas as tentativas.</summary>
public class EmailSendException : Exception
{
    public EmailSendException(string message, Exception? inner = null) : base(message, inner) { }
}
