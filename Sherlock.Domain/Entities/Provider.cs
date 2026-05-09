namespace Sherlock.Domain.Entities;

public class Provider
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public ProviderCategoryEnum ProviderCategoryEnum { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Template da URL de busca. Use {search} como placeholder para o termo de busca.
    /// Default: "index.php?route=product/search&search={search}" (OpenCart - usado por 89/93 providers)
    /// WooCommerce: "?s={search}&post_type=product"
    /// </summary>
    public string SearchUrlTemplate { get; set; } = "index.php?route=product/search&search={search}";

    // ========== PROVIDERS EXISTENTES ==========

    public static Provider LivrariaAlexandreCosta = new()
    {
        Id = 1,
        Name = "Livraria Alexandre Costa",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariaalexandrecosta.com.br/",
    };

    public static Provider LivrariaAnaCampagnolo = new()
    {
        Id = 2,
        Name = "Livraria Ana Campagnolo",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariacampagnolo.com.br/",
    };

    public static Provider LivrariaAraceli = new()
    {
        Id = 3,
        Name = "Livraria da Araceli",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadaaraceli.com.br/",
    };

    public static Provider LivrariaBeneBarbosa = new()
    {
        Id = 4,
        Name = "Livraria do Bene Barbosa",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadobene.com.br/",
    };

    public static Provider LivrariaBernardo = new()
    {
        Id = 5,
        Name = "Livraria do Bernardo",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadobernardo.com/",
    };

    public static Provider LivrariaBrodbeck = new()
    {
        Id = 6,
        Name = "Livraria Brodbeck",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariabrodbeck.com.br/",
    };

    public static Provider LivrariaBrunaTorlay = new()
    {
        Id = 7,
        Name = "Livraria Bruna Torlay",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariabrunatorlay.com.br/",
    };

    public static Provider LivrariaCatolikids = new()
    {
        Id = 8,
        Name = "Livraria Catolikids",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariacatolikids.com.br/",
    };

    public static Provider LivrariaCazarre = new()
    {
        Id = 9,
        Name = "Livraria Cazarre",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariacazarre.com.br/",
    };

    public static Provider LivrariaChesterton = new()
    {
        Id = 10,
        Name = "Livraria Chesterton Brasil",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariachestertonbrasil.com.br/",
    };

    public static Provider LivrariaConstantino = new()
    {
        Id = 11,
        Name = "Livraria do Constantino",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadoconsta.com.br/",
    };

    public static Provider LivrariaContraAcademicos = new()
    {
        Id = 12,
        Name = "Livraria Contra os Acadêmicos",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariacontraosacademicos.com.br/",
    };

    public static Provider LivrariaDanielLopez = new()
    {
        Id = 13,
        Name = "Livraria Daniel Lopez",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livraria.daniellopez.com.br/",
    };

    public static Provider LivrariaDeiaTiba = new()
    {
        Id = 14,
        Name = "Livraria Déia e Tiba",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livraria.deiaetiba.com.br/",
    };

    public static Provider LivrariaEdmilson = new()
    {
        Id = 15,
        Name = "Livraria Edmilson Cruz",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariaedmilsoncruz.com.br/",
    };

    public static Provider LivrariaFormacaoImaginario = new()
    {
        Id = 16,
        Name = "Livraria Formação do Imaginário",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://afilivraria.com.br/",
    };

    public static Provider LivrariaGuilhermeFreire = new()
    {
        Id = 17,
        Name = "Livraria Guilherme Freire",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariaguilhermefreire.com.br/",
    };

    public static Provider LivrariaGurgel = new()
    {
        Id = 18,
        Name = "Livraria Rodrigo Gurgel",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livraria.rodrigogurgel.com.br/",
    };

    public static Provider LivrariaInstitutoBorborema = new()
    {
        Id = 19,
        Name = "Livraria Instituto Borborema",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariaib.com/",
    };

    public static Provider LivrariaKogos = new()
    {
        Id = 20,
        Name = "Livraria Paulo Kogos",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariapaulokogos.com.br/",
    };

    public static Provider LivrariaLacombe = new()
    {
        Id = 21,
        Name = "Livraria do Lacombe",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadolacombe.com.br/",
    };

    public static Provider LivrariaMarcela = new()
    {
        Id = 22,
        Name = "Livraria da Marcela",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadamarcela.com.br/",
    };

    public static Provider LivrariaNikolas = new()
    {
        Id = 23,
        Name = "Livraria do Nikolas",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadonikolas.com/",
    };

    public static Provider LivrariaPadreLucas = new()
    {
        Id = 24,
        Name = "Livraria do Padre Lucas",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadopadrelucas.com.br/",
    };

    public static Provider LivrariaPedroAugusto = new()
    {
        Id = 25,
        Name = "Livraria Pedro Augusto",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariapedroaugusto.com.br/",
    };

    public static Provider LivrariaRasta = new()
    {
        Id = 26,
        Name = "Livraria do Rasta",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadorasta.com.br/",
    };

    public static Provider LivrariaRobertoMotta = new()
    {
        Id = 27,
        Name = "Livraria Roberto Motta",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadorobertomotta.com.br/",
    };

    public static Provider LivrariaSabedoriaCatolica = new()
    {
        Id = 28,
        Name = "Livraria Sabedoria Católica",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariasabedoriacatolica.com.br/",
    };

    public static Provider LivrariaThomas = new()
    {
        Id = 29,
        Name = "Livraria do Thomas",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadothomas.com.br/",
    };

    public static Provider LivrariaVictorSales = new()
    {
        Id = 30,
        Name = "Livraria Victor Sales",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariavictorsales.com.br/",
    };

    public static Provider LivrariaVistaPatria = new()
    {
        Id = 31,
        Name = "Livraria Vista Pátria",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariavistapatria.com.br/",
    };

    // ========== NOVOS PROVIDERS ==========

    public static Provider LivrariaLuisEnrique = new()
    {
        Id = 32,
        Name = "Livraria do Luis Enrique",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://www.livrariadoluisenrique.com.br/",
    };

    public static Provider LivrariaAnalia = new()
    {
        Id = 33,
        Name = "Livraria Anália",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://analigialivraria.com.br/",
    };

    public static Provider LivrariaSilvioNavarro = new()
    {
        Id = 34,
        Name = "Livraria do Silvio Navarro",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadosilvionavarro.com.br/",
    };

    public static Provider LivrariaCrisCorrea = new()
    {
        Id = 35,
        Name = "Livraria Cris Corrêa",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://www.livrariacriscorrea.com.br/",
    };

    public static Provider LivrariaPsZerado = new()
    {
        Id = 36,
        Name = "Livraria PS Zerado",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariapszerado.com.br/",
    };

    public static Provider LivrariaVandressa = new()
    {
        Id = 37,
        Name = "Livraria da Vandressa",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadavandressa.com.br/",
    };

    public static Provider LivrariaForaDaCaixinha = new()
    {
        Id = 38,
        Name = "Livraria Fora da Caixinha",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://www.livrariaforadacaixinha.com.br/",
    };

    public static Provider LivrariaBrunaGutstein = new()
    {
        Id = 39,
        Name = "Livraria Bruna Gutstein",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariabrunagutstein.com.br/",
    };

    public static Provider LivrariaDantas = new()
    {
        Id = 40,
        Name = "Livraria do Dantas",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadodantas.com.br/",
    };

    public static Provider LivrariaFioDiario = new()
    {
        Id = 41,
        Name = "Livraria Fio Diário",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livraria.fiodiario.com/",
    };

    public static Provider LivrariaLaraBrenner = new()
    {
        Id = 42,
        Name = "Livraria Lara Brenner",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrarialarabrenner.com.br/",
    };

    public static Provider LivrariaInstitutoLiberal = new()
    {
        Id = 43,
        Name = "Livraria Instituto Liberal",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariainstitutoliberal.com.br/",
    };

    public static Provider LivrariaNfc = new()
    {
        Id = 44,
        Name = "Livraria NFC",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrarianfc.com.br/",
    };

    public static Provider LivrariaRosaly = new()
    {
        Id = 45,
        Name = "Livraria da Rosaly",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadarosaly.com.br/",
    };

    public static Provider LivrariaInstitutoMises = new()
    {
        Id = 46,
        Name = "Livraria Instituto Mises",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariainstitutomises.com.br/",
    };

    public static Provider LivrariaArno = new()
    {
        Id = 47,
        Name = "Livraria do Arno",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadoarno.com.br/",
    };

    public static Provider LivrariaRevistaOeste = new()
    {
        Id = 48,
        Name = "Livraria Revista Oeste",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariarevistaoeste.com.br/",
    };

    public static Provider LivrariaEdisonCarlos = new()
    {
        Id = 49,
        Name = "Livraria do Edison Carlos",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadoedisoncarlos.com.br/",
    };

    public static Provider LivrariaDiacono = new()
    {
        Id = 50,
        Name = "Livraria do Diácono",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadodiacono.com.br/",
    };

    public static Provider LivrariaRafaelNogueira = new()
    {
        Id = 51,
        Name = "Livraria Rafael Nogueira",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariarafaelnogueira.com.br/",
    };

    public static Provider LivrariaDaLiberdade = new()
    {
        Id = 52,
        Name = "Livraria da Liberdade",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadaliberdade.com.br/",
    };

    public static Provider LivrariaSeuFilhoLeitor = new()
    {
        Id = 53,
        Name = "Livraria Seu Filho Leitor",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariaseufilholeitor.com.br/",
    };

    public static Provider LivrariaRebelo = new()
    {
        Id = 54,
        Name = "Livraria Rebelo",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariarebelo.com.br/",
    };

    public static Provider LivrariaJulia = new()
    {
        Id = 55,
        Name = "Livraria da Julia",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadajulia.com.br/",
    };

    public static Provider LivrariaCamila = new()
    {
        Id = 56,
        Name = "Livraria da Camila",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadacamila.com.br/",
    };

    public static Provider LivrariaCatolicosDeVerdade = new()
    {
        Id = 57,
        Name = "Livraria Católicos de Verdade",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariacatolicosdeverdade.com.br/",
    };

    public static Provider LivrariaLorenzo = new()
    {
        Id = 58,
        Name = "Livraria do Lorenzo",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadolorenzo.com.br/",
    };

    public static Provider LivrariaRaphaelTonon = new()
    {
        Id = 59,
        Name = "Livraria Raphael Tonon",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariaraphaeltonon.com.br/",
    };

    public static Provider LivrariaBbp = new()
    {
        Id = 60,
        Name = "Livraria BBP",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariabbp.com/",
    };

    public static Provider LivrariaLaise = new()
    {
        Id = 61,
        Name = "Livraria da Laíse",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://www.livrariadalaise.com.br/",
    };

    public static Provider LivrariaGrimaldo = new()
    {
        Id = 62,
        Name = "Livraria do Grimaldo",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadogrimaldo.com.br/",
    };

    public static Provider LivrariaPatthy = new()
    {
        Id = 63,
        Name = "Livraria da Patthy",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadapatthy.com.br/",
    };

    public static Provider LivrariaPeregrino = new()
    {
        Id = 64,
        Name = "Livraria do Peregrino",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadoperegrino.com.br/",
    };

    public static Provider LivrariaFamiliaCatolica = new()
    {
        Id = 65,
        Name = "Livraria da Família Católica",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadafamiliacatolica.com.br/",
    };

    public static Provider LivrariaGrandeFamilia = new()
    {
        Id = 66,
        Name = "Livraria Grande Família",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariagrandefamilia.com.br/",
    };

    public static Provider LivrariaTati = new()
    {
        Id = 67,
        Name = "Livraria da Tati",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadatati.com.br/",
    };

    public static Provider LivrariaDanilo = new()
    {
        Id = 68,
        Name = "Livraria do Danilo",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadodanilo.com.br/",
    };

    public static Provider LivrariaSantaCarona = new()
    {
        Id = 69,
        Name = "Livraria Santa Carona",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariasantacarona.com.br/",
    };

    public static Provider LivrariaMarize = new()
    {
        Id = 70,
        Name = "Livraria da Marize",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadamarize.com.br/",
    };

    public static Provider LivrariaPriAntunes = new()
    {
        Id = 71,
        Name = "Livraria Pri Antunes",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariapriantunes.com.br/",
    };

    public static Provider LivrariaPaulaMarisa = new()
    {
        Id = 72,
        Name = "Livraria Paula Marisa",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livraria.paulamarisa.com.br/",
    };

    public static Provider LivrariaNuzioNeto = new()
    {
        Id = 73,
        Name = "Livraria da Nuzio Neto",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadanuzioneto.com.br/",
    };

    public static Provider LivrariaPietra = new()
    {
        Id = 74,
        Name = "Livraria da Pietra",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadapietra.com.br/",
    };

    public static Provider LivrariaDoutorPacheco = new()
    {
        Id = 75,
        Name = "Livraria Doutor Pacheco",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadoutorpacheco.com.br/",
    };

    public static Provider LivrariaEduardoBolsonaro = new()
    {
        Id = 76,
        Name = "Livraria Eduardo Bolsonaro",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariaeduardobolsonaro.com.br/",
    };

    public static Provider LivrariaCep = new()
    {
        Id = 77,
        Name = "Livraria CEP",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariacep.com.br/",
    };

    public static Provider LivrariaZapparoli = new()
    {
        Id = 78,
        Name = "Livraria Zapparoli",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariazapparoli.com.br/",
    };

    public static Provider LivrariaBoraSerSanto = new()
    {
        Id = 79,
        Name = "Livraria Bora Ser Santo",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariaborasersanto.com.br/",
    };

    public static Provider LivrariaCassia = new()
    {
        Id = 80,
        Name = "Livraria da Cássia",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadacassia.com.br/",
    };

    public static Provider LivrariaFran = new()
    {
        Id = 81,
        Name = "Livraria da Fran",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadafran.com.br/",
    };

    public static Provider LivrariaProf = new()
    {
        Id = 82,
        Name = "Livraria da Prof",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadaprof.com.br/",
    };

    public static Provider LivrariaSaulo = new()
    {
        Id = 83,
        Name = "Livraria do Saulo",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadosaulo.com.br/",
    };

    public static Provider LivrariaMarieBruno = new()
    {
        Id = 84,
        Name = "Livraria Marie Bruno",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://mariebrunobookshop.com.br/",
    };

    public static Provider LivrariaRodrigo = new()
    {
        Id = 85,
        Name = "Livraria do Rodrigo",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadorodrigo.com.br/",
    };

    public static Provider LivrariaAmandaButtchevits = new()
    {
        Id = 86,
        Name = "Livraria Amanda Buttchevits",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariaamandabuttchevits.com.br/",
    };

    public static Provider LivrariaTeAtualizei = new()
    {
        Id = 87,
        Name = "Livraria Te Atualizei",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariateatualizei.com.br/",
    };

    public static Provider LivrariaAlam = new()
    {
        Id = 88,
        Name = "Livraria do Alam",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livrariadoalam.com.br/",
    };

    public static Provider BibliotecaDoLuiz = new()
    {
        Id = 89,
        Name = "Biblioteca do Luiz",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://bibliotecadoluiz.com.br/",
    };

    public static Provider LivrariaSensoIncomum = new()
    {
        Id = 90,
        Name = "Livraria Senso Incomum",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://livraria.sensoincomum.org/",
    };

    public static Provider Kirion = new()
    {
        Id = 91,
        Name = "Kirion",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://www.kirion.com.br/",
    };

    public static Provider VideEditorial = new()
    {
        Id = 92,
        Name = "Vide Editorial",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://videeditorial.com.br/",
    };

    public static Provider EditoraSetimoSelo = new()
    {
        Id = 93,
        Name = "Editora Setimo Selo",
        ProviderCategoryEnum = ProviderCategoryEnum.Cedet,
        Url = "https://editorasetimoselo.com.br/",
    };

    public static List<Provider> AllSources = new()
    {
        // Existentes
        LivrariaAlexandreCosta,
        LivrariaAnaCampagnolo,
        LivrariaAraceli,
        LivrariaBeneBarbosa,
        LivrariaBernardo,
        LivrariaBrodbeck,
        LivrariaBrunaTorlay,
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
        LivrariaThomas,
        LivrariaVictorSales,
        // LivrariaVistaPatria, // Inativo: NXDOMAIN (livrariavistapatria.com.br não resolve)
        // Novos
        LivrariaLuisEnrique,
        LivrariaAnalia,
        // LivrariaSilvioNavarro, // Inativo: DNS SERVFAIL (livrariadosilvionavarro.com.br)
        LivrariaCrisCorrea,
        LivrariaPsZerado,
        // LivrariaVandressa, // Inativo: NXDOMAIN (livrariadavandressa.com.br)
        LivrariaForaDaCaixinha,
        LivrariaBrunaGutstein,
        LivrariaDantas,
        LivrariaFioDiario,
        LivrariaLaraBrenner,
        LivrariaInstitutoLiberal,
        LivrariaNfc,
        // LivrariaRosaly, // Inativo: NXDOMAIN (livrariadarosaly.com.br)
        LivrariaInstitutoMises,
        LivrariaArno,
        LivrariaRevistaOeste,
        LivrariaEdisonCarlos,
        LivrariaDiacono,
        LivrariaRafaelNogueira,
        LivrariaDaLiberdade,
        LivrariaSeuFilhoLeitor,
        // LivrariaRebelo, // Inativo: NXDOMAIN (livrariarebelo.com.br)
        LivrariaJulia,
        LivrariaCamila,
        LivrariaCatolicosDeVerdade,
        LivrariaLorenzo,
        LivrariaRaphaelTonon,
        LivrariaBbp,
        LivrariaLaise,
        LivrariaGrimaldo,
        // LivrariaPatthy, // Inativo: DNS SERVFAIL (livrariadapatthy.com.br)
        LivrariaPeregrino,
        LivrariaFamiliaCatolica,
        LivrariaGrandeFamilia,
        LivrariaTati,
        LivrariaDanilo,
        LivrariaSantaCarona,
        LivrariaMarize,
        LivrariaPriAntunes,
        LivrariaPaulaMarisa,
        // LivrariaNuzioNeto, // Inativo: NXDOMAIN (livrariadanuzioneto.com.br)
        LivrariaPietra,
        LivrariaDoutorPacheco,
        LivrariaEduardoBolsonaro,
        LivrariaCep,
        // LivrariaZapparoli, // Inativo: NXDOMAIN (livrariazapparoli.com.br)
        LivrariaBoraSerSanto,
        LivrariaCassia,
        LivrariaFran,
        LivrariaProf,
        LivrariaSaulo,
        LivrariaMarieBruno,
        LivrariaRodrigo,
        LivrariaAmandaButtchevits,
        LivrariaTeAtualizei,
        LivrariaAlam,
        BibliotecaDoLuiz,
        LivrariaSensoIncomum,
        Kirion,
        VideEditorial,
        EditoraSetimoSelo,
    };
}
