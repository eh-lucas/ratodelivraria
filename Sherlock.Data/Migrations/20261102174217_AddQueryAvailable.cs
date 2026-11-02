using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sherlock.Data.Migrations
{
    /// <summary>
    /// Sinal de estoque por consulta. Default <c>true</c> e não o <c>false</c> que o
    /// scaffold propõe: as consultas antigas não sabiam ler estoque, e marcá-las como
    /// esgotadas apagaria o histórico de preço e o menor-do-mês dos observados.
    /// </summary>
    public partial class AddQueryAvailable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "available",
                table: "queries",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "available",
                table: "queries");
        }
    }
}
