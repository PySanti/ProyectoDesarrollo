using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Umbral.Partidas.Api.Middleware;
using Umbral.Partidas.Application.Exceptions;
using Umbral.Partidas.Domain.Exceptions;

namespace Umbral.Partidas.UnitTests.Api;

public class ExceptionHandlingMiddlewareTests
{
    private static async Task<(int Status, string Body)> InvokeWith(Exception toThrow)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw toThrow,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        return (context.Response.StatusCode, body);
    }

    [Fact]
    public async Task Maps_partida_no_encontrada_to_404()
    {
        var ex = new PartidaNoEncontradaException(Guid.NewGuid());
        var (status, body) = await InvokeWith(ex);
        Assert.Equal(404, status);
        Assert.Contains("\"message\"", body);
        Assert.Contains(ex.Message, body);
    }

    [Fact]
    public async Task Maps_orden_duplicado_to_409()
    {
        var (status, _) = await InvokeWith(new OrdenJuegoDuplicadoException(1));
        Assert.Equal(409, status);
    }

    [Fact]
    public async Task Maps_juego_duplicado_to_409()
    {
        var (status, _) = await InvokeWith(new JuegoDuplicadoException(Guid.NewGuid()));
        Assert.Equal(409, status);
    }

    [Fact]
    public async Task Maps_pregunta_invalida_to_400()
    {
        var (status, _) = await InvokeWith(new PreguntaInvalidaException("x"));
        Assert.Equal(400, status);
    }

    [Fact]
    public async Task Maps_etapa_invalida_to_400()
    {
        var (status, _) = await InvokeWith(new EtapaBDTInvalidaException("x"));
        Assert.Equal(400, status);
    }

    [Fact]
    public async Task Maps_unknown_to_500()
    {
        var (status, body) = await InvokeWith(new InvalidOperationException("boom"));
        Assert.Equal(500, status);
        Assert.Contains("boom", body);
    }

    [Fact]
    public async Task Maps_juego_trivia_sin_preguntas_to_400() => Assert.Equal(400, (await InvokeWith(new JuegoTriviaSinPreguntasException())).Status);

    [Fact]
    public async Task Maps_juego_bdt_sin_etapas_to_400() => Assert.Equal(400, (await InvokeWith(new JuegoBDTSinEtapasException())).Status);

    [Fact]
    public async Task Maps_area_busqueda_requerida_to_400() => Assert.Equal(400, (await InvokeWith(new AreaBusquedaRequeridaException())).Status);

    [Fact]
    public async Task Maps_partida_sin_juegos_to_400() => Assert.Equal(400, (await InvokeWith(new PartidaSinJuegosException(Guid.NewGuid()))).Status);

    [Fact]
    public async Task Maps_orden_no_contiguo_to_400() => Assert.Equal(400, (await InvokeWith(new OrdenJuegosNoContiguoException(Guid.NewGuid()))).Status);

    [Fact]
    public async Task Maps_argument_exception_to_400() => Assert.Equal(400, (await InvokeWith(new ArgumentException("x"))).Status);

    private sealed class CountingLogger<T> : ILogger<T>
    {
        public int LogCount { get; private set; }
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => LogCount++;
        private sealed class NullScope : IDisposable { public static readonly NullScope Instance = new(); public void Dispose() { } }
    }

    private static async Task<int> LogCountFor(Exception toThrow)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var logger = new CountingLogger<ExceptionHandlingMiddleware>();
        var middleware = new ExceptionHandlingMiddleware(_ => throw toThrow, logger);
        await middleware.InvokeAsync(context);
        return logger.LogCount;
    }

    [Fact]
    public async Task Does_not_log_on_4xx()
    {
        Assert.Equal(0, await LogCountFor(new PartidaNoEncontradaException(Guid.NewGuid())));
        Assert.Equal(0, await LogCountFor(new OrdenJuegoDuplicadoException(1)));
    }

    [Fact]
    public async Task Logs_on_500()
    {
        Assert.Equal(1, await LogCountFor(new InvalidOperationException("boom")));
    }
}
