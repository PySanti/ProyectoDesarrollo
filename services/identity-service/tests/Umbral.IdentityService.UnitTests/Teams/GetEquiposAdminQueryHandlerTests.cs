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

public class GetEquiposAdminQueryHandlerTests
{
    // Fake a mano de IEquipoRepository: solo GetAllAsync/GetByIdAsync se usan aquí.
    private sealed class FakeEquipoRepository : IEquipoRepository
    {
        public IReadOnlyList<Equipo> Equipos = Array.Empty<Equipo>();
        public Task<IReadOnlyList<Equipo>> GetAllAsync(CancellationToken ct) => Task.FromResult(Equipos);
        public Task<Equipo?> GetActiveByMemberUserIdAsync(Guid userId, CancellationToken ct) => Task.FromResult<Equipo?>(null);
        public Task<bool> ExistsActiveTeamByUserIdAsync(Guid userId, CancellationToken ct) => Task.FromResult(false);
        public Task<Equipo?> GetByIdAsync(Guid equipoId, CancellationToken ct)
            => Task.FromResult(Equipos.SingleOrDefault(e => e.EquipoId == equipoId));
        public Task AddAsync(Equipo equipo, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateAsync(Equipo equipo, CancellationToken ct) => Task.CompletedTask;
    }

    [Fact]
    public async Task Devuelve_equipos_de_todos_los_estados_con_mapeo_correcto()
    {
        var liderActivo = Guid.NewGuid();
        var miembroActivo = Guid.NewGuid();
        var equipoActivo = Equipo.CrearPorParticipante("Los Halcones", liderActivo);
        equipoActivo.AgregarParticipante(miembroActivo);

        var liderEliminado = Guid.NewGuid();
        var equipoEliminado = Equipo.CrearPorParticipante("Los Zorros", liderEliminado);
        equipoEliminado.EliminarPorLider(liderEliminado);

        var repo = new FakeEquipoRepository { Equipos = new List<Equipo> { equipoActivo, equipoEliminado } };
        var handler = new GetEquiposAdminQueryHandler(repo);

        var result = await handler.Handle(new GetEquiposAdminQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);

        var activoDto = result.Single(r => r.EquipoId == equipoActivo.EquipoId);
        Assert.Equal("Los Halcones", activoDto.NombreEquipo);
        Assert.Equal("Activo", activoDto.Estado);
        Assert.Equal(liderActivo, activoDto.LiderUserId);
        Assert.Equal(2, activoDto.Integrantes.Count);
        Assert.True(activoDto.Integrantes.Single(i => i.UsuarioId == liderActivo).EsLider);
        Assert.False(activoDto.Integrantes.Single(i => i.UsuarioId == miembroActivo).EsLider);

        var eliminadoDto = result.Single(r => r.EquipoId == equipoEliminado.EquipoId);
        Assert.Equal("Los Zorros", eliminadoDto.NombreEquipo);
        Assert.Equal("Eliminado", eliminadoDto.Estado);
    }
}
