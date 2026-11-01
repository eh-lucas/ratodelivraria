using System.Globalization;
using Sherlock.Business.Configuration;
using Sherlock.Domain.Entities;

namespace Sherlock.Business.Core.Watch;

/// <summary>O que o avaliador decidiu sobre um livro, e por quê.</summary>
public record WatchDecision(bool Notify, string Reason)
{
    public static readonly WatchDecision Nao = new(false, string.Empty);
}

/// <summary>
/// Decide se um livro merece e-mail hoje. Função pura de propósito — sem banco,
/// sem relógio escondido — porque é a regra que define se o produto vale alguma
/// coisa: depois de dois ou três avisos que não eram oportunidade, a pessoa para
/// de abrir o e-mail, e aí o aviso verdadeiro também passa batido.
///
/// A regra inteira está em docs/PLANO-OBSERVADOS.md §3.3.
/// </summary>
public class WatchRuleEvaluator
{
    private readonly WatchSettings _settings;

    public WatchRuleEvaluator(WatchSettings settings) => _settings = settings;

    /// <param name="livro">O observado, com o preço de hoje já gravado em LastPrice.</param>
    /// <param name="menor30d">Menor preço dos últimos 30 dias, de `queries`.</param>
    /// <param name="agora">Momento da avaliação, em UTC.</param>
    public WatchDecision Avaliar(WatchedBook livro, decimal? menor30d, DateTime agora)
    {
        if (!livro.Active) return WatchDecision.Nao;

        var hoje = livro.LastPrice;
        if (hoje is not > 0) return WatchDecision.Nao;

        if (!PassouNoSilencio(livro, hoje.Value, agora)) return WatchDecision.Nao;

        return livro.Rule == WatchRule.TargetPrice
            ? AvaliarAlvo(livro, hoje.Value)
            : AvaliarQueda(livro, hoje.Value, menor30d);
    }

    private static WatchDecision AvaliarAlvo(WatchedBook livro, decimal hoje)
    {
        if (livro.TargetPrice is not > 0) return WatchDecision.Nao;

        return hoje <= livro.TargetPrice.Value
            ? new WatchDecision(true, $"chegou ao seu preço-alvo de {Moeda(livro.TargetPrice.Value)}")
            : WatchDecision.Nao;
    }

    /// <summary>
    /// Queda considerável, pelas duas portas do §3.3:
    ///
    /// (a) hoje &lt;= menor_30d × (1 − p) — a queda acumulada. Sete dias caindo 2%
    ///     não disparam nenhuma regra diária, mas hoje é o melhor preço do mês.
    ///
    /// (b) hoje &lt;= ontem × (1 − p) E hoje &lt;= menor_30d — a queda de ontem para
    ///     hoje, com guarda-corpo. O "e hoje &lt;= menor_30d" é o que impede o aviso
    ///     do livro que custa R$100 há um mês, subiu para R$130 ontem e está R$105
    ///     hoje: −19% no dia, e a pessoa sairia pior por causa do nosso e-mail.
    ///     Isso não é hipótese — 98,8% dos produtos dessas lojas estão marcados
    ///     como promoção, então preço de referência inflado é o estado normal.
    /// </summary>
    private static WatchDecision AvaliarQueda(WatchedBook livro, decimal hoje, decimal? menor30d)
    {
        var fator = 1m - (livro.DropPercent / 100m);

        if (menor30d is > 0 && hoje <= menor30d.Value * fator)
            return new WatchDecision(true,
                $"é o menor preço dos últimos 30 dias — {livro.DropPercent}% abaixo de {Moeda(menor30d.Value)}");

        var ontem = livro.PreviousPrice;
        if (ontem is > 0 && hoje <= ontem.Value * fator && (menor30d is null || hoje <= menor30d.Value))
            return new WatchDecision(true,
                $"caiu de {Moeda(ontem.Value)} para {Moeda(hoje)} desde ontem");

        return WatchDecision.Nao;
    }

    /// <summary>
    /// Antifadiga (§3.3.1): depois de avisar, silêncio de N dias para aquele livro
    /// — a menos que o preço caia mais uma fatia abaixo do que já foi avisado.
    /// Sem isso, um livro que fica barato uma semana vira sete e-mails iguais.
    /// </summary>
    private bool PassouNoSilencio(WatchedBook livro, decimal hoje, DateTime agora)
    {
        if (livro.LastNotifiedAt is not { } ultimoAviso) return true;

        if (agora - ultimoAviso >= TimeSpan.FromDays(_settings.QuietDays)) return true;

        if (livro.LastNotifiedPrice is not > 0) return false;

        var limite = livro.LastNotifiedPrice.Value * (1m - (_settings.RenotifyDropPercent / 100m));
        return hoje <= limite;
    }

    // O motivo do aviso vai no e-mail; o preço dentro dele segue a mesma regra do
    // WatchDigest — pt-BR fixo, não a cultura do servidor.
    private static string Moeda(decimal v)
        => string.Format(CultureInfo.GetCultureInfo("pt-BR"), "R$ {0:N2}", v);
}
