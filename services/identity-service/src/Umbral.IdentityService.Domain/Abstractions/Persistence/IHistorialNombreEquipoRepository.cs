using Umbral.IdentityService.Domain.Entities;

namespace Umbral.IdentityService.Domain.Abstractions.Persistence;

public interface IHistorialNombreEquipoRepository
{
    Task AddRangeAsync(IEnumerable<HistorialNombreEquipo> registros, CancellationToken cancellationToken);
    Task<IReadOnlyList<HistorialNombreEquipo>> GetByUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken);
    Task<bool> AnyAsync(CancellationToken cancellationToken);
}
