using Umbral.IdentityService.Domain.ValueObjects;
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

namespace Umbral.IdentityService.UnitTests.Directory;

public class ResolverNombresQueryHandlerTests
{
    private sealed class FakeUsuarioRepository : IUsuarioRepository
    {
        public List<Usuario> Usuarios = new();
        public int GetAllCalls;
        public Task<IReadOnlyList<Usuario>> GetAllAsync(CancellationToken ct)
        {
            GetAllCalls++;
            return Task.FromResult<IReadOnlyList<Usuario>>(Usuarios);
        }
        public Task<Usuario?> GetByIdAsync(UsuarioLocalId userId, CancellationToken ct) => Task.FromResult<Usuario?>(null);
        public Task<Usuario?> GetByKeycloakIdAsync(Guid keycloakId, CancellationToken ct) =>
            Task.FromResult<Usuario?>(Usuarios.FirstOrDefault(u => u.KeycloakId == keycloakId.ToString()));
        public Task<bool> ExistsByEmailAsync(string email, UsuarioLocalId? excludingUserId, CancellationToken ct) => Task.FromResult(false);
        public Task AddAsync(Usuario usuario, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateAsync(Usuario usuario, CancellationToken ct) => Task.CompletedTask;
        public Task RemoveAsync(Usuario usuario, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeEquipoRepository : IEquipoRepository
    {
        public List<Equipo> Equipos = new();
        public int GetAllCalls;
        public Task<IReadOnlyList<Equipo>> GetAllAsync(CancellationToken ct)
        {
            GetAllCalls++;
            return Task.FromResult<IReadOnlyList<Equipo>>(Equipos);
        }
        public Task<Equipo?> GetActiveByMemberUserIdAsync(Guid userId, CancellationToken ct) => Task.FromResult<Equipo?>(null);
        public Task<bool> ExistsActiveTeamByUserIdAsync(Guid userId, CancellationToken ct) => Task.FromResult(false);
        public Task<Equipo?> GetByIdAsync(Guid equipoId, CancellationToken ct) => Task.FromResult<Equipo?>(null);
        public Task AddAsync(Equipo equipo, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateAsync(Equipo equipo, CancellationToken ct) => Task.CompletedTask;
    }

    [Fact]
    public async Task Resuelve_participante_por_KeycloakId_y_equipo_por_EquipoId()
    {
        // El competidorId de una partida Individual viaja en el espacio del sub de
        // Keycloak, por eso el usuario se crea con KeycloakId = sub.ToString().
        var sub = Guid.NewGuid();
        var usuarios = new FakeUsuarioRepository();
        usuarios.Usuarios.Add(Usuario.Crear(sub.ToString(), "María González", "maria@umbral.test", RolUsuario.Participante));
        var equipo = Equipo.CrearPorParticipante("Los Cazadores", Guid.NewGuid());
        var equipos = new FakeEquipoRepository { Equipos = { equipo } };
        var handler = new ResolverNombresQueryHandler(usuarios, equipos);

        var result = await handler.Handle(
            new ResolverNombresQuery(new[] { sub }, new[] { equipo.EquipoId }), CancellationToken.None);

        var p = Assert.Single(result.Participantes);
        Assert.Equal(sub, p.ParticipanteId);
        Assert.Equal("María González", p.Nombre);
        var e = Assert.Single(result.Equipos);
        Assert.Equal(equipo.EquipoId, e.EquipoId);
        Assert.Equal("Los Cazadores", e.NombreEquipo);
    }

    [Fact]
    public async Task Omite_ids_desconocidos_en_vez_de_devolver_vacio()
    {
        var handler = new ResolverNombresQueryHandler(new FakeUsuarioRepository(), new FakeEquipoRepository());

        var result = await handler.Handle(
            new ResolverNombresQuery(new[] { Guid.NewGuid() }, new[] { Guid.NewGuid() }), CancellationToken.None);

        Assert.Empty(result.Participantes);
        Assert.Empty(result.Equipos);
    }

    [Fact]
    public async Task Tolera_KeycloakId_no_parseable_a_Guid()
    {
        var sub = Guid.NewGuid();
        var usuarios = new FakeUsuarioRepository();
        // Caso real: KeycloakId se persiste como string y no siempre tiene forma de Guid
        // (ver TestKeycloakIdentityPort, que devuelve Guid con formato "N").
        usuarios.Usuarios.Add(Usuario.Crear("no-es-un-guid", "Fantasma", "f@umbral.test", RolUsuario.Participante));
        usuarios.Usuarios.Add(Usuario.Crear(sub.ToString(), "Ana", "ana@umbral.test", RolUsuario.Participante));
        var handler = new ResolverNombresQueryHandler(usuarios, new FakeEquipoRepository());

        var result = await handler.Handle(
            new ResolverNombresQuery(new[] { sub }, Array.Empty<Guid>()), CancellationToken.None);

        var p = Assert.Single(result.Participantes);
        Assert.Equal("Ana", p.Nombre);
    }

    [Fact]
    public async Task Lista_vacia_no_consulta_el_repositorio()
    {
        var usuarios = new FakeUsuarioRepository();
        var equipos = new FakeEquipoRepository();
        var handler = new ResolverNombresQueryHandler(usuarios, equipos);

        var result = await handler.Handle(
            new ResolverNombresQuery(Array.Empty<Guid>(), Array.Empty<Guid>()), CancellationToken.None);

        Assert.Empty(result.Participantes);
        Assert.Empty(result.Equipos);
        Assert.Equal(0, usuarios.GetAllCalls);
        Assert.Equal(0, equipos.GetAllCalls);
    }
}
