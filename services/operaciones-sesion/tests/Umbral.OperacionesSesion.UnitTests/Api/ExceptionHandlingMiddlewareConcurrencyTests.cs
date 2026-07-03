using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Umbral.OperacionesSesion.Api.Middleware;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Api;

public class ExceptionHandlingMiddlewareConcurrencyTests
{
    [Fact]
    public async Task DbUpdateConcurrencyException_se_mapea_a_409()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        var mw = new ExceptionHandlingMiddleware(
            _ => throw new DbUpdateConcurrencyException("conflict"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await mw.InvokeAsync(ctx);

        Assert.Equal(409, ctx.Response.StatusCode);
    }
}
