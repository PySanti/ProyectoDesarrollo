using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Umbral.Puntuaciones.Api.Middleware;
using Umbral.Puntuaciones.Application.Exceptions;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.UnitTests.Api;

public class ExceptionHandlingMiddlewareTests
{
    private sealed class RecordingLogger : ILogger<ExceptionHandlingMiddleware>
    {
        public List<(LogLevel Nivel, string Mensaje)> Entradas { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entradas.Add((logLevel, formatter(state, exception)));
    }

    private static async Task<int> StatusDe(Exception ex)
    {
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw ex, NullLogger<ExceptionHandlingMiddleware>.Instance);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        return context.Response.StatusCode;
    }

    [Fact]
    public async Task JuegoNoEncontrado_mapea_404()
        => Assert.Equal(StatusCodes.Status404NotFound, await StatusDe(new JuegoNoEncontradoException(Guid.NewGuid())));

    [Fact]
    public async Task MarcadorNoEncontrado_mapea_404()
        => Assert.Equal(StatusCodes.Status404NotFound, await StatusDe(new MarcadorNoEncontradoException(Guid.NewGuid(), Guid.NewGuid())));

    [Fact]
    public async Task Excepcion_generica_mapea_500()
        => Assert.Equal(StatusCodes.Status500InternalServerError, await StatusDe(new InvalidOperationException("x")));

    [Fact]
    public async Task PartidaNoEncontrada_mapea_404()
        => Assert.Equal(StatusCodes.Status404NotFound,
            await StatusDe(new PartidaNoEncontradaException(Guid.NewGuid())));

    [Fact]
    public async Task PartidaNoTerminada_mapea_409()
        => Assert.Equal(StatusCodes.Status409Conflict,
            await StatusDe(new PartidaNoTerminadaException(Guid.NewGuid(), EstadoPartidaProyectada.Iniciada)));

    [Fact]
    public async Task ArgumentException_mapea_400_y_emite_warning_con_path()
    {
        var logger = new RecordingLogger();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new ArgumentException("limit debe estar entre 1 y 500."), logger);
        var context = new DefaultHttpContext();
        context.Request.Path = "/puntuaciones/partidas/x/historial";

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        var entrada = Assert.Single(logger.Entradas);
        Assert.Equal(LogLevel.Warning, entrada.Nivel);
        Assert.Contains("limit debe estar entre 1 y 500.", entrada.Mensaje);
        Assert.Contains("/puntuaciones/partidas/x/historial", entrada.Mensaje);
    }

    [Fact]
    public async Task NotFound_no_emite_warning()
    {
        var logger = new RecordingLogger();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new PartidaNoEncontradaException(Guid.NewGuid()), logger);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Empty(logger.Entradas);
    }
}
