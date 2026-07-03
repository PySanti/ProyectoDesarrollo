using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbral.OperacionesSesion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SP3eParticipacionEquipo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "equipoid",
                table: "inscripciones",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "modalidad",
                table: "inscripciones",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "convocatorias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    partidaid = table.Column<Guid>(type: "uuid", nullable: false),
                    equipoid = table.Column<Guid>(type: "uuid", nullable: false),
                    usuarioid = table.Column<Guid>(type: "uuid", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false),
                    fechaenvio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fecharespuesta = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    inscripcionid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_convocatorias", x => x.id);
                    table.ForeignKey(
                        name: "FK_convocatorias_inscripciones_inscripcionid",
                        column: x => x.inscripcionid,
                        principalTable: "inscripciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_convocatorias_inscripcionid",
                table: "convocatorias",
                column: "inscripcionid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "convocatorias");

            migrationBuilder.DropColumn(
                name: "equipoid",
                table: "inscripciones");

            migrationBuilder.DropColumn(
                name: "modalidad",
                table: "inscripciones");
        }
    }
}
