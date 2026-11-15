using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Campanhas.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class IndicesListas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_doacoes_campanha_id",
                table: "doacoes");

            migrationBuilder.DropIndex(
                name: "ix_doacoes_doador_id",
                table: "doacoes");

            migrationBuilder.CreateIndex(
                name: "ix_doacoes_campanha_criada",
                table: "doacoes",
                columns: new[] { "campanha_id", "criada_em" });

            migrationBuilder.CreateIndex(
                name: "ix_doacoes_criada_em",
                table: "doacoes",
                column: "criada_em");

            migrationBuilder.CreateIndex(
                name: "ix_doacoes_doador_criada",
                table: "doacoes",
                columns: new[] { "doador_id", "criada_em" });

            migrationBuilder.CreateIndex(
                name: "ix_doacoes_status_processada",
                table: "doacoes",
                columns: new[] { "status", "processada_em" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_doacoes_campanha_criada",
                table: "doacoes");

            migrationBuilder.DropIndex(
                name: "ix_doacoes_criada_em",
                table: "doacoes");

            migrationBuilder.DropIndex(
                name: "ix_doacoes_doador_criada",
                table: "doacoes");

            migrationBuilder.DropIndex(
                name: "ix_doacoes_status_processada",
                table: "doacoes");

            migrationBuilder.CreateIndex(
                name: "ix_doacoes_campanha_id",
                table: "doacoes",
                column: "campanha_id");

            migrationBuilder.CreateIndex(
                name: "ix_doacoes_doador_id",
                table: "doacoes",
                column: "doador_id");
        }
    }
}
