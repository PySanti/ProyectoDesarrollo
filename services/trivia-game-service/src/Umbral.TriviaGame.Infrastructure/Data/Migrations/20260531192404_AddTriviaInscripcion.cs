using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbral.TriviaGame.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTriviaInscripcion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "trivia_inscripciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartidaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FechaInscripcion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trivia_inscripciones", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_trivia_inscripciones_PartidaId_UsuarioId",
                table: "trivia_inscripciones",
                columns: new[] { "PartidaId", "UsuarioId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "trivia_inscripciones");
        }
    }
}
