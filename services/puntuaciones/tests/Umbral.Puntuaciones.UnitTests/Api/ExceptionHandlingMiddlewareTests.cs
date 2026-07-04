using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Umbral.Puntuaciones.Api.Middleware;
using Umbral.Puntuaciones.Application.Exceptions;

namespace Umbral.Puntuaciones.UnitTests.Api;

public class ExceptionHandlingMiddlewareTests
{
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
}
