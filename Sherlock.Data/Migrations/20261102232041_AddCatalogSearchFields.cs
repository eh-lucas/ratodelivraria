using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sherlock.Data.Migrations
{
    /// <summary>
    /// Campos para a caixa de busca mostrar capa e autor e para buscar pelo autor.
    ///
    /// Escrita à mão por cima do scaffold: o scaffold trazia UpdateData de
    /// credit_packages (o seed usa DateTime.UtcNow e "muda" a cada build) e não
    /// sabe criar índice trigram.
    /// </summary>
    public partial class AddCatalogSearchFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "authors_normalized",
                table: "catalog_items",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "thumb_url",
                table: "catalog_items",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            // pg_trgm e unaccent são extensões "trusted" desde o PG 13: o dono do
            // banco consegue criá-las sem superusuário.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS unaccent;");

            // Preenche o que já foi coletado. Aproxima o CatalogCrawler.Normalize
            // (minúsculo, sem acento); a próxima coleta regrava com o valor exato.
            migrationBuilder.Sql(
                "UPDATE catalog_items SET authors_normalized = lower(unaccent(authors)) " +
                "WHERE authors IS NOT NULL;");

            // O índice do título já existia em produção, criado à mão fora de
            // migration — daí o IF NOT EXISTS. Um banco novo agora também o ganha.
            // É ele que faz o LIKE '%termo%' da sugestão não varrer a tabela.
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ix_catalog_items_name_trgm " +
                "ON catalog_items USING gin (name_normalized gin_trgm_ops);");

            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ix_catalog_items_authors_trgm " +
                "ON catalog_items USING gin (authors_normalized gin_trgm_ops);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ix_catalog_items_name_trgm fica: ele existia antes desta migration
            // em produção, e desfazê-la não deve deixar a sugestão sem índice.
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_catalog_items_authors_trgm;");

            migrationBuilder.DropColumn(
                name: "authors_normalized",
                table: "catalog_items");

            migrationBuilder.DropColumn(
                name: "thumb_url",
                table: "catalog_items");
        }
    }
}
