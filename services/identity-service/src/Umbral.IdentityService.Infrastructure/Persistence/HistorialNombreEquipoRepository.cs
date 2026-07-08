using Microsoft.EntityFrameworkCore;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;

namespace Umbral.IdentityService.Infrastructure.Persistence;

public sealed class HistorialNombreEquipoRepository : IHistorialNombreEquipoRepository
{
    private readonly IdentityDbContext _db;

    public HistorialNombreEquipoRepository(IdentityDbContext db) => _db = db;

    public async Task AddRangeAsync(IEnumerable<HistorialNombreEquipo> registros, CancellationToken cancellationToken)
    {
        await _db.HistorialNombresEquipo.AddRangeAsync(registros, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HistorialNombreEquipo>> GetByUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken)
        => await _db.HistorialNombresEquipo
            .Where(x => x.UsuarioId == usuarioId)
            .OrderBy(x => x.FechaRegistroUtc)
            .ToListAsync(cancellationToken);

    public Task<bool> AnyAsync(CancellationToken cancellationToken)
        => _db.HistorialNombresEquipo.AnyAsync(cancellationToken);
}
