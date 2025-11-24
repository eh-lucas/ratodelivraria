namespace Sherlock.Domain.Entities;

public class Provider
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public ProviderCategoryEnum ProviderCategoryEnum { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public decimal? MinFreeShipping { get; set; }
    public decimal? BaseShippingCost { get; set; }


    public static Provider LivrariaAlexandreCosta = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariaalexandrecosta.com.br/",
    };

    public static Provider LivrariaAnaCampagnolo = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariacampagnolo.com.br/",
    };

    public static Provider LivrariaAraceli = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadaaraceli.com.br/",
    };

    public static Provider LivrariaBeneBarbosa = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadobene.com.br/",
    };

    public static Provider LivrariaBernardo = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadobernardo.com/",
    };

    public static Provider LivrariaBrodbeck = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariabrodbeck.com.br/",
    };

    public static Provider LivrariaBrunaTorlay = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariabrunatorlay.com.br/",
    };

    public static Provider LivrariaBsm = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariacampagnolo.com.br/",
    };

    public static Provider LivrariaCatolikids = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariacatolikids.com.br/",
    };

    public static Provider LivrariaCazarre = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariacazarre.com.br/",
    };

    public static Provider LivrariaChesterton = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariachestertonbrasil.com.br/",
    };

    public static Provider LivrariaConstantino = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadoconsta.com.br/",
    };

    public static Provider LivrariaContraAcademicos = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariacontraosacademicos.com.br/",
    };

    public static Provider LivrariaDanielLopez = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livraria.daniellopez.com.br/",
    };

    public static Provider LivrariaDeiaTiba = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livraria.deiaetiba.com.br/",
    };

    public static Provider LivrariaEdmilson = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariaedmilsoncruz.com.br/",
    };

    public static Provider LivrariaFormacaoImaginario = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://afilivraria.com.br/",
    };

    public static Provider LivrariaGuilhermeFreire = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariaguilhermefreire.com.br/",
    };

    public static Provider LivrariaGurgel = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livraria.rodrigogurgel.com.br/",
    };

    public static Provider LivrariaInstitutoBorborema = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariaib.com/",
    };

    public static Provider LivrariaKogos = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariapaulokogos.com.br/",
    };

    public static Provider LivrariaLacombe = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadolacombe.com.br/",
    };

    public static Provider LivrariaMarcela = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariamarcela.com.br/",
    };

    public static Provider LivrariaNikolas = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadonikolas.com/",
    };

    public static Provider LivrariaPadreLucas = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadopadrelucas.com.br/",
    };

    public static Provider LivrariaPedroAugusto = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariapedroaugusto.com.br/",
    };

    public static Provider LivrariaRasta = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadorasta.com.br/",
    };

    public static Provider LivrariaRobertoMotta = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadorobertomotta.com.br/",
    };

    public static Provider LivrariaSabedoriaCatolica = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariasabedoriacatolica.com.br/",
    };

    public static Provider LivrariaSeminario = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livraria.seminariodefilosofia.org/",
        Name = "Livraria Seminário de Filosofia",
    };

    public static Provider LivrariaThomas = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadothomas.com.br/",
    };

    public static Provider LivrariaVictorSales = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariavictorsales.com.br/",
    };

    public static Provider LivrariaVistaPatria = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariavistapatria.com.br/",
    };

    public static Provider LivrariaZanette = new()
    {
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariazanette.com.br/",
    };

    public static List<Provider> AllSources = new()
    {
        LivrariaAlexandreCosta,
        LivrariaAnaCampagnolo,
        LivrariaAraceli,
        LivrariaBeneBarbosa,
        LivrariaBernardo,
        LivrariaBrodbeck,
        LivrariaBrunaTorlay,
        LivrariaBsm,
        LivrariaCatolikids,
        LivrariaCazarre,
        LivrariaChesterton,
        LivrariaConstantino,
        LivrariaContraAcademicos,
        LivrariaDanielLopez,
        LivrariaDeiaTiba,
        LivrariaEdmilson,
        LivrariaFormacaoImaginario,
        LivrariaGuilhermeFreire,
        LivrariaGurgel,
        LivrariaInstitutoBorborema,
        LivrariaKogos,
        LivrariaLacombe,
        LivrariaMarcela,
        LivrariaNikolas,
        LivrariaPadreLucas,
        LivrariaPedroAugusto,
        LivrariaRasta,
        LivrariaRobertoMotta,
        LivrariaSabedoriaCatolica,
        LivrariaSeminario,
        LivrariaThomas,
        LivrariaVictorSales,
        LivrariaVistaPatria,
        LivrariaZanette,
    };
}