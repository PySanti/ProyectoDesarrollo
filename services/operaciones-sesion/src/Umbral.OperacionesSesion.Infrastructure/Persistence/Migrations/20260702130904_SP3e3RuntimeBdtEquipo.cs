using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbral.OperacionesSesion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SP3e3RuntimeBdtEquipo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "equipoid",
                table: "tesoros_qr",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ganadorequipoid",
                table: "etapas_snapshot",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "equipoid",
                table: "tesoros_qr");

            migrationBuilder.DropColumn(
                name: "ganadorequipoid",
                table: "etapas_snapshot");
        }
    }
}
