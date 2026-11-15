using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Campanhas.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DetalhesCampanha : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "capa",
                table: "campanhas",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "categoria",
                table: "campanhas",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "criancas_atendidas",
                table: "campanhas",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "capa",
                table: "campanhas");

            migrationBuilder.DropColumn(
                name: "categoria",
                table: "campanhas");

            migrationBuilder.DropColumn(
                name: "criancas_atendidas",
                table: "campanhas");
        }
    }
}
