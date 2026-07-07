using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Infrastructure.Services;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Infrastructure;

public class IdentityEquipoHttpClientTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;
        public string? CapturedPath { get; private set; }
        public StubHandler(HttpStatusCode status, string body = "") { _status = status; _body = body; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedPath = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(_status)
            { Content = new StringContent(_body, Encoding.UTF8, "application/json") });
        }
    }

    private static (IdentityEquipoHttpClient, StubHandler) BuildWithHandler(HttpStatusCode status, string body = "")
    {
        var handler = new StubHandler(status, body);
        var client = new IdentityEquipoHttpClient(new HttpClient(handler) { BaseAddress = new Uri("http://identity.test") });
        return (client, handler);
    }

    private static IdentityEquipoHttpClient Build(HttpStatusCode status, string body = "")
        => BuildWithHandler(status, body).Item1;

    [Fact]
    public async Task Ok_mapea_snapshot()
    {
        var lider = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var body = $$"""
        {"equipoId":"{{equipoId}}","nombreEquipo":"Halcones","estado":"Activo",
         "participantes":[{"usuarioId":"{{lider}}","esLider":true}]}
        """;
        var (client, handler) = BuildWithHandler(HttpStatusCode.OK, body);

        var r = await client.ObtenerMiEquipoAsync("Bearer x", CancellationToken.None);

        Assert.NotNull(r);
        Assert.Equal(equipoId, r!.EquipoId);
        Assert.Equal("Halcones", r.NombreEquipo);
        Assert.True(r.Miembros[0].EsLider);
        Assert.Equal(lider, r.Miembros[0].UsuarioId);
        Assert.Equal("/identity/teams/mine", handler.CapturedPath);
    }

    [Fact]
    public async Task NotFound_devuelve_null()
    {
        var client = Build(HttpStatusCode.NotFound);
        Assert.Null(await client.ObtenerMiEquipoAsync("Bearer x", CancellationToken.None));
    }

    [Fact]
    public async Task ServerError_lanza_identity_inaccesible()
    {
        var client = Build(HttpStatusCode.InternalServerError);
        await Assert.ThrowsAsync<IdentityInaccesibleException>(
            () => client.ObtenerMiEquipoAsync("Bearer x", CancellationToken.None));
    }
}
