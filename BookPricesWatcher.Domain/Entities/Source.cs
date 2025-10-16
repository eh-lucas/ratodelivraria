namespace Sherlock.Domain.Entities;

public class Source
{
    public string Url { get; set; }
    public SourceCategory SourceCategory { get; set; }
    public string Name { get; set; }

    public static Source LivrariaSeminario = new()
    {
        SourceCategory = SourceCategory.Cedet,
        Url = "https://livraria.seminariodefilosofia.org/",
        Name = "Livraria Seminário de Filosofia",
    };

    public static Source LivrariaDoBernardo = new()
    {
        SourceCategory = SourceCategory.Cedet,
        Url = "https://livrariadobernardo.com/",
    };

    public static Source LivrariaDeiaTiba = new()
    {
        SourceCategory = SourceCategory.Cedet,
        Url = "https://livraria.deiaetiba.com.br/",
    };

    public static Source LivrariaBeneBarbosa = new()
    {
        SourceCategory = SourceCategory.Cedet,
        Url = "https://livrariadobene.com.br/",
    };

    public static Source LivrariaAnaCampagnolo = new()
    {
        SourceCategory = SourceCategory.Cedet,
        Url = "https://livrariacampagnolo.com.br/",
    };

    public static Source LivrariaBsm = new()
    {
        SourceCategory = SourceCategory.Cedet,
        Url = "https://livrariacampagnolo.com.br/",
    };

    public static Source LivrariaNikolas = new()
    {
        SourceCategory = SourceCategory.Cedet,
        Url = "https://livrariadonikolas.com/",
    };

    public static Source LivrariaInstitutoBorborema = new()
    {
        SourceCategory = SourceCategory.Cedet,
        Url = "https://livrariaib.com/",
    };

    public static Source LivrariaPadreWander = new()
    {
        SourceCategory = SourceCategory.Cedet,
        Url = "https://livrariadopadrewander.com.br/",
    };

    public static Source LivrariaThomas = new()
    {
        SourceCategory = SourceCategory.Cedet,
        Url = "https://livrariadothomas.com.br/",
    };

    public static Source LivrariaCazarre = new()
    {
        SourceCategory = SourceCategory.Cedet,
        Url = "https://livrariacazarre.com.br/",
    };

    public static Source LivrariaGurgel = new()
    {
        SourceCategory = SourceCategory.Cedet,
        Url = "https://livraria.rodrigogurgel.com.br/",
    };

    public static Source LivrariaVictorSales = new()
    {
        SourceCategory = SourceCategory.Cedet,
        Url = "https://livrariavictorsales.com.br/",
    };

    public static Source LivrariaBrodbeck = new()
    {
        SourceCategory = SourceCategory.Cedet,
        Url = "https://livrariabrodbeck.com.br/",
    };

    public static Source LivrariaFormacaoImaginario = new()
    {
        SourceCategory = SourceCategory.Cedet,
        Url = "https://afilivraria.com.br/",
    };

    public static Source LivrariaConstantino = new()
    {
        SourceCategory = SourceCategory.Cedet,
        Url = "https://livrariadoconsta.com.br/",
    };

    public static Source LivrariaLacombe = new()
    {
        SourceCategory = SourceCategory.Cedet,
        Url = "https://livrariadolacombe.com.br/",
    };

    public static Source LivrariaContraAcademicos = new()
    {
        SourceCategory = SourceCategory.Cedet,
        Url = "https://livrariacontraosacademicos.com.br/",
    };

    public static Source LivrariaPedroAugusto = new()
    {
        SourceCategory = SourceCategory.Cedet,
        Url = "https://livrariapedroaugusto.com.br/",
    };

    public static Source LivrariaChesterton = new()
    {
        SourceCategory = SourceCategory.Cedet,
        Url = "https://livrariachestertonbrasil.com.br/",
    };

    public static Source LivrariaSabedoriaCatolica = new()
    {
        SourceCategory = SourceCategory.Cedet,
        Url = "https://livrariasabedoriacatolica.com.br/",
    };

    public static Source LivrariaKogos = new()
    {
        SourceCategory = SourceCategory.Cedet,
        Url = "https://livrariapaulokogos.com.br/",
    };

    public static Source LivrariaRasta = new()
    {
        SourceCategory = SourceCategory.Cedet,
        Url = "https://livrariadorasta.com.br/",
    };

    public static Source LivrariaEdmilson = new()
    {
        SourceCategory = SourceCategory.Cedet,
        Url = "https://livrariaedmilsoncruz.com.br/",
    };

    public static Source LivrariaCatolikids = new()
    {
        SourceCategory = SourceCategory.Cedet,
        Url = "https://livrariacatolikids.com.br/",
    };

    public static Source LivrariaZanette = new()
    {
        SourceCategory = SourceCategory.Cedet,
        Url = "https://livrariazanette.com.br/",
    };

    public static Source LivrariaPadreLucas = new()
    {
        SourceCategory = SourceCategory.Cedet,
        Url = "https://livrariadopadrelucas.com.br/",
    };

    public static List<Source> AllSources = new()
    {
        LivrariaDoBernardo,
        LivrariaDeiaTiba,
        LivrariaBeneBarbosa,
        LivrariaAnaCampagnolo,
        LivrariaBsm,
        LivrariaNikolas,
        LivrariaInstitutoBorborema,
        LivrariaPadreWander,
        LivrariaThomas,
        LivrariaCazarre,
        LivrariaGurgel,
        LivrariaVictorSales,
        LivrariaBrodbeck,
        LivrariaFormacaoImaginario,
        LivrariaConstantino,
        LivrariaLacombe,
        LivrariaContraAcademicos,
        LivrariaPedroAugusto,
        LivrariaChesterton,
        LivrariaSabedoriaCatolica,
        LivrariaKogos,
        LivrariaRasta,
        LivrariaEdmilson,
        LivrariaCatolikids,
        LivrariaZanette,
        LivrariaPadreLucas,
    };
}