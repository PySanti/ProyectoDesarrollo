using Umbral.OperacionesSesion.Application.DTOs;

namespace Umbral.OperacionesSesion.Application.Interfaces;

public interface IEquipoDirectoryClient
{
    Task<EquipoSnapshotDto?> ObtenerMiEquipoAsync(string? bearerToken, CancellationToken cancellationToken);
}
