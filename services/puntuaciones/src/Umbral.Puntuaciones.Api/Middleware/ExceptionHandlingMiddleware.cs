using System.Net;
using System.Text.Json;
using FluentValidation;
using Umbral.Puntuaciones.Application.Exceptions;

namespace Umbral.Puntuaciones.Api.Middleware;

// Centralized exception handling. SP-2 adds domain/application exception → status mappings.
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
        JuegoNoEncontradoException or MarcadorNoEncontradoException => HttpStatusCode.NotFound,
        ValidationException or ArgumentException => HttpStatusCode.BadRequest,
        _ => HttpStatusCode.InternalServerError
    };
}
