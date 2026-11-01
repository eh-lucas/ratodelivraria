using FluentAssertions;
using Sherlock.Business.Configuration;
using Sherlock.Business.Core.Watch;
using Sherlock.Domain.Entities;

namespace Sherlock.Tests.Business;

/// <summary>
/// Esta é a regra que decide se o produto vale alguma coisa. Um vigia de preço só
/// vale o que vale a confiança no aviso dele: depois de dois ou três e-mails que
/// não eram oportunidade, a pessoa para de abrir — e aí o aviso verdadeiro passa
/// batido junto. Daí o teste do caso que mente (§3.3).
/// </summary>
public class WatchRuleEvaluatorTests
{
    private static readonly DateTime Agora = new(2026, 10, 31, 12, 0, 0, DateTimeKind.Utc);

    private static WatchRuleEvaluator Avaliador(WatchSettings? s = null)
        => new(s ?? new WatchSettings());

    private static WatchedBook Livro(
        WatchRule regra = WatchRule.PercentDrop,
        decimal? hoje = null,
        decimal? ontem = null,
        decimal? alvo = null,
        int queda = 15)
        => new()
        {
            Isbn = "9788535914849",
            Title = "O Hobbit",
            Active = true,
            Rule = regra,
            TargetPrice = alvo,
            DropPercent = queda,
            LastPrice = hoje,
            PreviousPrice = ontem,
        };

    // ---------- preço-alvo ----------

    [Fact]
    public void Alvo_dispara_ao_chegar_no_preco()
    {
        var decisao = Avaliador().Avaliar(
            Livro(WatchRule.TargetPrice, hoje: 49.90m, alvo: 50m), menor30d: 60m, Agora);

        decisao.Notify.Should().BeTrue();
        decisao.Reason.Should().Contain("preço-alvo");
    }

    [Fact]
    public void Alvo_nao_dispara_um_centavo_acima()
    {
        Avaliador().Avaliar(Livro(WatchRule.TargetPrice, hoje: 50.01m, alvo: 50m), 60m, Agora)
            .Notify.Should().BeFalse();
    }

    [Fact]
    public void Alvo_sem_valor_nao_dispara()
    {
        // Regra de alvo sem alvo guardado seria um livro observado em silêncio
        // para sempre. O serviço impede de gravar assim; aqui o avaliador confere.
        Avaliador().Avaliar(Livro(WatchRule.TargetPrice, hoje: 10m, alvo: null), 60m, Agora)
            .Notify.Should().BeFalse();
    }

    // ---------- queda acumulada (a) ----------

    [Fact]
    public void Queda_acumulada_dispara_mesmo_sem_queda_no_dia()
    {
        // 100 → 97 → 94 → 91 → 88 → 85 ao longo da semana: nenhum dia caiu 15%,
        // mas hoje é o melhor preço do mês com folga. É o caso que a regra de
        // "queda desde ontem" sozinha perderia.
        var decisao = Avaliador().Avaliar(Livro(hoje: 85m, ontem: 88m), menor30d: 100m, Agora);

        decisao.Notify.Should().BeTrue();
        decisao.Reason.Should().Contain("menor preço");
    }

    // ---------- queda no dia, com guarda-corpo (b) ----------

    [Fact]
    public void Queda_no_dia_dispara_quando_hoje_tambem_e_o_melhor_do_mes()
    {
        // Spike para R$200 e volta para R$90, com o normal em R$100: é novo piso
        // e caiu forte. Avisa.
        var decisao = Avaliador().Avaliar(Livro(hoje: 90m, ontem: 200m), menor30d: 95m, Agora);

        decisao.Notify.Should().BeTrue();
        decisao.Reason.Should().Contain("desde ontem");
    }

    [Fact]
    public void Queda_no_dia_nao_dispara_se_hoje_ainda_esta_acima_do_melhor_do_mes()
    {
        // O caso que mente, e o motivo do guarda-corpo existir:
        //   preço normal há um mês .... R$100
        //   ontem ..................... R$130
        //   hoje ...................... R$105
        // Pela queda diária isso é −19%, "corre que vai acabar". Na prática a
        // pessoa compraria por R$105 um livro que custa R$100 na maior parte do
        // tempo: ela sai PIOR por causa do nosso e-mail.
        Avaliador().Avaliar(Livro(hoje: 105m, ontem: 130m), menor30d: 100m, Agora)
            .Notify.Should().BeFalse();
    }

    [Fact]
    public void Livro_recem_adicionado_sem_historico_usa_so_o_menor_conhecido()
    {
        // Um ponto só de histórico: menor30d é o próprio preço de referência, e as
        // duas portas dão a mesma resposta. O guarda-corpo não custa nada aqui.
        Avaliador().Avaliar(Livro(hoje: 100m, ontem: null), menor30d: 100m, Agora)
            .Notify.Should().BeFalse();

        Avaliador().Avaliar(Livro(hoje: 80m, ontem: null), menor30d: 100m, Agora)
            .Notify.Should().BeTrue();
    }

    [Fact]
    public void Percentual_da_pessoa_e_respeitado()
    {
        var folgado = Livro(hoje: 95m, ontem: 100m, queda: 5);
        Avaliador().Avaliar(folgado, menor30d: 100m, Agora).Notify.Should().BeTrue();

        var exigente = Livro(hoje: 95m, ontem: 100m, queda: 40);
        Avaliador().Avaliar(exigente, menor30d: 100m, Agora).Notify.Should().BeFalse();
    }

    // ---------- antifadiga ----------

    [Fact]
    public void Nao_repete_o_aviso_dentro_do_silencio()
    {
        var livro = Livro(hoje: 50m, ontem: 100m);
        livro.LastNotifiedAt = Agora.AddDays(-2);
        livro.LastNotifiedPrice = 52m;

        Avaliador().Avaliar(livro, menor30d: 60m, Agora).Notify.Should().BeFalse();
    }

    [Fact]
    public void Quebra_o_silencio_se_cair_outra_fatia()
    {
        // Avisado a R$52; hoje R$40 é 23% abaixo disso. Vale um segundo e-mail.
        var livro = Livro(hoje: 40m, ontem: 52m);
        livro.LastNotifiedAt = Agora.AddDays(-2);
        livro.LastNotifiedPrice = 52m;

        Avaliador().Avaliar(livro, menor30d: 45m, Agora).Notify.Should().BeTrue();
    }

    [Fact]
    public void Volta_a_avisar_depois_do_silencio()
    {
        var livro = Livro(hoje: 50m, ontem: 100m);
        livro.LastNotifiedAt = Agora.AddDays(-8);
        livro.LastNotifiedPrice = 52m;

        Avaliador().Avaliar(livro, menor30d: 60m, Agora).Notify.Should().BeTrue();
    }

    // ---------- bordas ----------

    [Fact]
    public void Pausado_nunca_avisa()
    {
        var livro = Livro(hoje: 10m, ontem: 100m);
        livro.Active = false;

        Avaliador().Avaliar(livro, menor30d: 100m, Agora).Notify.Should().BeFalse();
    }

    [Fact]
    public void Sem_preco_de_hoje_nao_avisa()
    {
        // Loja fora do ar na varredura. Silêncio é a resposta certa — avisar
        // "não encontramos" todo dia seria o aviso mais inútil possível.
        Avaliador().Avaliar(Livro(hoje: null, ontem: 100m), menor30d: 100m, Agora)
            .Notify.Should().BeFalse();
    }

    [Fact]
    public void Preco_zero_nao_e_oportunidade()
    {
        // Loja sem o livro às vezes grava preço zero. Um zero no menor-30-dias
        // vira "o melhor preço do mês é R$0,00".
        Avaliador().Avaliar(Livro(hoje: 0m, ontem: 100m), menor30d: 100m, Agora)
            .Notify.Should().BeFalse();
    }
}
