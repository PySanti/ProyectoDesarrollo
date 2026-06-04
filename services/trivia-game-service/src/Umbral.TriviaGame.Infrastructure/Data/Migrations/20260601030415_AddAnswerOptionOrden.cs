using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbral.TriviaGame.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAnswerOptionOrden : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EquipoId",
                table: "trivia_inscripciones",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Orden",
                table: "QuestionOptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PreguntaAbiertaEnUtc",
                table: "partidas_trivia",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PreguntaActualId",
                table: "partidas_trivia",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "respuestas_trivia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartidaId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreguntaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    OpcionSeleccionadaIndex = table.Column<int>(type: "integer", nullable: false),
                    EsCorrecta = table.Column<bool>(type: "boolean", nullable: false),
                    PuntajeObtenido = table.Column<int>(type: "integer", nullable: false),
                    FechaRespuesta = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_respuestas_trivia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_respuestas_trivia_partidas_trivia_PartidaId",
                        column: x => x.PartidaId,
                        principalTable: "partidas_trivia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_respuestas_trivia_partida_usuario_pregunta",
                table: "respuestas_trivia",
                columns: new[] { "PartidaId", "UsuarioId", "PreguntaId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "respuestas_trivia");

            migrationBuilder.DropColumn(
                name: "EquipoId",
                table: "trivia_inscripciones");

            migrationBuilder.DropColumn(
                name: "Orden",
                table: "QuestionOptions");

            migrationBuilder.DropColumn(
                name: "PreguntaAbiertaEnUtc",
                table: "partidas_trivia");

            migrationBuilder.DropColumn(
                name: "PreguntaActualId",
                table: "partidas_trivia");
        }
    }
}
