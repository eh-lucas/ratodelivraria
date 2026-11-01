namespace Sherlock.Business.Core.Watch;

/// <summary>
/// "Rodar todo dia às N horas UTC", sem agendador.
///
/// Existe para os dois workers não repetirem a mesma aritmética de relógio, que é
/// onde esse tipo de código erra: o caso que quebra é o worker que sobe depois da
/// hora e espera 24 horas quando devia rodar agora, ou o que roda duas vezes
/// porque a rodada anterior terminou antes do fim da hora cheia.
///
/// Tudo em UTC de propósito (§8.2): horário de verão que vai e volta por decreto
/// não pode mudar quando o rato trabalha.
/// </summary>
public static class DailyWindow
{
    /// <summary>
    /// Quanto falta para a próxima ocorrência de <paramref name="horaUtc"/>.
    /// Se já passou hoje, é amanhã.
    /// </summary>
    public static TimeSpan AteProxima(DateTime agoraUtc, int horaUtc)
    {
        var hoje = new DateTime(agoraUtc.Year, agoraUtc.Month, agoraUtc.Day, 0, 0, 0, DateTimeKind.Utc)
            .AddHours(Normalizar(horaUtc));

        return hoje > agoraUtc ? hoje - agoraUtc : hoje.AddDays(1) - agoraUtc;
    }

    /// <summary>
    /// A janela [início, fim) do dia corrente contém este instante? Usado para a
    /// varredura parar quando o horário de folga das lojas acaba.
    /// </summary>
    public static bool DentroDaJanela(DateTime agoraUtc, int inicioHoraUtc, int fimHoraUtc)
    {
        var inicio = Normalizar(inicioHoraUtc);
        var fim = Normalizar(fimHoraUtc);
        var hora = agoraUtc.Hour;

        // Janela que atravessa a meia-noite (22h→04h, por exemplo).
        return inicio <= fim
            ? hora >= inicio && hora < fim
            : hora >= inicio || hora < fim;
    }

    private static int Normalizar(int hora) => ((hora % 24) + 24) % 24;
}
