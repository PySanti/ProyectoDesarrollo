using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbral.OperacionesSesion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SP3e2RuntimeTriviaEquipo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "equipoid",
                table: "respuestas_trivia",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ganadorequipoid",
                table: "preguntas_snapshot",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "equipoid",
                table: "respuestas_trivia");

            migrationBuilder.DropColumn(
                name: "ganadorequipoid",
                table: "preguntas_snapshot");
        }
    }
}
