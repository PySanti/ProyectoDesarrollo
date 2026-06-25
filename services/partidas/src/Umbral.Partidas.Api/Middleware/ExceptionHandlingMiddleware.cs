using System.Net;
using System.Text.Json;
using FluentValidation;
using Umbral.Partidas.Application.Exceptions;
using Umbral.Partidas.Domain.Exceptions;

namespace Umbral.Partidas.Api.Middleware;

// Centralized exception handling with domain/application exception → status mapping (SP-2).
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
        PartidaNoEncontradaException => HttpStatusCode.NotFound,
        JuegoDuplicadoException or OrdenJuegoDuplicadoException => HttpStatusCode.Conflict,
        ValidationException
            or PreguntaInvalidaException
            or JuegoTriviaSinPreguntasException
            or EtapaBDTInvalidaException
            or JuegoBDTSinEtapasException
            or AreaBusquedaRequeridaException
            or PartidaSinJuegosException
            or OrdenJuegosNoContiguoException
            or ArgumentException => HttpStatusCode.BadRequest,
        _ => HttpStatusCode.InternalServerError
    };
}
