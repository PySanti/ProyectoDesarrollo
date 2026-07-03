using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Domain.Exceptions;

namespace Umbral.OperacionesSesion.Api.Middleware;

// Centralized exception handling with domain/application exception → status mapping (SP-3a).
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
            var status = MapStatus(ex);
            if (status == HttpStatusCode.InternalServerError)
            {
                _logger.LogError(ex, "Unhandled exception.");
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)status;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { message = ex.Message }));
        }
    }

    private static HttpStatusCode MapStatus(Exception ex) => ex switch
    {
        ParticipanteNoIdentificadoException => HttpStatusCode.Unauthorized,
        ParticipanteNoInscritoException
            or NoEsLiderEquipoException => HttpStatusCode.Forbidden,
        PartidaConfigNoEncontradaException
            or SesionNoEncontradaException
            or InscripcionNoEncontradaException
            or ConvocatoriaNoEncontradaException => HttpStatusCode.NotFound,
        PartidasConfigInaccesibleException
            or IdentityInaccesibleException => HttpStatusCode.BadGateway,
        DbUpdateConcurrencyException => HttpStatusCode.Conflict,
        SesionYaPublicadaException
            or PartidaNoPublicableException
            or SesionNoEnLobbyException
            or ModalidadNoSoportadaException
            or ParticipanteYaInscritoException
            or ParticipacionActivaExistenteException
            or CupoLlenoException
            or ModoInicioNoCompatibleException
            or SesionNoIniciadaException
            or JuegoActivoNoEsTriviaException
            or NoHayPreguntaActivaException
            or RespuestaDuplicadaException
            or PreguntaFueraDeTiempoException
            or JuegoConPreguntasPendientesException
            or JuegoActivoNoEsBDTException
            or NoHayEtapaActivaException
            or JuegoConEtapasPendientesException
            or EquipoYaInscritoException
            or SinEquipoActivoException => HttpStatusCode.Conflict,
        ValidationException or ArgumentException => HttpStatusCode.BadRequest,
        _ => HttpStatusCode.InternalServerError
    };
}
