using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbral.OperacionesSesion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SP3bInicioSecuenciacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "fechafin",
                table: "sesiones_partida",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "fechainicio",
                table: "sesiones_partida",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "estadojuego",
                table: "sesion_juegos",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fechafin",
                table: "sesiones_partida");

            migrationBuilder.DropColumn(
                name: "fechainicio",
                table: "sesiones_partida");

            migrationBuilder.DropColumn(
                name: "estadojuego",
                table: "sesion_juegos");
        }
    }
}
