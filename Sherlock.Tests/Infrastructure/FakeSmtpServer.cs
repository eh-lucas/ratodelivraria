using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Sherlock.Tests.Infrastructure;

/// <summary>
/// Servidor SMTP de mentira, em processo, só o suficiente para o MailKit conversar:
/// saudação, EHLO, MAIL FROM, RCPT TO, DATA, QUIT.
///
/// Existe para que o teste exercite o <c>SmtpEmailSender</c> de verdade — socket,
/// protocolo e montagem da mensagem — sem Docker e sem rede. Um mock de
/// <c>IEmailSender</c> não provaria nada: o que pode quebrar está justamente no pedaço
/// que o mock substituiria.
/// </summary>
internal sealed class FakeSmtpServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly int _conexoesQueFalham;
    private readonly List<string> _mensagens = new();
    private readonly CancellationTokenSource _cts = new();
    private int _conexoes;

    /// <param name="conexoesQueFalham">
    /// Quantas conexões iniciais derrubar antes de começar a aceitar. Serve para testar
    /// a repetição do remetente.
    /// </param>
    public FakeSmtpServer(int conexoesQueFalham = 0)
    {
        _conexoesQueFalham = conexoesQueFalham;
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _ = Task.Run(AceitarAsync);
    }

    public int Port { get; }

    /// <summary>Quantas vezes alguém conectou — é o contador de tentativas.</summary>
    public int Conexoes => Volatile.Read(ref _conexoes);

    public IReadOnlyList<string> Mensagens
    {
        get { lock (_mensagens) return _mensagens.ToList(); }
    }

    private async Task AceitarAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            TcpClient cliente;
            try
            {
                cliente = await _listener.AcceptTcpClientAsync(_cts.Token);
            }
            catch (OperationCanceledException) { return; }
            catch (ObjectDisposedException) { return; }
            catch (SocketException) { return; }

            var numero = Interlocked.Increment(ref _conexoes);
            _ = Task.Run(() => AtenderAsync(cliente, numero));
        }
    }

    private async Task AtenderAsync(TcpClient cliente, int numero)
    {
        using (cliente)
        {
            // Derruba sem dizer nada: é o que o MailKit vê como falha de conexão.
            if (numero <= _conexoesQueFalham)
                return;

            using var stream = cliente.GetStream();
            using var leitor = new StreamReader(stream, Encoding.ASCII);
            using var escritor = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true, NewLine = "\r\n" };

            await escritor.WriteLineAsync("220 fake ESMTP");

            while (await leitor.ReadLineAsync() is { } linha)
            {
                var comando = linha.Split(' ')[0].ToUpperInvariant();

                switch (comando)
                {
                    case "EHLO":
                        // Nenhuma capacidade anunciada de propósito: sem AUTH e sem
                        // STARTTLS o MailKit vai direto ao ponto.
                        await escritor.WriteLineAsync("250-fake");
                        await escritor.WriteLineAsync("250 HELP");
                        break;

                    case "HELO":
                    case "MAIL":
                    case "RCPT":
                    case "RSET":
                        await escritor.WriteLineAsync("250 OK");
                        break;

                    case "DATA":
                        await escritor.WriteLineAsync("354 manda");
                        var corpo = new StringBuilder();
                        while (await leitor.ReadLineAsync() is { } dado && dado != ".")
                            corpo.AppendLine(dado);
                        lock (_mensagens) _mensagens.Add(corpo.ToString());
                        await escritor.WriteLineAsync("250 OK");
                        break;

                    case "QUIT":
                        await escritor.WriteLineAsync("221 tchau");
                        return;

                    default:
                        await escritor.WriteLineAsync("502 nao implementado");
                        break;
                }
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Stop();
        _cts.Dispose();
    }
}
