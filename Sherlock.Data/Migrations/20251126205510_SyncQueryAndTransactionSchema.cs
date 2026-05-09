using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sherlock.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncQueryAndTransactionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_transactions_books_book_id",
                table: "transactions");

            migrationBuilder.DropForeignKey(
                name: "fk_transactions_result_types_result_type_id",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "ix_transactions_book_id",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "book_id",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "from_cache",
                table: "transactions");

            migrationBuilder.AlterColumn<float>(
                name: "discount",
                table: "queries",
                type: "real",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "from_cache",
                table: "queries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "search_isbn",
                table: "queries",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "search_url_template",
                table: "providers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_queries_search_isbn_provider_id_queried_at",
                table: "queries",
                columns: new[] { "search_isbn", "provider_id", "queried_at" },
                filter: "search_isbn IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_transactions_transaction_results_result_type_id",
                table: "transactions",
                column: "result_type_id",
                principalTable: "result_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_transactions_transaction_results_result_type_id",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "ix_queries_search_isbn_provider_id_queried_at",
                table: "queries");

            migrationBuilder.DropColumn(
                name: "from_cache",
                table: "queries");

            migrationBuilder.DropColumn(
                name: "search_isbn",
                table: "queries");

            migrationBuilder.DropColumn(
                name: "search_url_template",
                table: "providers");

            migrationBuilder.AddColumn<int>(
                name: "book_id",
                table: "transactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "from_cache",
                table: "transactions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "discount",
                table: "queries",
                type: "integer",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "real",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_transactions_book_id",
                table: "transactions",
                column: "book_id");

            migrationBuilder.AddForeignKey(
                name: "fk_transactions_books_book_id",
                table: "transactions",
                column: "book_id",
                principalTable: "books",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_transactions_result_types_result_type_id",
                table: "transactions",
                column: "result_type_id",
                principalTable: "result_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
