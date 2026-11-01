using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sherlock.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleLoginAndFirstName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cpf",
                table: "users");

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "first_name",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "google_id",
                table: "users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

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

            // O índice único abaixo roda no startup da API (Program.cs chama
            // db.Database.Migrate()). Se o banco tiver duas linhas com o mesmo e-mail,
            // ele falha e a API não sobe — o site cai junto.
            //
            // O caso realista é diferença de caixa: o AuthController normaliza desde
            // sempre, mas nada impedia uma linha antiga com "Fulano@x.com". Normalizar
            // antes resolve esse caso sem perder nada.
            //
            // Duplicata de verdade (duas contas, mesmo e-mail já em minúsculas) continua
            // derrubando a migration, e deve mesmo: aí os dados estão errados e a decisão
            // de qual conta fica é de quem conhece o negócio, não de um script.
            migrationBuilder.Sql("UPDATE users SET email = lower(trim(email));");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_google_id",
                table: "users",
                column: "google_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_google_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "first_name",
                table: "users");

            migrationBuilder.DropColumn(
                name: "google_id",
                table: "users");

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "cpf",
                table: "users",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

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
    }
}
