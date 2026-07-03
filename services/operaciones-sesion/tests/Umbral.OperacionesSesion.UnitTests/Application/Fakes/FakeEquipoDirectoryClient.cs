using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Interfaces;

namespace Umbral.OperacionesSesion.UnitTests.Application.Fakes;

public sealed class FakeEquipoDirectoryClient : IEquipoDirectoryClient
{
    public EquipoSnapshotDto? Equipo { get; set; }

    public Task<EquipoSnapshotDto?> ObtenerMiEquipoAsync(string? bearerToken, CancellationToken cancellationToken)
        => Task.FromResult(Equipo);
}
