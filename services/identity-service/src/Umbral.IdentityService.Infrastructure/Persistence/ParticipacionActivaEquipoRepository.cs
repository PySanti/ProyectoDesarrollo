using Microsoft.EntityFrameworkCore;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;

namespace Umbral.IdentityService.Infrastructure.Persistence;

public sealed class ParticipacionActivaEquipoRepository : IParticipacionActivaEquipoRepository
{
    private readonly IdentityDbContext _db;

    public ParticipacionActivaEquipoRepository(IdentityDbContext db) => _db = db;

    public async Task UpsertAsync(Guid equipoId, Guid partidaId, DateTime fechaUtc, CancellationToken cancellationToken)
    {
        var existe = await _db.ParticipacionesActivasEquipo
            .AnyAsync(x => x.EquipoId == equipoId && x.PartidaId == partidaId, cancellationToken);
        if (existe) return;

        _db.ParticipacionesActivasEquipo.Add(ParticipacionActivaEquipo.Registrar(equipoId, partidaId, fechaUtc));
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveByPartidaAsync(Guid partidaId, CancellationToken cancellationToken)
    {
        var filas = await _db.ParticipacionesActivasEquipo
            .Where(x => x.PartidaId == partidaId).ToListAsync(cancellationToken);
        if (filas.Count == 0) return;
        _db.ParticipacionesActivasEquipo.RemoveRange(filas);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Guid equipoId, Guid partidaId, CancellationToken cancellationToken)
    {
        var fila = await _db.ParticipacionesActivasEquipo
            .FirstOrDefaultAsync(x => x.EquipoId == equipoId && x.PartidaId == partidaId, cancellationToken);
        if (fila is null) return;
        _db.ParticipacionesActivasEquipo.Remove(fila);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExistsByEquipoAsync(Guid equipoId, CancellationToken cancellationToken)
        => _db.ParticipacionesActivasEquipo.AnyAsync(x => x.EquipoId == equipoId, cancellationToken);
}
