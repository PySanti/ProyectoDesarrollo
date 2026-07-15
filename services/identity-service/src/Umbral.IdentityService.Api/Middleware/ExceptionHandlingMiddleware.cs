using System.Net;
using System.Text.Json;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Domain.Exceptions;

namespace Umbral.IdentityService.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var status = exception switch
        {
            AlreadyBelongsToActiveTeamException => HttpStatusCode.Conflict,
            ConcurrentTeamCreationException     => HttpStatusCode.Conflict,
            UsuarioYaEnEquipoException           => HttpStatusCode.Conflict,
            LeaveTeamConflictException           => HttpStatusCode.Conflict,
            TransferirLiderazgoConflictException => HttpStatusCode.Conflict,
            EquipoLlenoException                 => HttpStatusCode.Conflict,
            InvitacionPendienteYaExisteException => HttpStatusCode.Conflict,
            DuplicateEmailException              => HttpStatusCode.Conflict,
            RolDeAdministradorInmutableException => HttpStatusCode.Conflict,
            UsuarioConEquipoActivoException       => HttpStatusCode.Conflict,
            EquipoConParticipacionActivaException => HttpStatusCode.Conflict,
            NoActiveTeamForParticipantException  => HttpStatusCode.NotFound,
            InvitacionNoEncontradaException      => HttpStatusCode.NotFound,
            UserNotFoundException                => HttpStatusCode.NotFound,
            EquipoNoEncontradoException          => HttpStatusCode.NotFound,
            NoEsLiderException                   => HttpStatusCode.Forbidden,
            KeycloakIntegrationException         => HttpStatusCode.BadGateway,
            EmailDeliveryException               => HttpStatusCode.BadGateway,
            PersistenceException                 => HttpStatusCode.InternalServerError,
            _                                    => HttpStatusCode.InternalServerError
        };

        if (status == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception.");
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { message = exception.Message }));
    }
}
