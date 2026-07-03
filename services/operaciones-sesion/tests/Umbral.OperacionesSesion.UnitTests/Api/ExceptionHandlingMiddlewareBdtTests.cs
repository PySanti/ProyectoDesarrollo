using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Umbral.OperacionesSesion.Api.Middleware;
using Umbral.OperacionesSesion.Domain.Exceptions;

namespace Umbral.OperacionesSesion.UnitTests.Api;

public class ExceptionHandlingMiddlewareBdtTests
{
    private static async Task<int> StatusFor(Exception ex)
    {
        var middleware = new ExceptionHandlingMiddleware(_ => throw ex, NullLogger<ExceptionHandlingMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(context);
        return context.Response.StatusCode;
    }

    [Theory]
    [InlineData(typeof(JuegoActivoNoEsBDTException))]
    [InlineData(typeof(NoHayEtapaActivaException))]
    [InlineData(typeof(JuegoConEtapasPendientesException))]
    public async Task Bdt_conflicts_map_to_409(Type excType)
    {
        var ex = (Exception)Activator.CreateInstance(excType, Guid.NewGuid())!;
        var status = await StatusFor(ex);
        Assert.Equal((int)HttpStatusCode.Conflict, status);
    }
}
