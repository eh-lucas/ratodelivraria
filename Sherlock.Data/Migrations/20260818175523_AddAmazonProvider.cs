using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sherlock.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAmazonProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "credit_packages",
                keyColumn: "id",
                keyValue: 1,
                column: "created_at",
                value: new DateTime(2026, 8, 18, 17, 55, 22, 616, DateTimeKind.Utc).AddTicks(1607));

            migrationBuilder.UpdateData(
                table: "credit_packages",
                keyColumn: "id",
                keyValue: 2,
                column: "created_at",
                value: new DateTime(2026, 8, 18, 17, 55, 22, 616, DateTimeKind.Utc).AddTicks(3173));

            migrationBuilder.UpdateData(
                table: "credit_packages",
                keyColumn: "id",
                keyValue: 3,
                column: "created_at",
                value: new DateTime(2026, 8, 18, 17, 55, 22, 616, DateTimeKind.Utc).AddTicks(3187));

            migrationBuilder.UpdateData(
                table: "credit_packages",
                keyColumn: "id",
                keyValue: 4,
                column: "created_at",
                value: new DateTime(2026, 8, 18, 17, 55, 22, 616, DateTimeKind.Utc).AddTicks(3201));

            migrationBuilder.InsertData(
                table: "providers",
                columns: new[] { "id", "is_active", "name", "provider_category_enum", "search_url_template", "url" },
                values: new object[] { 94, true, "Amazon", 200, "s?k={search}&i=stripbooks", "https://www.amazon.com.br/" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValue: 94);

            migrationBuilder.UpdateData(
                table: "credit_packages",
                keyColumn: "id",
                keyValue: 1,
                column: "created_at",
                value: new DateTime(2026, 8, 18, 11, 7, 55, 187, DateTimeKind.Utc).AddTicks(2450));

            migrationBuilder.UpdateData(
                table: "credit_packages",
                keyColumn: "id",
                keyValue: 2,
                column: "created_at",
                value: new DateTime(2026, 8, 18, 11, 7, 55, 187, DateTimeKind.Utc).AddTicks(3972));

            migrationBuilder.UpdateData(
                table: "credit_packages",
                keyColumn: "id",
                keyValue: 3,
                column: "created_at",
                value: new DateTime(2026, 8, 18, 11, 7, 55, 187, DateTimeKind.Utc).AddTicks(3986));

            migrationBuilder.UpdateData(
                table: "credit_packages",
                keyColumn: "id",
                keyValue: 4,
                column: "created_at",
                value: new DateTime(2026, 8, 18, 11, 7, 55, 187, DateTimeKind.Utc).AddTicks(4000));
        }
    }
}
