using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbral.Partidas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFechaCreacionAPartida : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Centinela en SQL crudo, editado a mano sobre lo que genero EF (que emitia un
            // DateTime con Kind=Unspecified, y Npgsql lo rechaza contra timestamptz).
            //
            // El valor NO es now() a proposito: las partidas anteriores a esta migracion no
            // tienen fecha de creacion recuperable (no esta en el modelo, ni en el id, ni en
            // ningun evento: Partidas no publica). Con now() quedarian fechadas hoy y al tope
            // del listado, que es el lugar reservado a lo ultimo creado -- confiadamente
            // equivocadas. Con el centinela quedan al fondo y muestran una fecha obviamente
            // falsa que nadie se cree.
            migrationBuilder.AddColumn<DateTime>(
                name: "fechacreacion",
                table: "partidas",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "'0001-01-01 00:00:00+00'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fechacreacion",
                table: "partidas");
        }
    }
}
