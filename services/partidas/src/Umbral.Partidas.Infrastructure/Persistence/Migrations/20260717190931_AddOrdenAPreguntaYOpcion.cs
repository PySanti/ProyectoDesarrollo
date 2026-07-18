using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbral.Partidas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrdenAPreguntaYOpcion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "orden",
                table: "preguntas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "orden",
                table: "opciones",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "orden",
                table: "preguntas");

            migrationBuilder.DropColumn(
                name: "orden",
                table: "opciones");
        }
    }
}
