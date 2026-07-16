using Microsoft.EntityFrameworkCore;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;

namespace Umbral.IdentityService.Infrastructure.Persistence;

public sealed class HistorialNombreEquipoRepository : IHistorialNombreEquipoRepository
{
    private readonly IdentityDbContext _db;

    public HistorialNombreEquipoRepository(IdentityDbContext db) => _db = db;

    public async Task AddRangeAsync(IEnumerable<HistorialNombreEquipo> registros, CancellationToken cancellationToken)
    {
        try
        {
            await _db.HistorialNombresEquipo.AddRangeAsync(registros, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new PersistenceException("No fue posible persistir el historial de nombre de equipo.", ex);
        }
    }

    public async Task<IReadOnlyList<HistorialNombreEquipo>> GetByUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken)
        => await _db.HistorialNombresEquipo
            .Where(x => x.SubjectId == usuarioId)
            .OrderBy(x => x.FechaRegistroUtc)
            .ToListAsync(cancellationToken);

    public Task<bool> AnyAsync(CancellationToken cancellationToken)
        => _db.HistorialNombresEquipo.AnyAsync(cancellationToken);
}
