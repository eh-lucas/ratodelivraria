namespace Sherlock.Infrastructure.Email;

/// <summary>
/// Configuração do envio. Os valores de verdade vêm por variável de ambiente
/// (<c>Email__Host</c>, <c>Email__Password</c>, ...), nunca do appsettings versionado.
/// </summary>
public class EmailSettings
{
    public const string SectionName = "Email";

    /// <summary>
    /// Desligado, nada sai da máquina: o <c>LogEmailSender</c> escreve a mensagem no log
    /// e pronto. É o padrão, para que ninguém dispare e-mail de verdade sem querer ao
    /// clonar o repo.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>Servidor SMTP. Mailpit local: <c>localhost</c>.</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>587 em qualquer relay de provedor; 1025 no Mailpit.</summary>
    public int Port { get; set; } = 1025;

    /// <summary>
    /// STARTTLS na porta 587. Falso só para o Mailpit, que não fala TLS — e é por isso
    /// que ele nunca pode ser o destino em produção.
    /// </summary>
    public bool UseStartTls { get; set; } = false;

    public string? Username { get; set; }
    public string? Password { get; set; }

    /// <summary>Remetente. Tem que ser endereço verificado no provedor, senão ele recusa.</summary>
    public string FromAddress { get; set; } = "nao-responda@localhost";

    /// <summary>Nome exibido na caixa de entrada.</summary>
    public string FromName { get; set; } = "ratodelivraria";

    /// <summary>
    /// Para onde vai a resposta de quem apertar "responder". Vazio = o próprio remetente.
    /// </summary>
    public string? ReplyTo { get; set; }

    public int TimeoutSeconds { get; set; } = 20;

    /// <summary>
    /// Tentativas no total (não "tentativas extras"). SMTP falha por motivo passageiro
    /// com alguma frequência — conexão derrubada, greylisting — e repetir resolve a maior
    /// parte. Espera 1s e depois 3s entre elas.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;
}
