using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Sherlock.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedResultTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "result_types",
                columns: new[] { "id", "description", "is_billable", "is_success", "name" },
                values: new object[,]
                {
                    { 1, "Busca realizada com sucesso", true, true, "Success" },
                    { 2, "Busca parcialmente realizada - alguns providers falharam", true, true, "PartialSuccess" },
                    { 3, "Nenhum resultado encontrado", false, false, "NoResults" },
                    { 4, "Todos os providers falharam", false, false, "AllFailed" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "result_types",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "result_types",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "result_types",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "result_types",
                keyColumn: "id",
                keyValue: 4);
        }
    }
}
