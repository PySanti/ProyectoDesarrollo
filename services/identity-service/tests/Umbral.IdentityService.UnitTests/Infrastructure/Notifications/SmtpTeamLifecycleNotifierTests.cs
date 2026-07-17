using Umbral.IdentityService.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;
using Umbral.IdentityService.Infrastructure.Services.Notifications;
using Xunit;

namespace Umbral.IdentityService.UnitTests.Infrastructure.Notifications;

/// <summary>
/// Prueba de resolución de destinatarios de <see cref="SmtpTeamLifecycleNotifier"/>. Los
/// miembros de equipo se identifican en espacio KeycloakId (el `sub` del JWT), no en el
/// UsuarioId local (ver <c>Equipo.EliminarPorLider</c>/<c>EliminarPorAdmin</c> y
/// <c>ReasignarLiderazgoPorAdmin</c>). El notificador debe resolver por KeycloakId; no debe
/// consultar por UsuarioId local.
///
/// No se mockea <see cref="System.Net.Mail.SmtpClient"/> (no es mockeable); en su lugar se deja
/// la configuración SMTP vacía para que el guard de configuración corte el flujo antes del envío
/// real, dejando la prueba enfocada exclusivamente en el camino de resolución del destinatario.
/// </summary>
public sealed class SmtpTeamLifecycleNotifierTests
{
    [Fact]
    public async Task NotificarEquipoEliminadoAsync_resuelve_destinatarios_por_KeycloakId_no_por_UsuarioId_local()
    {
        var keycloakId1 = Guid.NewGuid();
        var keycloakId2 = Guid.NewGuid();
        var repository = new FakeUsuarioRepository(new[]
        {
            Usuario.Crear(keycloakId1.ToString(), "Ana", "ana@umbral.dev", RolUsuario.Participante),
            Usuario.Crear(keycloakId2.ToString(), "Beto", "beto@umbral.dev", RolUsuario.Participante)
        });
        var notifier = CreateNotifier(repository);

        await notifier.NotificarEquipoEliminadoAsync(
            "Titanes",
            new List<Guid> { keycloakId1, keycloakId2 },
            CancellationToken.None);

        Assert.Equal(new[] { keycloakId1, keycloakId2 }, repository.GetByKeycloakIdCalls);
        Assert.Empty(repository.GetByIdCalls);
    }

    [Fact]
    public async Task NotificarLiderazgoModificadoAsync_resuelve_destinatarios_por_KeycloakId_no_por_UsuarioId_local()
    {
        var liderAnteriorKeycloakId = Guid.NewGuid();
        var nuevoLiderKeycloakId = Guid.NewGuid();
        var repository = new FakeUsuarioRepository(new[]
        {
            Usuario.Crear(liderAnteriorKeycloakId.ToString(), "Ana", "ana@umbral.dev", RolUsuario.Participante),
            Usuario.Crear(nuevoLiderKeycloakId.ToString(), "Beto", "beto@umbral.dev", RolUsuario.Participante)
        });
        var notifier = CreateNotifier(repository);

        await notifier.NotificarLiderazgoModificadoAsync(
            liderAnteriorKeycloakId,
            nuevoLiderKeycloakId,
            CancellationToken.None);

        Assert.Equal(new[] { liderAnteriorKeycloakId, nuevoLiderKeycloakId }, repository.GetByKeycloakIdCalls);
        Assert.Empty(repository.GetByIdCalls);
    }

    private static SmtpTeamLifecycleNotifier CreateNotifier(FakeUsuarioRepository repository)
    {
        // Host vacío a propósito: el notificador corta antes de intentar el envío real por SMTP
        // (guard de configuración best-effort), así la prueba no depende de un servidor SMTP.
        var options = Options.Create(new SmtpOptions());
        return new SmtpTeamLifecycleNotifier(repository, options, NullLogger<SmtpTeamLifecycleNotifier>.Instance);
    }

    /// <summary>
    /// Fake que registra por cuál método/espacio de id fue consultado, para poder distinguir
    /// una resolución por KeycloakId (correcta) de una resolución por UsuarioId local (el bug).
    /// </summary>
    private sealed class FakeUsuarioRepository : IUsuarioRepository
    {
        private readonly List<Usuario> _usuarios;

        public FakeUsuarioRepository(IEnumerable<Usuario> usuarios)
        {
            _usuarios = usuarios.ToList();
        }

        public List<UsuarioLocalId> GetByIdCalls { get; } = new();
        public List<Guid> GetByKeycloakIdCalls { get; } = new();

        public Task<IReadOnlyList<Usuario>> GetAllAsync(CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<Usuario?> GetByIdAsync(UsuarioLocalId userId, CancellationToken cancellationToken)
        {
            GetByIdCalls.Add(userId);
            return Task.FromResult(_usuarios.FirstOrDefault(u => u.UsuarioId == userId));
        }

        /// <summary>
        /// Método adicional (todavía fuera de <see cref="IUsuarioRepository"/> en RED). Cuando el
        /// fix agregue este miembro a la interfaz, esta implementación implícita ya calza por
        /// firma y la prueba pasa a GREEN sin más cambios en el fake.
        /// </summary>
        public Task<Usuario?> GetByKeycloakIdAsync(Guid keycloakId, CancellationToken cancellationToken)
        {
            GetByKeycloakIdCalls.Add(keycloakId);
            var buscado = keycloakId.ToString();
            return Task.FromResult(_usuarios.FirstOrDefault(u => u.KeycloakId == buscado));
        }

        public Task<bool> ExistsByEmailAsync(string email, UsuarioLocalId? excludingUserId, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task AddAsync(Usuario usuario, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task RemoveAsync(Usuario usuario, CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }
}
