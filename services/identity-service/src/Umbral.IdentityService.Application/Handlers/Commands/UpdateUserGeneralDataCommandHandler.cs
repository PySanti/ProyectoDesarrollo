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
        // contraseña temporal pendiente (no completó el cambio en su primer inicio de sesión).
        var mustResendCredentials = emailChanged
            && await _keycloakIdentityPort.HasTemporaryPasswordAsync(user.KeycloakId, cancellationToken);

        user.EditarDatosGenerales(request.Name, normalizedEmail);
        await _usuarioRepository.UpdateAsync(user, cancellationToken);

        if (mustResendCredentials)
        {
            try
            {
                // El correo temporal original es irrecuperable (RB-U03): se sincroniza el email en
                // Keycloak y se genera/reasigna una nueva contraseña temporal para poder enviarla.
                await _keycloakIdentityPort.UpdateEmailAsync(user.KeycloakId, user.Correo, cancellationToken);

                var newTemporaryPassword = _temporaryPasswordGenerator.Generate();
                await _keycloakIdentityPort.ResetTemporaryPasswordAsync(user.KeycloakId, newTemporaryPassword, cancellationToken);

                // Re-emisión vía evento async (RabbitMQ), no correo inline (7f, BR-R05): best-effort
                // (ADR-0012) — un fallo de publicación se loguea pero no revierte este cambio, que ya
                // quedó consumado en Keycloak (email + password reseteada).
                await _identityEventsPublisher.PublishCredencialTemporalEmitidaAsync(
                    new CredencialTemporalEmitidaIntegrationEvent(user.Nombre, user.Correo, user.Rol.ToString(), newTemporaryPassword, DateTime.UtcNow),
                    cancellationToken);
            }
            catch
            {
                // Falló el reenvío: revertir la edición (local + email en Keycloak) al estado previo.
                // La contraseña ya reseteada se deja como está (la anterior era irrecuperable).
                await RevertEmailChangeAsync(user, previousName, previousEmail, cancellationToken);
                throw;
            }
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
            await _keycloakIdentityPort.UpdateEmailAsync(user.KeycloakId, previousEmail, cancellationToken);
        }
        catch
        {
            // Compensación best-effort: no enmascarar la excepción original.
        }
    }
}
