using MediatR;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Domain.Entities;

using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
namespace Umbral.IdentityService.Application.Handlers.Commands;

public sealed class UpdateUserGeneralDataCommandHandler : IRequestHandler<UpdateUserGeneralDataCommand, UpdateUserGeneralDataResponse>
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IKeycloakIdentityPort _keycloakIdentityPort;
    private readonly ITemporaryPasswordGenerator _temporaryPasswordGenerator;
    private readonly IIdentityEventsPublisher _identityEventsPublisher;

    public UpdateUserGeneralDataCommandHandler(
        IUsuarioRepository usuarioRepository,
        IKeycloakIdentityPort keycloakIdentityPort,
        ITemporaryPasswordGenerator temporaryPasswordGenerator,
        IIdentityEventsPublisher identityEventsPublisher)
    {
        _usuarioRepository = usuarioRepository;
        _keycloakIdentityPort = keycloakIdentityPort;
        _temporaryPasswordGenerator = temporaryPasswordGenerator;
        _identityEventsPublisher = identityEventsPublisher;
    }

    public async Task<UpdateUserGeneralDataResponse> Handle(UpdateUserGeneralDataCommand request, CancellationToken cancellationToken)
    {
        var user = await _usuarioRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            throw new UserNotFoundException(request.UserId);
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await _usuarioRepository.ExistsByEmailAsync(normalizedEmail, request.UserId, cancellationToken))
        {
            throw new DuplicateEmailException(normalizedEmail);
        }

        var previousName = user.Nombre;
        var previousEmail = user.Correo;
        var emailChanged = !string.Equals(previousEmail, normalizedEmail, StringComparison.Ordinal);

        // Solo se reenvían credenciales cuando el correo realmente cambia y el usuario todavía tiene
        // contraseña temporal pendiente (no completó el cambio en su primer inicio de sesión). Esto
        // condiciona el reenvío, nunca la sincronización con Keycloak: ver el bloque de abajo.
        var mustResendCredentials = emailChanged
            && await _keycloakIdentityPort.HasTemporaryPasswordAsync(user.KeycloakId, cancellationToken);

        user.EditarDatosGenerales(request.Name, normalizedEmail);
        await _usuarioRepository.UpdateAsync(user, cancellationToken);

        try
        {
            // Se sincroniza SIEMPRE, cambie o no algo y haya o no credencial temporal. Keycloak es quien
            // autentica: un correo que se quede solo en la BD local deja al usuario sin poder iniciar
            // sesión con él. Sincronizar incondicionalmente además hace de esta edición el camino de
            // reparación de cualquier usuario que ya haya quedado desincronizado: guardar reconcilia.
            await _keycloakIdentityPort.SyncUserProfileAsync(user.KeycloakId, user.Nombre, user.Correo, cancellationToken);

            if (mustResendCredentials)
            {
                // El correo temporal original es irrecuperable (RB-U03): se genera/reasigna una
                // nueva contraseña temporal para poder enviarla.
                var newTemporaryPassword = _temporaryPasswordGenerator.Generate();
                await _keycloakIdentityPort.ResetTemporaryPasswordAsync(user.KeycloakId, newTemporaryPassword, cancellationToken);

                // Re-emisión vía evento async (RabbitMQ), no correo inline (7f, BR-R05): best-effort
                // (ADR-0012) — un fallo de publicación se loguea pero no revierte este cambio, que ya
                // quedó consumado en Keycloak (email + password reseteada).
                await _identityEventsPublisher.PublishCredencialTemporalEmitidaAsync(
                    new CredencialTemporalEmitidaIntegrationEvent(user.Nombre, user.Correo, user.Rol.ToString(), newTemporaryPassword, DateTime.UtcNow),
                    cancellationToken);
            }
        }
        catch
        {
            // Falló la sincronización o el reenvío: revertir la edición (local + Keycloak) al estado
            // previo. La contraseña ya reseteada se deja como está (la anterior era irrecuperable).
            await RevertEmailChangeAsync(user, previousName, previousEmail, cancellationToken);
            throw;
        }

        return new UpdateUserGeneralDataResponse(
            user.UsuarioId,
            user.Nombre,
            user.Correo,
            user.Rol.ToString(),
            user.Estado.ToString());
    }

    private async Task RevertEmailChangeAsync(Usuario user, string previousName, string previousEmail, CancellationToken cancellationToken)
    {
        try
        {
            user.EditarDatosGenerales(previousName, previousEmail);
            await _usuarioRepository.UpdateAsync(user, cancellationToken);
        }
        catch
        {
            // Compensación best-effort: no enmascarar la excepción original.
        }

        try
        {
            await _keycloakIdentityPort.SyncUserProfileAsync(user.KeycloakId, previousName, previousEmail, cancellationToken);
        }
        catch
        {
            // Compensación best-effort: no enmascarar la excepción original.
        }
    }
}
