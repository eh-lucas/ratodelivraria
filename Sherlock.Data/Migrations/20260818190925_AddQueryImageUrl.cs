using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sherlock.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQueryImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "queries",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "credit_packages",
                keyColumn: "id",
                keyValue: 1,
                column: "created_at",
                value: new DateTime(2026, 8, 18, 19, 9, 24, 302, DateTimeKind.Utc).AddTicks(1735));

            migrationBuilder.UpdateData(
                table: "credit_packages",
                keyColumn: "id",
                keyValue: 2,
                column: "created_at",
                value: new DateTime(2026, 8, 18, 19, 9, 24, 302, DateTimeKind.Utc).AddTicks(3431));

            migrationBuilder.UpdateData(
                table: "credit_packages",
                keyColumn: "id",
                keyValue: 3,
                column: "created_at",
                value: new DateTime(2026, 8, 18, 19, 9, 24, 302, DateTimeKind.Utc).AddTicks(3450));

            migrationBuilder.UpdateData(
                table: "credit_packages",
                keyColumn: "id",
                keyValue: 4,
                column: "created_at",
                value: new DateTime(2026, 8, 18, 19, 9, 24, 302, DateTimeKind.Utc).AddTicks(3465));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image_url",
                table: "queries");

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
        }
    }
}
