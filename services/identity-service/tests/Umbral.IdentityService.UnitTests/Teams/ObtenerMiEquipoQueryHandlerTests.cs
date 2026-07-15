using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.IdentityService.Application.Handlers.Queries;
using Umbral.IdentityService.Application.Queries;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;
using Xunit;

namespace Umbral.IdentityService.UnitTests.Teams;

public class ObtenerMiEquipoQueryHandlerTests
{
    // Fake a mano de IEquipoRepository: solo GetActiveByMemberUserIdAsync se usa aquí.
    private sealed class FakeEquipoRepository : IEquipoRepository
    {
        public Equipo? Activo;
        public Task<Equipo?> GetActiveByMemberUserIdAsync(Guid userId, CancellationToken ct) => Task.FromResult(Activo);
        public Task<bool> ExistsActiveTeamByUserIdAsync(Guid userId, CancellationToken ct) => Task.FromResult(Activo is not null);
        public Task<Equipo?> GetByIdAsync(Guid equipoId, CancellationToken ct) => Task.FromResult(Activo);
        public Task<IReadOnlyList<Equipo>> GetAllAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Equipo>>(Activo is null ? Array.Empty<Equipo>() : new[] { Activo });
        public Task AddAsync(Equipo equipo, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateAsync(Equipo equipo, CancellationToken ct) => Task.CompletedTask;
    }

    [Fact]
    public async Task Sin_equipo_activo_devuelve_null()
    {
        var handler = new ObtenerMiEquipoQueryHandler(new FakeEquipoRepository { Activo = null });

        var result = await handler.Handle(new ObtenerMiEquipoQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Con_equipo_activo_mapea_miembros_y_lider()
    {
        var lider = Guid.NewGuid();
        var miembro = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Los Halcones", lider);
        equipo.AgregarParticipante(miembro);
        var handler = new ObtenerMiEquipoQueryHandler(new FakeEquipoRepository { Activo = equipo });

        var result = await handler.Handle(new ObtenerMiEquipoQuery(lider), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(equipo.EquipoId, result!.EquipoId);
        Assert.Equal("Los Halcones", result.NombreEquipo);
        Assert.Equal("Activo", result.Estado);
        Assert.Equal(2, result.Participantes.Count);
        Assert.True(result.Participantes.Single(p => p.UsuarioId == lider).EsLider);
        Assert.False(result.Participantes.Single(p => p.UsuarioId == miembro).EsLider);
    }
}
