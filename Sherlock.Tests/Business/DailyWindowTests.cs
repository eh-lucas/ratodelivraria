using FluentAssertions;
using Sherlock.Business.Core.Watch;

namespace Sherlock.Tests.Business;

/// <summary>
/// Aritmética de relógio é onde esse tipo de worker erra em silêncio: ou dorme 24
/// horas quando devia rodar agora, ou roda duas vezes no mesmo dia.
/// </summary>
public class DailyWindowTests
{
    private static DateTime Utc(int hora, int minuto = 0)
        => new(2026, 10, 31, hora, minuto, 0, DateTimeKind.Utc);

    [Fact]
    public void Antes_da_hora_espera_o_resto_do_dia()
    {
        DailyWindow.AteProxima(Utc(1), horaUtc: 3).Should().Be(TimeSpan.FromHours(2));
    }

    [Fact]
    public void Depois_da_hora_espera_ate_amanha()
    {
        DailyWindow.AteProxima(Utc(5), horaUtc: 3).Should().Be(TimeSpan.FromHours(22));
    }

    [Fact]
    public void Na_hora_cheia_espera_o_dia_inteiro_em_vez_de_disparar_de_novo()
    {
        // Um worker que subisse exatamente às 03h00 e tratasse "agora" como
        // "próxima" rodaria em laço apertado.
        DailyWindow.AteProxima(Utc(3), horaUtc: 3).Should().Be(TimeSpan.FromHours(24));
    }

    [Theory]
    [InlineData(3, true)]
    [InlineData(7, true)]
    [InlineData(8, false)]   // fim exclusivo: às 08h a janela já fechou
    [InlineData(2, false)]
    public void Janela_normal(int hora, bool dentro)
    {
        DailyWindow.DentroDaJanela(Utc(hora), 3, 8).Should().Be(dentro);
    }

    [Theory]
    [InlineData(23, true)]
    [InlineData(1, true)]
    [InlineData(4, false)]
    public void Janela_que_atravessa_a_meia_noite(int hora, bool dentro)
    {
        DailyWindow.DentroDaJanela(Utc(hora), 22, 4).Should().Be(dentro);
    }
}
