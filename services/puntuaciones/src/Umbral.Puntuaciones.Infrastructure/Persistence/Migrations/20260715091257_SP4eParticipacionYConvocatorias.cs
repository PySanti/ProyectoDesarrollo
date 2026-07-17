using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbral.Puntuaciones.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SP4eParticipacionYConvocatorias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "convocatorias_proyectadas",
                columns: table => new
                {
                    convocatoriaid = table.Column<Guid>(type: "uuid", nullable: false),
                    partidaid = table.Column<Guid>(type: "uuid", nullable: false),
                    equipoid = table.Column<Guid>(type: "uuid", nullable: false),
                    usuarioid = table.Column<Guid>(type: "uuid", nullable: false),
                    aceptada = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_convocatorias_proyectadas", x => x.convocatoriaid);
                });

            migrationBuilder.CreateTable(
                name: "participaciones_proyectadas",
                columns: table => new
                {
                    partidaid = table.Column<Guid>(type: "uuid", nullable: false),
                    competidorid = table.Column<Guid>(type: "uuid", nullable: false),
                    tipocompetidor = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_participaciones_proyectadas", x => new { x.partidaid, x.competidorid });
                });

            migrationBuilder.CreateIndex(
                name: "ix_convocatorias_proyectadas_usuarioid",
                table: "convocatorias_proyectadas",
                column: "usuarioid");

            migrationBuilder.CreateIndex(
                name: "ix_participaciones_proyectadas_competidorid",
                table: "participaciones_proyectadas",
                column: "competidorid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "convocatorias_proyectadas");

            migrationBuilder.DropTable(
                name: "participaciones_proyectadas");
        }
    }
}
