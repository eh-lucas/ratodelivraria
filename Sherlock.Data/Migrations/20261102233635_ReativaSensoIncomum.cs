using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sherlock.Data.Migrations
{
    /// <summary>
    /// Devolve a Livraria Senso Incomum (Id 90) ao cadastro.
    ///
    /// Escrita à mão: o scaffold gera InsertData, mas em bancos que já existiam a
    /// linha 90 nunca saiu de `providers` — ficou inativa porque `queries` a
    /// referencia com ON DELETE RESTRICT. Um INSERT puro quebraria nesses bancos;
    /// o upsert cobre os dois casos (banco antigo e banco novo). Também tira os
    /// UpdateData de credit_packages que o scaffold traz (seed com DateTime.UtcNow).
    /// </summary>
    public partial class ReativaSensoIncomum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO providers (id, is_active, name, provider_category_enum, search_url_template, url)
                VALUES (90, TRUE, 'Livraria Senso Incomum', 100,
                        'index.php?route=product/search&search={search}',
                        'https://livraria.sensoincomum.org/')
                ON CONFLICT (id) DO UPDATE SET
                    is_active = EXCLUDED.is_active,
                    name = EXCLUDED.name,
                    provider_category_enum = EXCLUDED.provider_category_enum,
                    search_url_template = EXCLUDED.search_url_template,
                    url = EXCLUDED.url;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Volta ao estado anterior (inativa) em vez de apagar: a linha pode ter
            // consultas apontando para ela.
            migrationBuilder.Sql("UPDATE providers SET is_active = FALSE WHERE id = 90;");
        }
    }
}
