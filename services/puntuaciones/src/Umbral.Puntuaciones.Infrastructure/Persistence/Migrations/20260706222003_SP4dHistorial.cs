using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Umbral.Puntuaciones.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SP4dHistorial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "eventos_historial",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    eventid = table.Column<Guid>(type: "uuid", nullable: false),
                    partidaid = table.Column<Guid>(type: "uuid", nullable: false),
                    juegoid = table.Column<Guid>(type: "uuid", nullable: true),
                    tipoevento = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    occurredat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    participanteid = table.Column<Guid>(type: "uuid", nullable: true),
                    equipoid = table.Column<Guid>(type: "uuid", nullable: true),
                    detalle = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eventos_historial", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_eventos_procesados_procesadoat",
                table: "eventos_procesados",
                column: "procesadoat");

            migrationBuilder.CreateIndex(
                name: "ix_eventos_historial_eventid",
                table: "eventos_historial",
                column: "eventid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_eventos_historial_partidaid_occurredat",
                table: "eventos_historial",
                columns: new[] { "partidaid", "occurredat" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "eventos_historial");

            migrationBuilder.DropIndex(
                name: "ix_eventos_procesados_procesadoat",
                table: "eventos_procesados");
        }
    }
}
