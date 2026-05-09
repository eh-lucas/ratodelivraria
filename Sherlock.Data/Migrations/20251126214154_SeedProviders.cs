using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Sherlock.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedProviders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "providers",
                columns: new[] { "id", "base_shipping_cost", "is_active", "min_free_shipping", "name", "provider_category_enum", "search_url_template", "url" },
                values: new object[,]
                {
                    { 1, 15m, true, 200m, "Livraria Alexandre Costa", 100, "index.php?route=product/search&search={search}", "https://livrariaalexandrecosta.com.br/" },
                    { 2, 15m, true, 200m, "Livraria Ana Campagnolo", 100, "index.php?route=product/search&search={search}", "https://livrariacampagnolo.com.br/" },
                    { 3, 15m, true, 200m, "Livraria da Araceli", 100, "index.php?route=product/search&search={search}", "https://livrariadaaraceli.com.br/" },
                    { 4, 15m, true, 200m, "Livraria do Bene Barbosa", 100, "index.php?route=product/search&search={search}", "https://livrariadobene.com.br/" },
                    { 5, 15m, true, 200m, "Livraria do Bernardo", 100, "index.php?route=product/search&search={search}", "https://livrariadobernardo.com/" },
                    { 6, 15m, true, 200m, "Livraria Brodbeck", 100, "index.php?route=product/search&search={search}", "https://livrariabrodbeck.com.br/" },
                    { 7, 15m, true, 200m, "Livraria Bruna Torlay", 100, "index.php?route=product/search&search={search}", "https://livrariabrunatorlay.com.br/" },
                    { 8, 15m, true, 200m, "Livraria Catolikids", 100, "index.php?route=product/search&search={search}", "https://livrariacatolikids.com.br/" },
                    { 9, 15m, true, 200m, "Livraria Cazarre", 100, "index.php?route=product/search&search={search}", "https://livrariacazarre.com.br/" },
                    { 10, 15m, true, 200m, "Livraria Chesterton Brasil", 100, "index.php?route=product/search&search={search}", "https://livrariachestertonbrasil.com.br/" },
                    { 11, 15m, true, 200m, "Livraria do Constantino", 100, "index.php?route=product/search&search={search}", "https://livrariadoconsta.com.br/" },
                    { 12, 15m, true, 200m, "Livraria Contra os Acadêmicos", 100, "index.php?route=product/search&search={search}", "https://livrariacontraosacademicos.com.br/" },
                    { 13, 15m, true, 200m, "Livraria Daniel Lopez", 100, "index.php?route=product/search&search={search}", "https://livraria.daniellopez.com.br/" },
                    { 14, 15m, true, 200m, "Livraria Déia e Tiba", 100, "index.php?route=product/search&search={search}", "https://livraria.deiaetiba.com.br/" },
                    { 15, 15m, true, 200m, "Livraria Edmilson Cruz", 100, "index.php?route=product/search&search={search}", "https://livrariaedmilsoncruz.com.br/" },
                    { 16, 15m, true, 200m, "Livraria Formação do Imaginário", 100, "index.php?route=product/search&search={search}", "https://afilivraria.com.br/" },
                    { 17, 15m, true, 200m, "Livraria Guilherme Freire", 100, "index.php?route=product/search&search={search}", "https://livrariaguilhermefreire.com.br/" },
                    { 18, 15m, true, 200m, "Livraria Rodrigo Gurgel", 100, "index.php?route=product/search&search={search}", "https://livraria.rodrigogurgel.com.br/" },
                    { 19, 15m, true, 200m, "Livraria Instituto Borborema", 100, "index.php?route=product/search&search={search}", "https://livrariaib.com/" },
                    { 20, 15m, true, 200m, "Livraria Paulo Kogos", 100, "index.php?route=product/search&search={search}", "https://livrariapaulokogos.com.br/" },
                    { 21, 15m, true, 200m, "Livraria do Lacombe", 100, "index.php?route=product/search&search={search}", "https://livrariadolacombe.com.br/" },
                    { 22, 15m, true, 200m, "Livraria da Marcela", 100, "index.php?route=product/search&search={search}", "https://livrariadamarcela.com.br/" },
                    { 23, 15m, true, 200m, "Livraria do Nikolas", 100, "index.php?route=product/search&search={search}", "https://livrariadonikolas.com/" },
                    { 24, 15m, true, 200m, "Livraria do Padre Lucas", 100, "index.php?route=product/search&search={search}", "https://livrariadopadrelucas.com.br/" },
                    { 25, 15m, true, 200m, "Livraria Pedro Augusto", 100, "index.php?route=product/search&search={search}", "https://livrariapedroaugusto.com.br/" },
                    { 26, 15m, true, 200m, "Livraria do Rasta", 100, "index.php?route=product/search&search={search}", "https://livrariadorasta.com.br/" },
                    { 27, 15m, true, 200m, "Livraria Roberto Motta", 100, "index.php?route=product/search&search={search}", "https://livrariadorobertomotta.com.br/" },
                    { 28, 15m, true, 200m, "Livraria Sabedoria Católica", 100, "index.php?route=product/search&search={search}", "https://livrariasabedoriacatolica.com.br/" },
                    { 29, 15m, true, 200m, "Livraria do Thomas", 100, "index.php?route=product/search&search={search}", "https://livrariadothomas.com.br/" },
                    { 30, 15m, true, 200m, "Livraria Victor Sales", 100, "index.php?route=product/search&search={search}", "https://livrariavictorsales.com.br/" },
                    { 31, 15m, true, 200m, "Livraria Vista Pátria", 100, "index.php?route=product/search&search={search}", "https://livrariavistapatria.com.br/" },
                    { 32, 15m, true, 200m, "Livraria do Luis Enrique", 100, "index.php?route=product/search&search={search}", "https://www.livrariadoluisenrique.com.br/" },
                    { 33, 15m, true, 200m, "Livraria Anália", 100, "index.php?route=product/search&search={search}", "https://analigialivraria.com.br/" },
                    { 34, 15m, true, 200m, "Livraria do Silvio Navarro", 100, "index.php?route=product/search&search={search}", "https://livrariadosilvionavarro.com.br/" },
                    { 35, 15m, true, 200m, "Livraria Cris Corrêa", 100, "index.php?route=product/search&search={search}", "https://www.livrariacriscorrea.com.br/" },
                    { 36, 15m, true, 200m, "Livraria PS Zerado", 100, "index.php?route=product/search&search={search}", "https://livrariapszerado.com.br/" },
                    { 37, 15m, true, 200m, "Livraria da Vandressa", 100, "index.php?route=product/search&search={search}", "https://livrariadavandressa.com.br/" },
                    { 38, 15m, true, 200m, "Livraria Fora da Caixinha", 100, "index.php?route=product/search&search={search}", "https://www.livrariaforadacaixinha.com.br/" },
                    { 39, 15m, true, 200m, "Livraria Bruna Gutstein", 100, "index.php?route=product/search&search={search}", "https://livrariabrunagutstein.com.br/" },
                    { 40, 15m, true, 200m, "Livraria do Dantas", 100, "index.php?route=product/search&search={search}", "https://livrariadodantas.com.br/" },
                    { 41, 15m, true, 200m, "Livraria Fio Diário", 100, "index.php?route=product/search&search={search}", "https://livraria.fiodiario.com/" },
                    { 42, 15m, true, 200m, "Livraria Lara Brenner", 100, "index.php?route=product/search&search={search}", "https://livrarialarabrenner.com.br/" },
                    { 43, 15m, true, 200m, "Livraria Instituto Liberal", 100, "index.php?route=product/search&search={search}", "https://livrariainstitutoliberal.com.br/" },
                    { 44, 15m, true, 200m, "Livraria NFC", 100, "index.php?route=product/search&search={search}", "https://livrarianfc.com.br/" },
                    { 45, 15m, true, 200m, "Livraria da Rosaly", 100, "index.php?route=product/search&search={search}", "https://livrariadarosaly.com.br/" },
                    { 46, 15m, true, 200m, "Livraria Instituto Mises", 100, "index.php?route=product/search&search={search}", "https://livrariainstitutomises.com.br/" },
                    { 47, 15m, true, 200m, "Livraria do Arno", 100, "index.php?route=product/search&search={search}", "https://livrariadoarno.com.br/" },
                    { 48, 15m, true, 200m, "Livraria Revista Oeste", 100, "index.php?route=product/search&search={search}", "https://livrariarevistaoeste.com.br/" },
                    { 49, 15m, true, 200m, "Livraria do Edison Carlos", 100, "index.php?route=product/search&search={search}", "https://livrariadoedisoncarlos.com.br/" },
                    { 50, 15m, true, 200m, "Livraria do Diácono", 100, "index.php?route=product/search&search={search}", "https://livrariadodiacono.com.br/" },
                    { 51, 15m, true, 200m, "Livraria Rafael Nogueira", 100, "index.php?route=product/search&search={search}", "https://livrariarafaelnogueira.com.br/" },
                    { 52, 15m, true, 200m, "Livraria da Liberdade", 100, "index.php?route=product/search&search={search}", "https://livrariadaliberdade.com.br/" },
                    { 53, 15m, true, 200m, "Livraria Seu Filho Leitor", 100, "index.php?route=product/search&search={search}", "https://livrariaseufilholeitor.com.br/" },
                    { 54, 15m, true, 200m, "Livraria Rebelo", 100, "index.php?route=product/search&search={search}", "https://livrariarebelo.com.br/" },
                    { 55, 15m, true, 200m, "Livraria da Julia", 100, "index.php?route=product/search&search={search}", "https://livrariadajulia.com.br/" },
                    { 56, 15m, true, 200m, "Livraria da Camila", 100, "index.php?route=product/search&search={search}", "https://livrariadacamila.com.br/" },
                    { 57, 15m, true, 200m, "Livraria Católicos de Verdade", 100, "index.php?route=product/search&search={search}", "https://livrariacatolicosdeverdade.com.br/" },
                    { 58, 15m, true, 200m, "Livraria do Lorenzo", 100, "index.php?route=product/search&search={search}", "https://livrariadolorenzo.com.br/" },
                    { 59, 15m, true, 200m, "Livraria Raphael Tonon", 100, "index.php?route=product/search&search={search}", "https://livrariaraphaeltonon.com.br/" },
                    { 60, 15m, true, 200m, "Livraria BBP", 100, "index.php?route=product/search&search={search}", "https://livrariabbp.com/" },
                    { 61, 15m, true, 200m, "Livraria da Laíse", 100, "index.php?route=product/search&search={search}", "https://www.livrariadalaise.com.br/" },
                    { 62, 15m, true, 200m, "Livraria do Grimaldo", 100, "index.php?route=product/search&search={search}", "https://livrariadogrimaldo.com.br/" },
                    { 63, 15m, true, 200m, "Livraria da Patthy", 100, "index.php?route=product/search&search={search}", "https://livrariadapatthy.com.br/" },
                    { 64, 15m, true, 200m, "Livraria do Peregrino", 100, "index.php?route=product/search&search={search}", "https://livrariadoperegrino.com.br/" },
                    { 65, 15m, true, 200m, "Livraria da Família Católica", 100, "index.php?route=product/search&search={search}", "https://livrariadafamiliacatolica.com.br/" },
                    { 66, 15m, true, 200m, "Livraria Grande Família", 100, "index.php?route=product/search&search={search}", "https://livrariagrandefamilia.com.br/" },
                    { 67, 15m, true, 200m, "Livraria da Tati", 100, "index.php?route=product/search&search={search}", "https://livrariadatati.com.br/" },
                    { 68, 15m, true, 200m, "Livraria do Danilo", 100, "index.php?route=product/search&search={search}", "https://livrariadodanilo.com.br/" },
                    { 69, 15m, true, 200m, "Livraria Santa Carona", 100, "index.php?route=product/search&search={search}", "https://livrariasantacarona.com.br/" },
                    { 70, 15m, true, 200m, "Livraria da Marize", 100, "index.php?route=product/search&search={search}", "https://livrariadamarize.com.br/" },
                    { 71, 15m, true, 200m, "Livraria Pri Antunes", 100, "index.php?route=product/search&search={search}", "https://livrariapriantunes.com.br/" },
                    { 72, 15m, true, 200m, "Livraria Paula Marisa", 100, "index.php?route=product/search&search={search}", "https://livraria.paulamarisa.com.br/" },
                    { 73, 15m, true, 200m, "Livraria da Nuzio Neto", 100, "index.php?route=product/search&search={search}", "https://livrariadanuzioneto.com.br/" },
                    { 74, 15m, true, 200m, "Livraria da Pietra", 100, "index.php?route=product/search&search={search}", "https://livrariadapietra.com.br/" },
                    { 75, 15m, true, 200m, "Livraria Doutor Pacheco", 100, "index.php?route=product/search&search={search}", "https://livrariadoutorpacheco.com.br/" },
                    { 76, 15m, true, 200m, "Livraria Eduardo Bolsonaro", 100, "index.php?route=product/search&search={search}", "https://livrariaeduardobolsonaro.com.br/" },
                    { 77, 15m, true, 200m, "Livraria CEP", 100, "index.php?route=product/search&search={search}", "https://livrariacep.com.br/" },
                    { 78, 15m, true, 200m, "Livraria Zapparoli", 100, "index.php?route=product/search&search={search}", "https://livrariazapparoli.com.br/" },
                    { 79, 15m, true, 200m, "Livraria Bora Ser Santo", 100, "index.php?route=product/search&search={search}", "https://livrariaborasersanto.com.br/" },
                    { 80, 15m, true, 200m, "Livraria da Cássia", 100, "index.php?route=product/search&search={search}", "https://livrariadacassia.com.br/" },
                    { 81, 15m, true, 200m, "Livraria da Fran", 100, "index.php?route=product/search&search={search}", "https://livrariadafran.com.br/" },
                    { 82, 15m, true, 200m, "Livraria da Prof", 100, "index.php?route=product/search&search={search}", "https://livrariadaprof.com.br/" },
                    { 83, 15m, true, 200m, "Livraria do Saulo", 100, "index.php?route=product/search&search={search}", "https://livrariadosaulo.com.br/" },
                    { 84, 15m, true, 200m, "Livraria Marie Bruno", 100, "index.php?route=product/search&search={search}", "https://mariebrunobookshop.com.br/" },
                    { 85, 15m, true, 200m, "Livraria do Rodrigo", 100, "index.php?route=product/search&search={search}", "https://livrariadorodrigo.com.br/" },
                    { 86, 15m, true, 200m, "Livraria Amanda Buttchevits", 100, "index.php?route=product/search&search={search}", "https://livrariaamandabuttchevits.com.br/" },
                    { 87, 15m, true, 200m, "Livraria Te Atualizei", 100, "index.php?route=product/search&search={search}", "https://livrariateatualizei.com.br/" },
                    { 88, 15m, true, 200m, "Livraria do Alam", 100, "index.php?route=product/search&search={search}", "https://livrariadoalam.com.br/" },
                    { 89, 15m, true, 200m, "Biblioteca do Luiz", 100, "index.php?route=product/search&search={search}", "https://bibliotecadoluiz.com.br/" },
                    { 90, 15m, true, 200m, "Livraria Senso Incomum", 100, "index.php?route=product/search&search={search}", "https://livraria.sensoincomum.org/" },
                    { 91, 15m, true, 200m, "Kirion", 100, "index.php?route=product/search&search={search}", "https://www.kirion.com.br/" },
                    { 92, 15m, true, 200m, "Vide Editorial", 100, "index.php?route=product/search&search={search}", "https://videeditorial.com.br/" },
                    { 93, 15m, true, 200m, "Editora Setimo Selo", 100, "index.php?route=product/search&search={search}", "https://editorasetimoselo.com.br/" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 46);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 47);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 48);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 49);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 50);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 51);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 52);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 53);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 54);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 55);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 56);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 57);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 58);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 59);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 60);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 61);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 62);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 63);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 64);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 65);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 66);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 67);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 68);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 69);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 70);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 71);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 72);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 73);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 74);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 75);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 76);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 77);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 78);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 79);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 80);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 81);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 82);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 83);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 84);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 85);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 86);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 87);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 88);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 89);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 90);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 91);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 92);

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 93);
        }
    }
}
