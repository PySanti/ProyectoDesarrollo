using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbral.Partidas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPartidasModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "juegos_bdt",
                columns: table => new
                {
                    juegoid = table.Column<Guid>(type: "uuid", nullable: false),
                    partidaid = table.Column<Guid>(type: "uuid", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false),
                    areabusqueda = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_juegos_bdt", x => x.juegoid);
                });

            migrationBuilder.CreateTable(
                name: "juegos_trivia",
                columns: table => new
                {
                    juegoid = table.Column<Guid>(type: "uuid", nullable: false),
                    partidaid = table.Column<Guid>(type: "uuid", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_juegos_trivia", x => x.juegoid);
                });

            migrationBuilder.CreateTable(
                name: "partidas",
                columns: table => new
                {
                    partidaid = table.Column<Guid>(type: "uuid", nullable: false),
                    nombrepartida = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: true),
                    modalidad = table.Column<int>(type: "integer", nullable: false),
                    modoinicio = table.Column<int>(type: "integer", nullable: false),
                    tiempoinicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    minimos = table.Column<int>(type: "integer", nullable: false),
                    maximos = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partidas", x => x.partidaid);
                });

            migrationBuilder.CreateTable(
                name: "etapas_bdt",
                columns: table => new
                {
                    etapabdtid = table.Column<Guid>(type: "uuid", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    codigoqr = table.Column<string>(type: "text", nullable: false),
                    puntaje = table.Column<int>(type: "integer", nullable: false),
                    tiempolimite = table.Column<int>(type: "integer", nullable: false),
                    juegoid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_etapas_bdt", x => x.etapabdtid);
                    table.ForeignKey(
                        name: "FK_etapas_bdt_juegos_bdt_juegoid",
                        column: x => x.juegoid,
                        principalTable: "juegos_bdt",
                        principalColumn: "juegoid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "preguntas",
                columns: table => new
                {
                    preguntaid = table.Column<Guid>(type: "uuid", nullable: false),
                    texto = table.Column<string>(type: "text", nullable: false),
                    puntaje = table.Column<int>(type: "integer", nullable: false),
                    tiempolimite = table.Column<int>(type: "integer", nullable: false),
                    juegoid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_preguntas", x => x.preguntaid);
                    table.ForeignKey(
                        name: "FK_preguntas_juegos_trivia_juegoid",
                        column: x => x.juegoid,
                        principalTable: "juegos_trivia",
                        principalColumn: "juegoid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "partida_juegos",
                columns: table => new
                {
                    juegoid = table.Column<Guid>(type: "uuid", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    tipojuego = table.Column<int>(type: "integer", nullable: false),
                    partidaid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partida_juegos", x => x.juegoid);
                    table.ForeignKey(
                        name: "FK_partida_juegos_partidas_partidaid",
                        column: x => x.partidaid,
                        principalTable: "partidas",
                        principalColumn: "partidaid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "opciones",
                columns: table => new
                {
                    opcionid = table.Column<Guid>(type: "uuid", nullable: false),
                    texto = table.Column<string>(type: "text", nullable: false),
                    escorrecta = table.Column<bool>(type: "boolean", nullable: false),
                    preguntaid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_opciones", x => x.opcionid);
                    table.ForeignKey(
                        name: "FK_opciones_preguntas_preguntaid",
                        column: x => x.preguntaid,
                        principalTable: "preguntas",
                        principalColumn: "preguntaid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_etapas_bdt_juegoid",
                table: "etapas_bdt",
                column: "juegoid");

            migrationBuilder.CreateIndex(
                name: "ix_juegos_bdt_partidaid",
                table: "juegos_bdt",
                column: "partidaid");

            migrationBuilder.CreateIndex(
                name: "ix_juegos_trivia_partidaid",
                table: "juegos_trivia",
                column: "partidaid");

            migrationBuilder.CreateIndex(
                name: "IX_opciones_preguntaid",
                table: "opciones",
                column: "preguntaid");

            migrationBuilder.CreateIndex(
                name: "IX_partida_juegos_partidaid",
                table: "partida_juegos",
                column: "partidaid");

            migrationBuilder.CreateIndex(
                name: "IX_preguntas_juegoid",
                table: "preguntas",
                column: "juegoid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "etapas_bdt");

            migrationBuilder.DropTable(
                name: "opciones");

            migrationBuilder.DropTable(
                name: "partida_juegos");

            migrationBuilder.DropTable(
                name: "juegos_bdt");

            migrationBuilder.DropTable(
                name: "preguntas");

            migrationBuilder.DropTable(
                name: "partidas");

            migrationBuilder.DropTable(
                name: "juegos_trivia");
        }
    }
}
