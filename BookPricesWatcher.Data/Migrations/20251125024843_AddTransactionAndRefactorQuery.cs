using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Sherlock.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionAndRefactorQuery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_queries_start_date_time",
                table: "queries");

            migrationBuilder.DropColumn(
                name: "cost_credits",
                table: "queries");

            migrationBuilder.DropColumn(
                name: "end_date_time",
                table: "queries");

            migrationBuilder.DropColumn(
                name: "failed_queries",
                table: "queries");

            migrationBuilder.DropColumn(
                name: "input_parameters",
                table: "queries");

            migrationBuilder.DropColumn(
                name: "providers_queried",
                table: "queries");

            migrationBuilder.DropColumn(
                name: "result",
                table: "queries");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "queries",
                newName: "discount");

            migrationBuilder.RenameColumn(
                name: "successful_queries",
                table: "queries",
                newName: "transaction_id");

            migrationBuilder.RenameColumn(
                name: "start_date_time",
                table: "queries",
                newName: "queried_at");

            migrationBuilder.RenameColumn(
                name: "result_type_id",
                table: "queries",
                newName: "provider_id");

            migrationBuilder.RenameColumn(
                name: "execution_time_ms",
                table: "queries",
                newName: "response_time_ms");

            migrationBuilder.AddColumn<string>(
                name: "author",
                table: "queries",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "error_message",
                table: "queries",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "price",
                table: "queries",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "product_url",
                table: "queries",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "success",
                table: "queries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "title",
                table: "queries",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "transactions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    book_id = table.Column<int>(type: "integer", nullable: true),
                    result_type_id = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    execution_time_ms = table.Column<long>(type: "bigint", nullable: false),
                    total_providers_queried = table.Column<int>(type: "integer", nullable: false),
                    successful_queries = table.Column<int>(type: "integer", nullable: false),
                    failed_queries = table.Column<int>(type: "integer", nullable: false),
                    cost_credits = table.Column<int>(type: "integer", nullable: false),
                    input_parameters = table.Column<string>(type: "jsonb", nullable: false),
                    from_cache = table.Column<bool>(type: "boolean", nullable: false),
                    errors = table.Column<string>(type: "jsonb", nullable: true),
                    best_query_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transactions", x => x.id);
                    table.ForeignKey(
                        name: "fk_transactions_books_book_id",
                        column: x => x.book_id,
                        principalTable: "books",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_transactions_queries_best_query_id",
                        column: x => x.best_query_id,
                        principalTable: "queries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_transactions_result_types_result_type_id",
                        column: x => x.result_type_id,
                        principalTable: "result_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_transactions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_queries_book_id",
                table: "queries",
                column: "book_id");

            migrationBuilder.CreateIndex(
                name: "ix_queries_provider_id",
                table: "queries",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "ix_queries_transaction_id",
                table: "queries",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_queries_transaction_id_provider_id",
                table: "queries",
                columns: new[] { "transaction_id", "provider_id" });

            migrationBuilder.CreateIndex(
                name: "ix_transactions_best_query_id",
                table: "transactions",
                column: "best_query_id");

            migrationBuilder.CreateIndex(
                name: "ix_transactions_book_id",
                table: "transactions",
                column: "book_id");

            migrationBuilder.CreateIndex(
                name: "ix_transactions_result_type_id",
                table: "transactions",
                column: "result_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_transactions_started_at",
                table: "transactions",
                column: "started_at");

            migrationBuilder.CreateIndex(
                name: "ix_transactions_user_id",
                table: "transactions",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_queries_books_book_id",
                table: "queries",
                column: "book_id",
                principalTable: "books",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_queries_providers_provider_id",
                table: "queries",
                column: "provider_id",
                principalTable: "providers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_queries_transactions_transaction_id",
                table: "queries",
                column: "transaction_id",
                principalTable: "transactions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_queries_books_book_id",
                table: "queries");

            migrationBuilder.DropForeignKey(
                name: "fk_queries_providers_provider_id",
                table: "queries");

            migrationBuilder.DropForeignKey(
                name: "fk_queries_transactions_transaction_id",
                table: "queries");

            migrationBuilder.DropTable(
                name: "transactions");

            migrationBuilder.DropIndex(
                name: "ix_queries_book_id",
                table: "queries");

            migrationBuilder.DropIndex(
                name: "ix_queries_provider_id",
                table: "queries");

            migrationBuilder.DropIndex(
                name: "ix_queries_transaction_id",
                table: "queries");

            migrationBuilder.DropIndex(
                name: "ix_queries_transaction_id_provider_id",
                table: "queries");

            migrationBuilder.DropColumn(
                name: "author",
                table: "queries");

            migrationBuilder.DropColumn(
                name: "error_message",
                table: "queries");

            migrationBuilder.DropColumn(
                name: "price",
                table: "queries");

            migrationBuilder.DropColumn(
                name: "product_url",
                table: "queries");

            migrationBuilder.DropColumn(
                name: "success",
                table: "queries");

            migrationBuilder.DropColumn(
                name: "title",
                table: "queries");

            migrationBuilder.RenameColumn(
                name: "transaction_id",
                table: "queries",
                newName: "successful_queries");

            migrationBuilder.RenameColumn(
                name: "response_time_ms",
                table: "queries",
                newName: "execution_time_ms");

            migrationBuilder.RenameColumn(
                name: "queried_at",
                table: "queries",
                newName: "start_date_time");

            migrationBuilder.RenameColumn(
                name: "provider_id",
                table: "queries",
                newName: "result_type_id");

            migrationBuilder.RenameColumn(
                name: "discount",
                table: "queries",
                newName: "user_id");

            migrationBuilder.AddColumn<int>(
                name: "cost_credits",
                table: "queries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "end_date_time",
                table: "queries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "failed_queries",
                table: "queries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "input_parameters",
                table: "queries",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "providers_queried",
                table: "queries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "result",
                table: "queries",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_queries_start_date_time",
                table: "queries",
                column: "start_date_time");
        }
    }
}
