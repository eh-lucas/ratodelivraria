using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Sherlock.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBooksAndProviders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SearchTypes",
                table: "SearchTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Searches",
                table: "Searches");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Results",
                table: "Results");

            migrationBuilder.RenameTable(
                name: "SearchTypes",
                newName: "Scrapers");

            migrationBuilder.RenameTable(
                name: "Searches",
                newName: "Queries");

            migrationBuilder.RenameTable(
                name: "Results",
                newName: "ResultTypes");

            migrationBuilder.RenameColumn(
                name: "TokenId",
                table: "Queries",
                newName: "SuccessfulQueries");

            migrationBuilder.RenameColumn(
                name: "ProviderTypeId",
                table: "Queries",
                newName: "ProvidersQueried");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            // Conversão de text para jsonb com USING
            migrationBuilder.Sql(
                @"ALTER TABLE ""Queries"" ALTER COLUMN ""Result"" TYPE jsonb USING ""Result""::jsonb;");

            migrationBuilder.Sql(
                @"ALTER TABLE ""Queries"" ALTER COLUMN ""InputParameters"" TYPE jsonb USING ""InputParameters""::jsonb;");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDateTime",
                table: "Queries",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<int>(
                name: "BookId",
                table: "Queries",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "CostCredits",
                table: "Queries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "ExecutionTimeMs",
                table: "Queries",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "FailedQueries",
                table: "Queries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Queries",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ResultTypes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Scrapers",
                table: "Scrapers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Queries",
                table: "Queries",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ResultTypes",
                table: "ResultTypes",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "BookPrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LastQueryId = table.Column<int>(type: "integer", nullable: false),
                    BookId = table.Column<int>(type: "integer", nullable: false),
                    ProviderId = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Discount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    QueryDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookPrices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Books",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Author = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Isbn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Editor = table.Column<string>(type: "text", nullable: false),
                    PageNumber = table.Column<int>(type: "integer", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Books", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Providers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ProviderCategoryEnum = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MinFreeShipping = table.Column<decimal>(type: "numeric", nullable: true),
                    BaseShippingCost = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Providers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Queries_StartDateTime",
                table: "Queries",
                column: "StartDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_BookPrices_BookId_ProviderId_QueryDateTime",
                table: "BookPrices",
                columns: new[] { "BookId", "ProviderId", "QueryDateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Books_Isbn",
                table: "Books",
                column: "Isbn");

            migrationBuilder.CreateIndex(
                name: "IX_Books_Title",
                table: "Books",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_Url",
                table: "Providers",
                column: "Url",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookPrices");

            migrationBuilder.DropTable(
                name: "Books");

            migrationBuilder.DropTable(
                name: "Providers");

            migrationBuilder.DropIndex(
                name: "IX_Users_Username",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Scrapers",
                table: "Scrapers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ResultTypes",
                table: "ResultTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Queries",
                table: "Queries");

            migrationBuilder.DropIndex(
                name: "IX_Queries_StartDateTime",
                table: "Queries");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CostCredits",
                table: "Queries");

            migrationBuilder.DropColumn(
                name: "ExecutionTimeMs",
                table: "Queries");

            migrationBuilder.DropColumn(
                name: "FailedQueries",
                table: "Queries");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Queries");

            migrationBuilder.RenameTable(
                name: "Scrapers",
                newName: "SearchTypes");

            migrationBuilder.RenameTable(
                name: "ResultTypes",
                newName: "Results");

            migrationBuilder.RenameTable(
                name: "Queries",
                newName: "Searches");

            migrationBuilder.RenameColumn(
                name: "SuccessfulQueries",
                table: "Searches",
                newName: "TokenId");

            migrationBuilder.RenameColumn(
                name: "ProvidersQueried",
                table: "Searches",
                newName: "ProviderTypeId");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Results",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Result",
                table: "Searches",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "InputParameters",
                table: "Searches",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDateTime",
                table: "Searches",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BookId",
                table: "Searches",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SearchTypes",
                table: "SearchTypes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Results",
                table: "Results",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Searches",
                table: "Searches",
                column: "Id");
        }
    }
}
