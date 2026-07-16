using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbral.OperacionesSesion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SP3e1AutoaceptarConvocatoriaLider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Guid.Empty en las filas existentes: sin lider registrado no hay auto-aceptado, que
            // es el default seguro. Ninguna inscripcion previa queda auto-aceptada por sorpresa.
            migrationBuilder.AddColumn<Guid>(
                name: "liderid",
                table: "inscripciones",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "liderid",
                table: "inscripciones");
        }
    }
}
