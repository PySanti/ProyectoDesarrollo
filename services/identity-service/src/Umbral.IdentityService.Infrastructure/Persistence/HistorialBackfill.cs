using Microsoft.EntityFrameworkCore;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;

namespace Umbral.IdentityService.Infrastructure.Persistence;

public static class HistorialBackfill
{
    public static async Task EjecutarAsync(IdentityDbContext db, TimeProvider time, CancellationToken cancellationToken)
    {
        if (await db.HistorialNombresEquipo.AnyAsync(cancellationToken))
            return;

        var equipos = await db.Equipos
            .Include(e => e.Participantes)
            .Where(e => e.Estado == EstadoEquipo.Activo)
            .ToListAsync(cancellationToken);

        var ahora = time.GetUtcNow().UtcDateTime;
        var filas = equipos.SelectMany(e => e.Participantes
            .Select(p => HistorialNombreEquipo.Registrar(p.UsuarioId, e.EquipoId, e.NombreEquipo, ahora)));

        db.HistorialNombresEquipo.AddRange(filas);
        await db.SaveChangesAsync(cancellationToken);
    }
}
