using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.IdentityService.Application.Handlers.Queries;
using Umbral.IdentityService.Application.Queries;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;
using Xunit;

namespace Umbral.IdentityService.UnitTests.Teams;

public class ListarEquiposQueryHandlerTests
{
    private sealed class FakeEquipoRepository : IEquipoRepository
    {
        public List<Equipo> Equipos = new();
        public Task<IReadOnlyList<Equipo>> GetAllAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Equipo>>(Equipos);
        public Task<Equipo?> GetActiveByMemberUserIdAsync(Guid userId, CancellationToken ct) =>
            Task.FromResult<Equipo?>(null);
        public Task<bool> ExistsActiveTeamByUserIdAsync(Guid userId, CancellationToken ct) =>
            Task.FromResult(false);
        public Task<Equipo?> GetByIdAsync(Guid equipoId, CancellationToken ct) =>
            Task.FromResult<Equipo?>(null);
        public Task AddAsync(Equipo equipo, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateAsync(Equipo equipo, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeUsuarioRepository : IUsuarioRepository
    {
        public List<Usuario> Usuarios = new();
        public Task<IReadOnlyList<Usuario>> GetAllAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Usuario>>(Usuarios);
        public Task<Usuario?> GetByIdAsync(Guid userId, CancellationToken ct) =>
            Task.FromResult<Usuario?>(null);
        public Task<bool> ExistsByEmailAsync(string email, Guid? excludingUserId, CancellationToken ct) =>
            Task.FromResult(false);
        public Task AddAsync(Usuario usuario, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateAsync(Usuario usuario, CancellationToken ct) => Task.CompletedTask;
        public Task RemoveAsync(Usuario usuario, CancellationToken ct) => Task.CompletedTask;
    }

    [Fact]
    public async Task Sin_equipos_devuelve_lista_vacia()
    {
        var handler = new ListarEquiposQueryHandler(new FakeEquipoRepository(), new FakeUsuarioRepository());

        var result = await handler.Handle(new ListarEquiposQuery(), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Mapea_equipo_con_nombres_de_miembros_y_lider()
    {
        // ParticipanteEquipo.UsuarioId guarda el sub de Keycloak (así lo puebla
        // AuthenticatedUserClaims.TryGetUserId), no el UsuarioId local de Usuario.Crear.
        // Por eso el usuario se crea con KeycloakId = lider.ToString(): pinnea el join
        // por KeycloakId en el handler.
        var lider = Guid.NewGuid();
        var liderUsuario = Usuario.Crear(lider.ToString(), "Ana", "ana@umbral.test", RolUsuario.Participante);
        var miembro = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Los Halcones", lider);
        equipo.AgregarParticipante(miembro);
        var usuarios = new FakeUsuarioRepository();
        usuarios.Usuarios.Add(liderUsuario);
        var handler = new ListarEquiposQueryHandler(
            new FakeEquipoRepository { Equipos = { equipo } }, usuarios);

        var result = await handler.Handle(new ListarEquiposQuery(), CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal(equipo.EquipoId, item.EquipoId);
        Assert.Equal("Los Halcones", item.NombreEquipo);
        Assert.Equal("Activo", item.Estado);
        Assert.Equal(2, item.Participantes.Count);
        var pLider = item.Participantes.Single(p => p.UsuarioId == lider);
        Assert.Equal("Ana", pLider.Nombre);
        Assert.True(pLider.EsLider);
        // Usuario no registrado en la tabla local → nombre vacío, no explota.
        var pMiembro = item.Participantes.Single(p => p.UsuarioId == miembro);
        Assert.Equal("", pMiembro.Nombre);
        Assert.False(pMiembro.EsLider);
    }
}
