using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbral.TriviaGame.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPartidaTrivia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "partidas_trivia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Modalidad = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ModoInicio = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FormularioAsociadoId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByOperatorId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TiempoInicio = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MinimoParticipantes = table.Column<int>(type: "integer", nullable: false),
                    MaximoJugadores = table.Column<int>(type: "integer", nullable: true),
                    MaximoEquipos = table.Column<int>(type: "integer", nullable: true),
                    MinimoJugadoresPorEquipo = table.Column<int>(type: "integer", nullable: true),
                    MaximoJugadoresPorEquipo = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partidas_trivia", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "partidas_trivia");
        }
    }
}
