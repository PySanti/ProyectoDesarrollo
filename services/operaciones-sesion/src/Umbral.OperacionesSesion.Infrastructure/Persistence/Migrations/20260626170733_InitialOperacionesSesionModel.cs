using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbral.OperacionesSesion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialOperacionesSesionModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sesiones_partida",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    partidaid = table.Column<Guid>(type: "uuid", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    modalidad = table.Column<int>(type: "integer", nullable: false),
                    modoinicio = table.Column<int>(type: "integer", nullable: false),
                    tiempoinicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    minimos = table.Column<int>(type: "integer", nullable: false),
                    maximos = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sesiones_partida", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inscripciones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    participanteid = table.Column<Guid>(type: "uuid", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false),
                    fechainscripcion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sesionid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inscripciones", x => x.id);
                    table.ForeignKey(
                        name: "FK_inscripciones_sesiones_partida_sesionid",
                        column: x => x.sesionid,
                        principalTable: "sesiones_partida",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sesion_juegos",
                columns: table => new
                {
                    juegoid = table.Column<Guid>(type: "uuid", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    tipojuego = table.Column<int>(type: "integer", nullable: false),
                    sesionid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sesion_juegos", x => x.juegoid);
                    table.ForeignKey(
                        name: "FK_sesion_juegos_sesiones_partida_sesionid",
                        column: x => x.sesionid,
                        principalTable: "sesiones_partida",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inscripciones_sesionid",
                table: "inscripciones",
                column: "sesionid");

            migrationBuilder.CreateIndex(
                name: "IX_sesion_juegos_sesionid",
                table: "sesion_juegos",
                column: "sesionid");

            migrationBuilder.CreateIndex(
                name: "ix_sesiones_partidaid",
                table: "sesiones_partida",
                column: "partidaid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inscripciones");

            migrationBuilder.DropTable(
                name: "sesion_juegos");

            migrationBuilder.DropTable(
                name: "sesiones_partida");
        }
    }
}
