using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Sherlock.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWatchedBooks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "watched_books",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    isbn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    author = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    image_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    rule = table.Column<int>(type: "integer", nullable: false),
                    target_price = table.Column<decimal>(type: "numeric", nullable: true),
                    drop_percent = table.Column<int>(type: "integer", nullable: false, defaultValue: 15),
                    reference_price = table.Column<decimal>(type: "numeric", nullable: true),
                    reference_price_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_price = table.Column<decimal>(type: "numeric", nullable: true),
                    previous_price = table.Column<decimal>(type: "numeric", nullable: true),
                    last_checked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_provider_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_product_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    last_notified_price = table.Column<decimal>(type: "numeric", nullable: true),
                    last_notified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_watched_books", x => x.id);
                    table.ForeignKey(
                        name: "fk_watched_books_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "credit_packages",
                keyColumn: "id",
                keyValue: 1,
                column: "created_at",
                value: new DateTime(2026, 10, 31, 21, 40, 11, 653, DateTimeKind.Utc).AddTicks(1518));

            migrationBuilder.UpdateData(
                table: "credit_packages",
                keyColumn: "id",
                keyValue: 2,
                column: "created_at",
                value: new DateTime(2026, 10, 31, 21, 40, 11, 653, DateTimeKind.Utc).AddTicks(5104));

            migrationBuilder.UpdateData(
                table: "credit_packages",
                keyColumn: "id",
                keyValue: 3,
                column: "created_at",
                value: new DateTime(2026, 10, 31, 21, 40, 11, 653, DateTimeKind.Utc).AddTicks(5111));

            migrationBuilder.UpdateData(
                table: "credit_packages",
                keyColumn: "id",
                keyValue: 4,
                column: "created_at",
                value: new DateTime(2026, 10, 31, 21, 40, 11, 653, DateTimeKind.Utc).AddTicks(5116));

            migrationBuilder.CreateIndex(
                name: "ix_watched_books_isbn",
                table: "watched_books",
                column: "isbn");

            migrationBuilder.CreateIndex(
                name: "ix_watched_books_user_id_active",
                table: "watched_books",
                columns: new[] { "user_id", "active" });

            migrationBuilder.CreateIndex(
                name: "ix_watched_books_user_id_isbn",
                table: "watched_books",
                columns: new[] { "user_id", "isbn" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "watched_books");

            migrationBuilder.UpdateData(
                table: "credit_packages",
                keyColumn: "id",
                keyValue: 1,
                column: "created_at",
                value: new DateTime(2026, 10, 31, 20, 37, 28, 450, DateTimeKind.Utc).AddTicks(2682));

            migrationBuilder.UpdateData(
                table: "credit_packages",
                keyColumn: "id",
                keyValue: 2,
                column: "created_at",
                value: new DateTime(2026, 10, 31, 20, 37, 28, 450, DateTimeKind.Utc).AddTicks(6154));

            migrationBuilder.UpdateData(
                table: "credit_packages",
                keyColumn: "id",
                keyValue: 3,
                column: "created_at",
                value: new DateTime(2026, 10, 31, 20, 37, 28, 450, DateTimeKind.Utc).AddTicks(6161));

            migrationBuilder.UpdateData(
                table: "credit_packages",
                keyColumn: "id",
                keyValue: 4,
                column: "created_at",
                value: new DateTime(2026, 10, 31, 20, 37, 28, 450, DateTimeKind.Utc).AddTicks(6166));
        }
    }
}
