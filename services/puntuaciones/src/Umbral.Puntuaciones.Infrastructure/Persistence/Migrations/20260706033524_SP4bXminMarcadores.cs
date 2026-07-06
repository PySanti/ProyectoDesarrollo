using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbral.Puntuaciones.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SP4bXminMarcadores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // xmin es una columna de sistema de PostgreSQL (siempre presente): no hay operación
            // de esquema que aplicar. Esta migración solo actualiza el model snapshot para
            // reflejar su uso como token de concurrencia optimista (ver PuntuacionesDbContext).
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
