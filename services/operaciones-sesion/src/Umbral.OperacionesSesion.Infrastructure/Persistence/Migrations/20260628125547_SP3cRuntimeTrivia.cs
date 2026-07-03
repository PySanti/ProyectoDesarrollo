using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbral.OperacionesSesion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SP3cRuntimeTrivia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "preguntas_snapshot",
                columns: table => new
                {
                    preguntaid = table.Column<Guid>(type: "uuid", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    texto = table.Column<string>(type: "text", nullable: false),
                    puntajeasignado = table.Column<int>(type: "integer", nullable: false),
                    tiempolimitesegundos = table.Column<int>(type: "integer", nullable: false),
                    estadopregunta = table.Column<int>(type: "integer", nullable: false),
                    fechaactivacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fechacierre = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivocierre = table.Column<int>(type: "integer", nullable: true),
                    ganadorparticipanteid = table.Column<Guid>(type: "uuid", nullable: true),
                    juegoid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_preguntas_snapshot", x => x.preguntaid);
                    table.ForeignKey(
                        name: "FK_preguntas_snapshot_sesion_juegos_juegoid",
                        column: x => x.juegoid,
                        principalTable: "sesion_juegos",
                        principalColumn: "juegoid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "opciones_snapshot",
                columns: table => new
                {
                    opcionid = table.Column<Guid>(type: "uuid", nullable: false),
                    texto = table.Column<string>(type: "text", nullable: false),
                    escorrecta = table.Column<bool>(type: "boolean", nullable: false),
                    preguntaid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_opciones_snapshot", x => x.opcionid);
                    table.ForeignKey(
                        name: "FK_opciones_snapshot_preguntas_snapshot_preguntaid",
                        column: x => x.preguntaid,
                        principalTable: "preguntas_snapshot",
                        principalColumn: "preguntaid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "respuestas_trivia",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    participanteid = table.Column<Guid>(type: "uuid", nullable: false),
                    opcionid = table.Column<Guid>(type: "uuid", nullable: false),
                    escorrecta = table.Column<bool>(type: "boolean", nullable: false),
                    instante = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    preguntaid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_respuestas_trivia", x => x.id);
                    table.ForeignKey(
                        name: "FK_respuestas_trivia_preguntas_snapshot_preguntaid",
                        column: x => x.preguntaid,
                        principalTable: "preguntas_snapshot",
                        principalColumn: "preguntaid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_opciones_snapshot_preguntaid",
                table: "opciones_snapshot",
                column: "preguntaid");

            migrationBuilder.CreateIndex(
                name: "IX_preguntas_snapshot_juegoid",
                table: "preguntas_snapshot",
                column: "juegoid");

            migrationBuilder.CreateIndex(
                name: "IX_respuestas_trivia_preguntaid",
                table: "respuestas_trivia",
                column: "preguntaid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "opciones_snapshot");

            migrationBuilder.DropTable(
                name: "respuestas_trivia");

            migrationBuilder.DropTable(
                name: "preguntas_snapshot");
        }
    }
}
