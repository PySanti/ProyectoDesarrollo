using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbral.Puntuaciones.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SP4aProyecciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "eventos_procesados",
                columns: table => new
                {
                    eventid = table.Column<Guid>(type: "uuid", nullable: false),
                    eventtype = table.Column<string>(type: "text", nullable: false),
                    occurredat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    procesadoat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eventos_procesados", x => x.eventid);
                });

            migrationBuilder.CreateTable(
                name: "juegos_proyectados",
                columns: table => new
                {
                    juegoid = table.Column<Guid>(type: "uuid", nullable: false),
                    partidaid = table.Column<Guid>(type: "uuid", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    tipojuego = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_juegos_proyectados", x => x.juegoid);
                });

            migrationBuilder.CreateTable(
                name: "marcadores",
                columns: table => new
                {
                    juegoid = table.Column<Guid>(type: "uuid", nullable: false),
                    competidorid = table.Column<Guid>(type: "uuid", nullable: false),
                    partidaid = table.Column<Guid>(type: "uuid", nullable: false),
                    tipocompetidor = table.Column<int>(type: "integer", nullable: false),
                    puntosacumulados = table.Column<int>(type: "integer", nullable: false),
                    tiempoacumuladoms = table.Column<long>(type: "bigint", nullable: false),
                    unidadesganadas = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marcadores", x => new { x.juegoid, x.competidorid });
                });

            migrationBuilder.CreateTable(
                name: "partidas_proyectadas",
                columns: table => new
                {
                    partidaid = table.Column<Guid>(type: "uuid", nullable: false),
                    sesionpartidaid = table.Column<Guid>(type: "uuid", nullable: false),
                    modalidad = table.Column<int>(type: "integer", nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false),
                    fechainicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fechafin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partidas_proyectadas", x => x.partidaid);
                });

            migrationBuilder.CreateIndex(
                name: "ix_juegos_proyectados_partidaid",
                table: "juegos_proyectados",
                column: "partidaid");

            migrationBuilder.CreateIndex(
                name: "ix_marcadores_juegoid",
                table: "marcadores",
                column: "juegoid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "eventos_procesados");

            migrationBuilder.DropTable(
                name: "juegos_proyectados");

            migrationBuilder.DropTable(
                name: "marcadores");

            migrationBuilder.DropTable(
                name: "partidas_proyectadas");
        }
    }
}
