using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Infrastructure.Services;

namespace Umbral.OperacionesSesion.UnitTests.Infrastructure;

public class PartidasConfigHttpClientTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string? _json;
        private readonly bool _throw;
        private readonly bool _throwTimeout;
        public string? AuthorizationSent { get; private set; }

        public StubHandler(HttpStatusCode status, string? json = null, bool throwNetwork = false, bool throwTimeout = false)
        {
            _status = status; _json = json; _throw = throwNetwork; _throwTimeout = throwTimeout;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            AuthorizationSent = request.Headers.Contains("Authorization")
                ? string.Join("", request.Headers.GetValues("Authorization")) : null;
            if (_throw) throw new HttpRequestException("boom");
            if (_throwTimeout) throw new TaskCanceledException("timeout");
            var response = new HttpResponseMessage(_status);
            if (_json is not null) response.Content = new StringContent(_json, System.Text.Encoding.UTF8, "application/json");
            return Task.FromResult(response);
        }
    }

    private static PartidasConfigHttpClient ClientWith(StubHandler handler)
        => new(new HttpClient(handler) { BaseAddress = new Uri("http://partidas.local") });

    private const string ValidJson = """
    {
      "partidaId": "11111111-1111-1111-1111-111111111111",
      "nombrePartida": "Copa",
      "modalidad": "Individual",
      "modoInicioPartida": "Manual",
      "tiempoInicio": null,
      "minimosParticipacion": 1,
      "maximosParticipacion": 10,
      "estado": null,
      "juegos": [
        { "juegoId": "22222222-2222-2222-2222-222222222222", "orden": 1, "tipoJuego": "Trivia", "estado": "Pendiente", "trivia": null, "bdt": null }
      ]
    }
    """;

    [Fact]
    public async Task Maps_200_payload_to_snapshot_dto_and_forwards_bearer()
    {
        var handler = new StubHandler(HttpStatusCode.OK, ValidJson);
        var dto = await ClientWith(handler).ObtenerConfiguracionAsync(Guid.NewGuid(), "Bearer tok", CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal("Copa", dto!.Nombre);
        Assert.Equal("Individual", dto.Modalidad);
        Assert.Single(dto.Juegos);
        Assert.Equal("Trivia", dto.Juegos[0].TipoJuego);
        Assert.Equal("Bearer tok", handler.AuthorizationSent);
    }

    [Fact]
    public async Task Returns_null_on_404()
    {
        var dto = await ClientWith(new StubHandler(HttpStatusCode.NotFound))
            .ObtenerConfiguracionAsync(Guid.NewGuid(), null, CancellationToken.None);
        Assert.Null(dto);
    }

    [Fact]
    public async Task Throws_inaccesible_on_500()
    {
        await Assert.ThrowsAsync<PartidasConfigInaccesibleException>(
            () => ClientWith(new StubHandler(HttpStatusCode.InternalServerError))
                .ObtenerConfiguracionAsync(Guid.NewGuid(), null, CancellationToken.None));
    }

    [Fact]
    public async Task Throws_inaccesible_on_network_failure()
    {
        await Assert.ThrowsAsync<PartidasConfigInaccesibleException>(
            () => ClientWith(new StubHandler(HttpStatusCode.OK, throwNetwork: true))
                .ObtenerConfiguracionAsync(Guid.NewGuid(), null, CancellationToken.None));
    }

    [Fact]
    public async Task Throws_inaccesible_on_malformed_json()
    {
        await Assert.ThrowsAsync<PartidasConfigInaccesibleException>(
            () => ClientWith(new StubHandler(HttpStatusCode.OK, "{ this is not valid json"))
                .ObtenerConfiguracionAsync(Guid.NewGuid(), null, CancellationToken.None));
    }

    [Fact]
    public async Task Throws_inaccesible_on_timeout()
    {
        await Assert.ThrowsAsync<PartidasConfigInaccesibleException>(
            () => ClientWith(new StubHandler(HttpStatusCode.OK, throwTimeout: true))
                .ObtenerConfiguracionAsync(Guid.NewGuid(), null, CancellationToken.None));
    }
}
