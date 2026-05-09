using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Sherlock.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCreditsColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "base_shipping_cost",
                table: "providers");

            migrationBuilder.DropColumn(
                name: "min_free_shipping",
                table: "providers");

            migrationBuilder.AddColumn<int>(
                name: "available_credits",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AddColumn<int>(
                name: "total_credits_used",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "credit_packages",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    credits = table.Column<int>(type: "integer", nullable: false),
                    price_in_cents = table.Column<int>(type: "integer", nullable: false),
                    bonus_credits = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_popular = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_credit_packages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "credit_transactions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<int>(type: "integer", nullable: false),
                    balance_after = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    credit_package_id = table.Column<int>(type: "integer", nullable: true),
                    search_transaction_id = table.Column<int>(type: "integer", nullable: true),
                    external_payment_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_credit_transactions", x => x.id);
                    table.ForeignKey(
                        name: "fk_credit_transactions_credit_packages_credit_package_id",
                        column: x => x.credit_package_id,
                        principalTable: "credit_packages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_credit_transactions_transactions_search_transaction_id",
                        column: x => x.search_transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_credit_transactions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "credit_packages",
                columns: new[] { "id", "bonus_credits", "created_at", "credits", "description", "display_order", "is_active", "is_popular", "name", "price_in_cents", "updated_at" },
                values: new object[,]
                {
                    { 1, 0, new DateTime(2025, 11, 28, 11, 48, 17, 27, DateTimeKind.Utc).AddTicks(1461), 50, "Ideal para testar o serviço", 1, true, false, "Starter", 490, null },
                    { 2, 10, new DateTime(2025, 11, 28, 11, 48, 17, 27, DateTimeKind.Utc).AddTicks(2418), 100, "Para uso casual", 2, true, false, "Básico", 890, null },
                    { 3, 50, new DateTime(2025, 11, 28, 11, 48, 17, 27, DateTimeKind.Utc).AddTicks(2420), 300, "Melhor custo-benefício", 3, true, true, "Popular", 1990, null },
                    { 4, 200, new DateTime(2025, 11, 28, 11, 48, 17, 27, DateTimeKind.Utc).AddTicks(2422), 1000, "Para usuários frequentes", 4, true, false, "Premium", 4990, null }
                });

            migrationBuilder.CreateIndex(
                name: "ix_credit_packages_display_order",
                table: "credit_packages",
                column: "display_order");

            migrationBuilder.CreateIndex(
                name: "ix_credit_packages_is_active",
                table: "credit_packages",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_credit_transactions_created_at",
                table: "credit_transactions",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_credit_transactions_credit_package_id",
                table: "credit_transactions",
                column: "credit_package_id");

            migrationBuilder.CreateIndex(
                name: "ix_credit_transactions_search_transaction_id",
                table: "credit_transactions",
                column: "search_transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_credit_transactions_type",
                table: "credit_transactions",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ix_credit_transactions_user_id",
                table: "credit_transactions",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "credit_transactions");

            migrationBuilder.DropTable(
                name: "credit_packages");

            migrationBuilder.DropColumn(
                name: "available_credits",
                table: "users");

            migrationBuilder.DropColumn(
                name: "total_credits_used",
                table: "users");

            migrationBuilder.AddColumn<decimal>(
                name: "base_shipping_cost",
                table: "providers",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "min_free_shipping",
                table: "providers",
                type: "numeric",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 1,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 2,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 3,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 4,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 5,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 6,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 7,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 8,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 9,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 10,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 11,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 12,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 13,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 14,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 15,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 16,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 17,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 18,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 19,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 20,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 21,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 22,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 23,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 24,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 25,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 26,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 27,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 28,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 29,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 30,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 31,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 32,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 33,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 34,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 35,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 36,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 37,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 38,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 39,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 40,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 41,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 42,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 43,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 44,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 45,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 46,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 47,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 48,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 49,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 50,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 51,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 52,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 53,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 54,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 55,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 56,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 57,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 58,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 59,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 60,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 61,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 62,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 63,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 64,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 65,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 66,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 67,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 68,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 69,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 70,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 71,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 72,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 73,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 74,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 75,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 76,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 77,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 78,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 79,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 80,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 81,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 82,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 83,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 84,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 85,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 86,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 87,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 88,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 89,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 90,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 91,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 92,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });

            migrationBuilder.UpdateData(
                table: "providers",
                keyColumn: "id",
                keyValue: 93,
                columns: new[] { "base_shipping_cost", "min_free_shipping" },
                values: new object[] { 15m, 200m });
        }
    }
}
