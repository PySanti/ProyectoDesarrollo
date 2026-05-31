using System.Net;
using System.Text.Json;
using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.Common.Exceptions;

namespace Umbral.TriviaGame.Api.Middleware;

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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail) = exception switch
        {
            TriviaFormNotFoundException => (
                HttpStatusCode.NotFound,
                "Formulario no encontrado",
                exception.Message),
            PartidaTriviaNotFoundException => (
                HttpStatusCode.NotFound,
                "Partida no encontrada",
                exception.Message),
            InvalidStateTransitionException => (
                HttpStatusCode.Conflict,
                "Transición de estado inválida",
                exception.Message),
            MinimosNoCumplidosException => (
                HttpStatusCode.Conflict,
                "Mínimos de participación no cumplidos",
                exception.Message),
            ModalidadInvalidaException => (
                HttpStatusCode.Conflict,
                "Modalidad inválida",
                exception.Message),
            CupoLlenoException => (
                HttpStatusCode.Conflict,
                "Cupo lleno",
                exception.Message),
            JugadorYaInscritoException => (
                HttpStatusCode.Conflict,
                "Jugador ya inscrito",
                exception.Message),
            UsuarioNoInscritoException => (
                HttpStatusCode.Forbidden,
                "Usuario no inscrito",
                exception.Message),
            ArgumentOutOfRangeException => (
                HttpStatusCode.BadRequest,
                "Argumento inválido",
                exception.Message),
            DomainValidationException => (
                HttpStatusCode.BadRequest,
                "Error de validación del dominio",
                exception.Message),
            _ => (
                HttpStatusCode.InternalServerError,
                "Error interno del servidor",
                "Ocurrió un error inesperado.")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Error interno no manejado.");
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            type = "https://tools.ietf.org/html/rfc7231",
            title,
            status = (int)statusCode,
            detail,
            instance = context.Request.Path
        };

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}
