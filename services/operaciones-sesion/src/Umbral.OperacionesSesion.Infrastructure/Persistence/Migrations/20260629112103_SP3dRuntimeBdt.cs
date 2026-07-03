using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbral.OperacionesSesion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SP3dRuntimeBdt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "areabusqueda",
                table: "sesion_juegos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "etapas_snapshot",
                columns: table => new
                {
                    etapaid = table.Column<Guid>(type: "uuid", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    codigoqresperado = table.Column<string>(type: "text", nullable: false),
                    puntaje = table.Column<int>(type: "integer", nullable: false),
                    tiempolimitesegundos = table.Column<int>(type: "integer", nullable: false),
                    estadoetapa = table.Column<int>(type: "integer", nullable: false),
                    fechaactivacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fechacierre = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivocierre = table.Column<int>(type: "integer", nullable: true),
                    ganadorparticipanteid = table.Column<Guid>(type: "uuid", nullable: true),
                    tiemporesolucionms = table.Column<long>(type: "bigint", nullable: true),
                    juegoid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_etapas_snapshot", x => x.etapaid);
                    table.ForeignKey(
                        name: "FK_etapas_snapshot_sesion_juegos_juegoid",
                        column: x => x.juegoid,
                        principalTable: "sesion_juegos",
                        principalColumn: "juegoid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tesoros_qr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    participanteid = table.Column<Guid>(type: "uuid", nullable: false),
                    qrdecodificado = table.Column<string>(type: "text", nullable: true),
                    resultado = table.Column<int>(type: "integer", nullable: false),
                    fechaenvio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    etapaid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tesoros_qr", x => x.id);
                    table.ForeignKey(
                        name: "FK_tesoros_qr_etapas_snapshot_etapaid",
                        column: x => x.etapaid,
                        principalTable: "etapas_snapshot",
                        principalColumn: "etapaid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_etapas_snapshot_juegoid",
                table: "etapas_snapshot",
                column: "juegoid");

            migrationBuilder.CreateIndex(
                name: "IX_tesoros_qr_etapaid",
                table: "tesoros_qr",
                column: "etapaid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tesoros_qr");

            migrationBuilder.DropTable(
                name: "etapas_snapshot");

            migrationBuilder.DropColumn(
                name: "areabusqueda",
                table: "sesion_juegos");
        }
    }
}
