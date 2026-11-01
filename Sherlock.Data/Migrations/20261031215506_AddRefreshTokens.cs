using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sherlock.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A tabela `tokens` existe desde o InitialCreate e nunca foi escrita —
            // nenhum código do sistema a usava. As colunas antigas (token_uid,
            // deleted_at) não têm equivalente no modelo novo, e qualquer linha
            // remanescente ganharia token_hash = '' e colidiria no índice único
            // que vem logo abaixo. Esvaziar é o único caminho que não deixa a
            // migração falhar no meio de um startup de produção.
            migrationBuilder.Sql("DELETE FROM tokens;");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "tokens");

            migrationBuilder.DropColumn(
                name: "token_uid",
                table: "tokens");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "tokens",
                newName: "expires_at");

            migrationBuilder.AddColumn<string>(
                name: "replaced_by_hash",
                table: "tokens",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "revoked_at",
                table: "tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "token_hash",
                table: "tokens",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "credit_packages",
                keyColumn: "id",
                keyValue: 1,
                column: "created_at",
                value: new DateTime(2026, 10, 31, 21, 55, 4, 588, DateTimeKind.Utc).AddTicks(7177));

            migrationBuilder.UpdateData(
                table: "credit_packages",
                keyColumn: "id",
                keyValue: 2,
                column: "created_at",
                value: new DateTime(2026, 10, 31, 21, 55, 4, 589, DateTimeKind.Utc).AddTicks(816));

            migrationBuilder.UpdateData(
                table: "credit_packages",
                keyColumn: "id",
                keyValue: 3,
                column: "created_at",
                value: new DateTime(2026, 10, 31, 21, 55, 4, 589, DateTimeKind.Utc).AddTicks(826));

            migrationBuilder.UpdateData(
                table: "credit_packages",
                keyColumn: "id",
                keyValue: 4,
                column: "created_at",
                value: new DateTime(2026, 10, 31, 21, 55, 4, 589, DateTimeKind.Utc).AddTicks(830));

            migrationBuilder.CreateIndex(
                name: "ix_tokens_token_hash",
                table: "tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tokens_user_id_revoked_at",
                table: "tokens",
                columns: new[] { "user_id", "revoked_at" });

            migrationBuilder.AddForeignKey(
                name: "fk_tokens_users_user_id",
                table: "tokens",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tokens_users_user_id",
                table: "tokens");

            migrationBuilder.DropIndex(
                name: "ix_tokens_token_hash",
                table: "tokens");

            migrationBuilder.DropIndex(
                name: "ix_tokens_user_id_revoked_at",
                table: "tokens");

            migrationBuilder.DropColumn(
                name: "replaced_by_hash",
                table: "tokens");

            migrationBuilder.DropColumn(
                name: "revoked_at",
                table: "tokens");

            migrationBuilder.DropColumn(
                name: "token_hash",
                table: "tokens");

            migrationBuilder.RenameColumn(
                name: "expires_at",
                table: "tokens",
                newName: "updated_at");

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "tokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "token_uid",
                table: "tokens",
                type: "text",
                nullable: false,
                defaultValue: "");

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
        }
    }
}
