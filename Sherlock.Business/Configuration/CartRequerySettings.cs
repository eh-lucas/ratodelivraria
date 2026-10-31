namespace Sherlock.Business.Configuration;

/// <summary>
/// Repescagem do carrinho: uma segunda chance para as lojas que ficaram
/// incompletas por timeout, não por falta do livro.
///
/// Existe porque o otimizador não distinguia "a loja não tem" de "a loja não
/// respondeu", e as duas acabavam no mesmo lugar — rebaixada abaixo de qualquer
/// loja completa, a qualquer preço. Em 31/10/2026 isso mostrou a Guilherme
/// Freire por R$ 205,20 num carrinho em que a Brodbeck saía por R$ 195,10; o
/// que separou as duas foi um único volume que estourou 20s.
///
/// Uma tentativa, e só. Se falhar de novo, a loja aparece marcada como
/// indeterminada e o carrinho segue com o que conseguiu — insistir mais seria
/// pôr carga no servidor que já demonstrou estar sem fôlego.
/// </summary>
public class CartRequerySettings
{
    public const string SectionName = "CartRequery";

    /// <summary>Desligado, o carrinho se comporta como antes da repescagem.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Teto de cada tentativa, em segundos.
    ///
    /// De 61.370 consultas com preço, sem cache, dos 7 dias anteriores a
    /// 31/10/2026: p50 1,54s, p95 2,16s, p99 5,22s, e 99,68% dos acertos
    /// chegam antes de 10s. É também o timeout já em produção no scraper, ou
    /// seja, número provado. Os 0,32% restantes custariam esperar até 20s.
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Válvula de segurança: teto da fase inteira, em segundos.
    ///
    /// Não é regra de negócio, é o que impede um momento ruim — dezenas de lojas
    /// caindo de uma vez — de esticar o carrinho indefinidamente. O que não
    /// couber aqui não vira terceira tentativa: a loja volta marcada e o
    /// carrinho segue.
    /// </summary>
    public int TotalBudgetSeconds { get; set; } = 20;

    // Concorrência não aparece aqui de propósito: a repescagem passa pelo mesmo
    // StoreRequestGate das buscas normais (Search:MaxGlobalParallelism, 20), que
    // já é o teto que protege os 2 IPs das livrarias. Um limite próprio aqui
    // seria um segundo número dizendo a mesma coisa.
}
