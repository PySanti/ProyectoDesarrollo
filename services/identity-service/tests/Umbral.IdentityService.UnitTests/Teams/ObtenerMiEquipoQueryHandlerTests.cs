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

    private sealed class FakeUsuarioRepository : IUsuarioRepository
    {
        public List<Usuario> Usuarios = new();
        public Task<IReadOnlyList<Usuario>> GetAllAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Usuario>>(Usuarios);
        public Task<Usuario?> GetByIdAsync(Guid userId, CancellationToken ct) =>
            Task.FromResult<Usuario?>(null);
        public Task<Usuario?> GetByKeycloakIdAsync(Guid keycloakId, CancellationToken ct) =>
            Task.FromResult<Usuario?>(Usuarios.FirstOrDefault(u => u.KeycloakId == keycloakId.ToString()));
        public Task<bool> ExistsByEmailAsync(string email, Guid? excludingUserId, CancellationToken ct) =>
            Task.FromResult(false);
        public Task AddAsync(Usuario usuario, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateAsync(Usuario usuario, CancellationToken ct) => Task.CompletedTask;
        public Task RemoveAsync(Usuario usuario, CancellationToken ct) => Task.CompletedTask;
    }

    [Fact]
    public async Task Sin_equipo_activo_devuelve_null()
    {
        var handler = new ObtenerMiEquipoQueryHandler(new FakeEquipoRepository { Activo = null }, new FakeUsuarioRepository());

        var result = await handler.Handle(new ObtenerMiEquipoQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Con_equipo_activo_mapea_miembros_lider_y_nombre()
    {
        // ParticipanteEquipo.UsuarioId guarda el sub de Keycloak, no el UsuarioId local:
        // el nombre se resuelve por KeycloakId (igual que ListarEquiposQueryHandler).
        var lider = Guid.NewGuid();
        var liderUsuario = Usuario.Crear(lider.ToString(), "Ana", "ana@umbral.test", RolUsuario.Participante);
        var miembro = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Los Halcones", lider);
        equipo.AgregarParticipante(miembro);
        var usuarios = new FakeUsuarioRepository();
        usuarios.Usuarios.Add(liderUsuario);
        var handler = new ObtenerMiEquipoQueryHandler(new FakeEquipoRepository { Activo = equipo }, usuarios);

        var result = await handler.Handle(new ObtenerMiEquipoQuery(lider), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(equipo.EquipoId, result!.EquipoId);
        Assert.Equal("Los Halcones", result.NombreEquipo);
        Assert.Equal("Activo", result.Estado);
        Assert.Equal(2, result.Participantes.Count);
        var pLider = result.Participantes.Single(p => p.UsuarioId == lider);
        Assert.Equal("Ana", pLider.Nombre);
        Assert.True(pLider.EsLider);
        // Usuario no registrado en la tabla local → nombre vacío, no explota.
        var pMiembro = result.Participantes.Single(p => p.UsuarioId == miembro);
        Assert.Equal("", pMiembro.Nombre);
        Assert.False(pMiembro.EsLider);
    }
}
