using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sherlock.Data.Migrations
{
    /// <inheritdoc />
    public partial class UseSnakeCaseNaming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tokens",
                table: "Tokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Scrapers",
                table: "Scrapers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Queries",
                table: "Queries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Providers",
                table: "Providers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Books",
                table: "Books");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ResultTypes",
                table: "ResultTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BookPrices",
                table: "BookPrices");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "users");

            migrationBuilder.RenameTable(
                name: "Tokens",
                newName: "tokens");

            migrationBuilder.RenameTable(
                name: "Scrapers",
                newName: "scrapers");

            migrationBuilder.RenameTable(
                name: "Queries",
                newName: "queries");

            migrationBuilder.RenameTable(
                name: "Providers",
                newName: "providers");

            migrationBuilder.RenameTable(
                name: "Books",
                newName: "books");

            migrationBuilder.RenameTable(
                name: "ResultTypes",
                newName: "result_types");

            migrationBuilder.RenameTable(
                name: "BookPrices",
                newName: "book_prices");

            migrationBuilder.RenameColumn(
                name: "Username",
                table: "users",
                newName: "username");

            migrationBuilder.RenameColumn(
                name: "Role",
                table: "users",
                newName: "role");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "users",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Cpf",
                table: "users",
                newName: "cpf");

            migrationBuilder.RenameColumn(
                name: "Active",
                table: "users",
                newName: "active");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "users",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "users",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "users",
                newName: "password_hash");

            migrationBuilder.RenameColumn(
                name: "DeletedAt",
                table: "users",
                newName: "deleted_at");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "users",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_Users_Username",
                table: "users",
                newName: "ix_users_username");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "tokens",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "tokens",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "tokens",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "TokenUid",
                table: "tokens",
                newName: "token_uid");

            migrationBuilder.RenameColumn(
                name: "DeletedAt",
                table: "tokens",
                newName: "deleted_at");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "tokens",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "scrapers",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Active",
                table: "scrapers",
                newName: "active");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "scrapers",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ScraperCategoryId",
                table: "scrapers",
                newName: "scraper_category_id");

            migrationBuilder.RenameColumn(
                name: "Result",
                table: "queries",
                newName: "result");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "queries",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "queries",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "SuccessfulQueries",
                table: "queries",
                newName: "successful_queries");

            migrationBuilder.RenameColumn(
                name: "StartDateTime",
                table: "queries",
                newName: "start_date_time");

            migrationBuilder.RenameColumn(
                name: "ResultTypeId",
                table: "queries",
                newName: "result_type_id");

            migrationBuilder.RenameColumn(
                name: "ProvidersQueried",
                table: "queries",
                newName: "providers_queried");

            migrationBuilder.RenameColumn(
                name: "InputParameters",
                table: "queries",
                newName: "input_parameters");

            migrationBuilder.RenameColumn(
                name: "FailedQueries",
                table: "queries",
                newName: "failed_queries");

            migrationBuilder.RenameColumn(
                name: "ExecutionTimeMs",
                table: "queries",
                newName: "execution_time_ms");

            migrationBuilder.RenameColumn(
                name: "EndDateTime",
                table: "queries",
                newName: "end_date_time");

            migrationBuilder.RenameColumn(
                name: "CostCredits",
                table: "queries",
                newName: "cost_credits");

            migrationBuilder.RenameColumn(
                name: "BookId",
                table: "queries",
                newName: "book_id");

            migrationBuilder.RenameIndex(
                name: "IX_Queries_StartDateTime",
                table: "queries",
                newName: "ix_queries_start_date_time");

            migrationBuilder.RenameColumn(
                name: "Url",
                table: "providers",
                newName: "url");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "providers",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "providers",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ProviderCategoryEnum",
                table: "providers",
                newName: "provider_category_enum");

            migrationBuilder.RenameColumn(
                name: "MinFreeShipping",
                table: "providers",
                newName: "min_free_shipping");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "providers",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "BaseShippingCost",
                table: "providers",
                newName: "base_shipping_cost");

            migrationBuilder.RenameIndex(
                name: "IX_Providers_Url",
                table: "providers",
                newName: "ix_providers_url");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "books",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "Language",
                table: "books",
                newName: "language");

            migrationBuilder.RenameColumn(
                name: "Isbn",
                table: "books",
                newName: "isbn");

            migrationBuilder.RenameColumn(
                name: "Editor",
                table: "books",
                newName: "editor");

            migrationBuilder.RenameColumn(
                name: "Author",
                table: "books",
                newName: "author");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "books",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "books",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "PageNumber",
                table: "books",
                newName: "page_number");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "books",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_Books_Title",
                table: "books",
                newName: "ix_books_title");

            migrationBuilder.RenameIndex(
                name: "IX_Books_Isbn",
                table: "books",
                newName: "ix_books_isbn");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "result_types",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "result_types",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "result_types",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "IsSuccess",
                table: "result_types",
                newName: "is_success");

            migrationBuilder.RenameColumn(
                name: "IsBillable",
                table: "result_types",
                newName: "is_billable");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "book_prices",
                newName: "price");

            migrationBuilder.RenameColumn(
                name: "Discount",
                table: "book_prices",
                newName: "discount");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "book_prices",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "QueryDateTime",
                table: "book_prices",
                newName: "query_date_time");

            migrationBuilder.RenameColumn(
                name: "ProviderId",
                table: "book_prices",
                newName: "provider_id");

            migrationBuilder.RenameColumn(
                name: "LastQueryId",
                table: "book_prices",
                newName: "last_query_id");

            migrationBuilder.RenameColumn(
                name: "BookId",
                table: "book_prices",
                newName: "book_id");

            migrationBuilder.RenameIndex(
                name: "IX_BookPrices_BookId_ProviderId_QueryDateTime",
                table: "book_prices",
                newName: "ix_book_prices_book_id_provider_id_query_date_time");

            migrationBuilder.AddPrimaryKey(
                name: "pk_users",
                table: "users",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_tokens",
                table: "tokens",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_scrapers",
                table: "scrapers",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_queries",
                table: "queries",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_providers",
                table: "providers",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_books",
                table: "books",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_result_types",
                table: "result_types",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_book_prices",
                table: "book_prices",
                column: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_users",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "pk_tokens",
                table: "tokens");

            migrationBuilder.DropPrimaryKey(
                name: "pk_scrapers",
                table: "scrapers");

            migrationBuilder.DropPrimaryKey(
                name: "pk_queries",
                table: "queries");

            migrationBuilder.DropPrimaryKey(
                name: "pk_providers",
                table: "providers");

            migrationBuilder.DropPrimaryKey(
                name: "pk_books",
                table: "books");

            migrationBuilder.DropPrimaryKey(
                name: "pk_result_types",
                table: "result_types");

            migrationBuilder.DropPrimaryKey(
                name: "pk_book_prices",
                table: "book_prices");

            migrationBuilder.RenameTable(
                name: "users",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "tokens",
                newName: "Tokens");

            migrationBuilder.RenameTable(
                name: "scrapers",
                newName: "Scrapers");

            migrationBuilder.RenameTable(
                name: "queries",
                newName: "Queries");

            migrationBuilder.RenameTable(
                name: "providers",
                newName: "Providers");

            migrationBuilder.RenameTable(
                name: "books",
                newName: "Books");

            migrationBuilder.RenameTable(
                name: "result_types",
                newName: "ResultTypes");

            migrationBuilder.RenameTable(
                name: "book_prices",
                newName: "BookPrices");

            migrationBuilder.RenameColumn(
                name: "username",
                table: "Users",
                newName: "Username");

            migrationBuilder.RenameColumn(
                name: "role",
                table: "Users",
                newName: "Role");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "Users",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "cpf",
                table: "Users",
                newName: "Cpf");

            migrationBuilder.RenameColumn(
                name: "active",
                table: "Users",
                newName: "Active");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Users",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "Users",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "password_hash",
                table: "Users",
                newName: "PasswordHash");

            migrationBuilder.RenameColumn(
                name: "deleted_at",
                table: "Users",
                newName: "DeletedAt");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Users",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_users_username",
                table: "Users",
                newName: "IX_Users_Username");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Tokens",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "Tokens",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "Tokens",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "token_uid",
                table: "Tokens",
                newName: "TokenUid");

            migrationBuilder.RenameColumn(
                name: "deleted_at",
                table: "Tokens",
                newName: "DeletedAt");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Tokens",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Scrapers",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "active",
                table: "Scrapers",
                newName: "Active");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Scrapers",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "scraper_category_id",
                table: "Scrapers",
                newName: "ScraperCategoryId");

            migrationBuilder.RenameColumn(
                name: "result",
                table: "Queries",
                newName: "Result");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Queries",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "Queries",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "successful_queries",
                table: "Queries",
                newName: "SuccessfulQueries");

            migrationBuilder.RenameColumn(
                name: "start_date_time",
                table: "Queries",
                newName: "StartDateTime");

            migrationBuilder.RenameColumn(
                name: "result_type_id",
                table: "Queries",
                newName: "ResultTypeId");

            migrationBuilder.RenameColumn(
                name: "providers_queried",
                table: "Queries",
                newName: "ProvidersQueried");

            migrationBuilder.RenameColumn(
                name: "input_parameters",
                table: "Queries",
                newName: "InputParameters");

            migrationBuilder.RenameColumn(
                name: "failed_queries",
                table: "Queries",
                newName: "FailedQueries");

            migrationBuilder.RenameColumn(
                name: "execution_time_ms",
                table: "Queries",
                newName: "ExecutionTimeMs");

            migrationBuilder.RenameColumn(
                name: "end_date_time",
                table: "Queries",
                newName: "EndDateTime");

            migrationBuilder.RenameColumn(
                name: "cost_credits",
                table: "Queries",
                newName: "CostCredits");

            migrationBuilder.RenameColumn(
                name: "book_id",
                table: "Queries",
                newName: "BookId");

            migrationBuilder.RenameIndex(
                name: "ix_queries_start_date_time",
                table: "Queries",
                newName: "IX_Queries_StartDateTime");

            migrationBuilder.RenameColumn(
                name: "url",
                table: "Providers",
                newName: "Url");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Providers",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Providers",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "provider_category_enum",
                table: "Providers",
                newName: "ProviderCategoryEnum");

            migrationBuilder.RenameColumn(
                name: "min_free_shipping",
                table: "Providers",
                newName: "MinFreeShipping");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "Providers",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "base_shipping_cost",
                table: "Providers",
                newName: "BaseShippingCost");

            migrationBuilder.RenameIndex(
                name: "ix_providers_url",
                table: "Providers",
                newName: "IX_Providers_Url");

            migrationBuilder.RenameColumn(
                name: "title",
                table: "Books",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "language",
                table: "Books",
                newName: "Language");

            migrationBuilder.RenameColumn(
                name: "isbn",
                table: "Books",
                newName: "Isbn");

            migrationBuilder.RenameColumn(
                name: "editor",
                table: "Books",
                newName: "Editor");

            migrationBuilder.RenameColumn(
                name: "author",
                table: "Books",
                newName: "Author");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Books",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "Books",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "page_number",
                table: "Books",
                newName: "PageNumber");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Books",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_books_title",
                table: "Books",
                newName: "IX_Books_Title");

            migrationBuilder.RenameIndex(
                name: "ix_books_isbn",
                table: "Books",
                newName: "IX_Books_Isbn");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "ResultTypes",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "ResultTypes",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "ResultTypes",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "is_success",
                table: "ResultTypes",
                newName: "IsSuccess");

            migrationBuilder.RenameColumn(
                name: "is_billable",
                table: "ResultTypes",
                newName: "IsBillable");

            migrationBuilder.RenameColumn(
                name: "price",
                table: "BookPrices",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "discount",
                table: "BookPrices",
                newName: "Discount");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "BookPrices",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "query_date_time",
                table: "BookPrices",
                newName: "QueryDateTime");

            migrationBuilder.RenameColumn(
                name: "provider_id",
                table: "BookPrices",
                newName: "ProviderId");

            migrationBuilder.RenameColumn(
                name: "last_query_id",
                table: "BookPrices",
                newName: "LastQueryId");

            migrationBuilder.RenameColumn(
                name: "book_id",
                table: "BookPrices",
                newName: "BookId");

            migrationBuilder.RenameIndex(
                name: "ix_book_prices_book_id_provider_id_query_date_time",
                table: "BookPrices",
                newName: "IX_BookPrices_BookId_ProviderId_QueryDateTime");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tokens",
                table: "Tokens",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Scrapers",
                table: "Scrapers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Queries",
                table: "Queries",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Providers",
                table: "Providers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Books",
                table: "Books",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ResultTypes",
                table: "ResultTypes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BookPrices",
                table: "BookPrices",
                column: "Id");
        }
    }
}
