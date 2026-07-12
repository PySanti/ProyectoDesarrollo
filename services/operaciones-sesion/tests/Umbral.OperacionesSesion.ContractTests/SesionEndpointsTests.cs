using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Umbral.OperacionesSesion.Application.DTOs;

namespace Umbral.OperacionesSesion.ContractTests;

public class SesionEndpointsTests : IClassFixture<OperacionesSesionWebFactory>
{
    private readonly OperacionesSesionWebFactory _factory;
    private readonly HttpClient _client;

    public SesionEndpointsTests(OperacionesSesionWebFactory factory)
    {
        _factory = factory;
        // Cliente por defecto autenticado (identidad de test arbitraria): con el fallback
        // fail-secure de Program.cs, todo endpoint sin [AllowAnonymous] exige un usuario
        // autenticado, aunque este task todavía no aplica [Authorize] por policy a los
        // controllers (eso es la tarea 4). Los tests que verifican comportamiento
        // genuinamente anónimo crean su propio cliente sin X-Test-Sub explícitamente.
        _client = factory.CreateClientAs(Guid.NewGuid());
    }

    private static ConfiguracionPartidaDto Config(string modalidad = "Individual", int max = 10, int juegos = 1) =>
        new("Copa", modalidad, "Manual", null, 1, max,
            BuildJuegos(juegos));

    private static List<JuegoResumenDto> BuildJuegos(int juegos)
    {
        var list = new List<JuegoResumenDto>();
        for (var o = 1; o <= juegos; o++) list.Add(new JuegoResumenDto(Guid.NewGuid(), o, "Trivia"));
        return list;
    }

    [Fact]
    public async Task Publish_then_lobby_then_inscribe_flow()
    {
        var partidaId = Guid.NewGuid();
        _factory.Stub.Respuestas[partidaId] = Config(juegos: 2);

        var publish = await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/publicacion", null);
        Assert.Equal(HttpStatusCode.Created, publish.StatusCode);
        Assert.NotNull(publish.Headers.Location);
        var lobby = await publish.Content.ReadFromJsonAsync<LobbyDto>();
        Assert.Equal("Lobby", lobby!.Estado);
        Assert.Equal(0, lobby.InscritosActivos);

        var getLobby = await _client.GetFromJsonAsync<LobbyDto>($"{Rutas.Base}/partidas/{partidaId}/lobby");
        Assert.Equal("Individual", getLobby!.Modalidad);
    }

    [Fact]
    public async Task Publish_unknown_partida_returns_404()
    {
        var partidaId = Guid.NewGuid();
        _factory.Stub.Respuestas[partidaId] = null; // explicit 404 from Partidas

        var publish = await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/publicacion", null);
        Assert.Equal(HttpStatusCode.NotFound, publish.StatusCode);
    }

    [Fact]
    public async Task Double_publish_returns_409()
    {
        var partidaId = Guid.NewGuid();
        _factory.Stub.Respuestas[partidaId] = Config();

        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/publicacion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/publicacion", null)).StatusCode);
    }

    [Fact]
    public async Task Inscribe_without_identity_returns_401()
    {
        var partidaId = Guid.NewGuid();
        _factory.Stub.Respuestas[partidaId] = Config(modalidad: "Equipo");
        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/publicacion", null)).StatusCode);

        // Cliente sin X-Test-Sub (anónimo): el fallback fail-secure exige usuario autenticado → 401.
        var anonimo = _factory.CreateClient();
        var inscribe = await anonimo.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inscripciones", null);
        Assert.Equal(HttpStatusCode.Unauthorized, inscribe.StatusCode);
    }

    [Fact]
    public async Task Inscribe_unpublished_without_identity_returns_401()
    {
        // No session published for this id; cliente sin X-Test-Sub (anónimo) → el fallback
        // fail-secure rechaza la petición antes de llegar al controller (401).
        var anonimo = _factory.CreateClient();
        var inscribe = await anonimo.PostAsync($"{Rutas.Base}/partidas/{Guid.NewGuid()}/inscripciones", null);
        Assert.Equal(HttpStatusCode.Unauthorized, inscribe.StatusCode);
    }

    [Fact]
    public async Task Get_lobby_for_unknown_partida_returns_404()
    {
        // GET lobby for a partida that was never published exercises SesionNoEncontradaException → 404.
        // No participant-id header is required for this query endpoint.
        var response = await _client.GetAsync($"{Rutas.Base}/partidas/{Guid.NewGuid()}/lobby");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Inscribe_authenticated_returns_201_and_appears_in_lobby()
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        _factory.Stub.Respuestas[partidaId] = Config(modalidad: "Individual", max: 10);

        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/publicacion", null)).StatusCode);

        var authClient = _factory.CreateClientAs(participanteId);
        var inscribe = await authClient.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inscripciones", null);
        Assert.Equal(HttpStatusCode.Created, inscribe.StatusCode);
        await AceptarInscripcion(partidaId, inscribe);

        var lobby = await _client.GetFromJsonAsync<LobbyDto>($"{Rutas.Base}/partidas/{partidaId}/lobby");
        Assert.Equal(1, lobby!.InscritosActivos);
        Assert.Contains(participanteId, lobby.Participantes);
    }

    [Fact]
    public async Task Inscribe_duplicate_returns_409()
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        _factory.Stub.Respuestas[partidaId] = Config(modalidad: "Individual", max: 10);

        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/publicacion", null)).StatusCode);

        var authClient = _factory.CreateClientAs(participanteId);
        Assert.Equal(HttpStatusCode.Created, (await authClient.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inscripciones", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await authClient.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inscripciones", null)).StatusCode);
    }

    [Fact]
    public async Task Inscribe_into_equipo_partida_returns_409()
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        _factory.Stub.Respuestas[partidaId] = Config(modalidad: "Equipo", max: 10);

        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/publicacion", null)).StatusCode);

        var authClient = _factory.CreateClientAs(participanteId);
        var inscribe = await authClient.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inscripciones", null);
        Assert.Equal(HttpStatusCode.Conflict, inscribe.StatusCode);
    }

    [Fact]
    public async Task Inscribe_full_capacity_returns_409()
    {
        var partidaId = Guid.NewGuid();
        var participanteA = Guid.NewGuid();
        var participanteB = Guid.NewGuid();
        _factory.Stub.Respuestas[partidaId] = Config(modalidad: "Individual", max: 1);

        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/publicacion", null)).StatusCode);

        var clientA = _factory.CreateClientAs(participanteA);
        var clientB = _factory.CreateClientAs(participanteB);

        // HU-19: el cupo sólo lo consumen las inscripciones ACTIVAS. A debe ser aceptada por el
        // operador para llenar el cupo (max=1); recién entonces B choca con CupoLleno → 409.
        var inscribeA = await clientA.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inscripciones", null);
        Assert.Equal(HttpStatusCode.Created, inscribeA.StatusCode);
        await AceptarInscripcion(partidaId, inscribeA);
        Assert.Equal(HttpStatusCode.Conflict, (await clientB.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inscripciones", null)).StatusCode);
    }

    [Fact]
    public async Task Cancel_own_inscription_returns_204()
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        _factory.Stub.Respuestas[partidaId] = Config(modalidad: "Individual", max: 10);

        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/publicacion", null)).StatusCode);

        var authClient = _factory.CreateClientAs(participanteId);
        var inscribe = await authClient.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inscripciones", null);
        Assert.Equal(HttpStatusCode.Created, inscribe.StatusCode);
        await AceptarInscripcion(partidaId, inscribe);

        var cancel = await authClient.DeleteAsync($"{Rutas.Base}/partidas/{partidaId}/inscripciones/mia");
        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);

        var lobby = await _client.GetFromJsonAsync<LobbyDto>($"{Rutas.Base}/partidas/{partidaId}/lobby");
        Assert.Equal(0, lobby!.InscritosActivos);
    }

    private async Task PublishAndInscribe(Guid partidaId, Guid participanteId, int juegos)
    {
        _factory.Stub.Respuestas[partidaId] = Config(modalidad: "Individual", max: 10, juegos: juegos);
        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/publicacion", null)).StatusCode);
        var authClient = _factory.CreateClientAs(participanteId);
        var inscribe = await authClient.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inscripciones", null);
        Assert.Equal(HttpStatusCode.Created, inscribe.StatusCode);
        await AceptarInscripcion(partidaId, inscribe);
    }

    // HU-19: una inscripción nace Pendiente; el operador debe aceptarla para que cuente
    // como activa (mínimos, cupo, inicio). Helper que lee el InscripcionResponse del
    // POST /inscripciones y despacha la aceptación con el cliente operador (_client).
    private async Task AceptarInscripcion(Guid partidaId, HttpResponseMessage inscribeResponse)
    {
        var inscripcion = await inscribeResponse.Content.ReadFromJsonAsync<InscripcionResponse>();
        var aceptar = await _client.PostAsync(
            $"{Rutas.Base}/partidas/{partidaId}/inscripciones/{inscripcion!.InscripcionId}/aceptacion", null);
        Assert.Equal(HttpStatusCode.OK, aceptar.StatusCode);
    }

    [Fact]
    public async Task Start_then_advance_runs_full_lifecycle_to_terminada()
    {
        var partidaId = Guid.NewGuid();
        await PublishAndInscribe(partidaId, Guid.NewGuid(), juegos: 2);

        var start = await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inicio", null);
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        var inicio = await start.Content.ReadFromJsonAsync<InicioPartidaResponse>();
        Assert.Equal("Iniciada", inicio!.Estado);
        Assert.Equal(1, inicio.JuegoActivadoOrden);

        var estado1 = await _client.GetFromJsonAsync<EstadoSesionDto>($"{Rutas.Base}/partidas/{partidaId}/estado");
        Assert.Equal("Iniciada", estado1!.Estado);
        Assert.Equal(1, estado1.JuegoActualOrden);
        Assert.Equal("Activo", estado1.Juegos.Single(j => j.Orden == 1).Estado);

        var avance1 = await (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/juego-actual/finalizacion", null)).Content.ReadFromJsonAsync<AvanceJuegoResponse>();
        Assert.False(avance1!.Terminada);
        Assert.Equal(2, avance1.JuegoActivadoOrden);

        var avance2 = await (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/juego-actual/finalizacion", null)).Content.ReadFromJsonAsync<AvanceJuegoResponse>();
        Assert.True(avance2!.Terminada);
        Assert.Equal("Terminada", avance2.Estado);

        var estadoFinal = await _client.GetFromJsonAsync<EstadoSesionDto>($"{Rutas.Base}/partidas/{partidaId}/estado");
        Assert.Equal("Terminada", estadoFinal!.Estado);
        Assert.Null(estadoFinal.JuegoActualOrden);
    }

    [Fact]
    public async Task Start_with_minimums_not_met_auto_cancels()
    {
        var partidaId = Guid.NewGuid();
        // min defaults to 1 in Config; publish a partida with min=2 by overriding the stub config.
        _factory.Stub.Respuestas[partidaId] = new ConfiguracionPartidaDto("Copa", "Individual", "Manual", null, 2, 10,
            new List<JuegoResumenDto> { new(Guid.NewGuid(), 1, "Trivia") });
        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/publicacion", null)).StatusCode);
        // no inscriptions → 0 < 2

        var start = await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inicio", null);
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        var inicio = await start.Content.ReadFromJsonAsync<InicioPartidaResponse>();
        Assert.Equal("Cancelada", inicio!.Estado);
    }

    [Fact]
    public async Task Start_when_not_in_lobby_returns_409()
    {
        var partidaId = Guid.NewGuid();
        await PublishAndInscribe(partidaId, Guid.NewGuid(), juegos: 1);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inicio", null)).StatusCode);

        var second = await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inicio", null);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode); // already Iniciada → SesionNoEnLobby
    }

    [Fact]
    public async Task Automatic_start_not_due_is_noop_lobby()
    {
        var partidaId = Guid.NewGuid();
        // Automatic mode, TiempoInicio in the far future → not due.
        _factory.Stub.Respuestas[partidaId] = new ConfiguracionPartidaDto("Copa", "Individual", "Automatico",
            new DateTime(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc), 1, 10,
            new List<JuegoResumenDto> { new(Guid.NewGuid(), 1, "Trivia") });
        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/publicacion", null)).StatusCode);

        var auto = await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inicio-automatico", null);
        Assert.Equal(HttpStatusCode.OK, auto.StatusCode);
        var inicio = await auto.Content.ReadFromJsonAsync<InicioPartidaResponse>();
        Assert.Equal("Lobby", inicio!.Estado); // not due → no-op
    }

    [Fact]
    public async Task Finalizar_when_not_iniciada_returns_409()
    {
        var partidaId = Guid.NewGuid();
        _factory.Stub.Respuestas[partidaId] = Config(juegos: 1);
        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/publicacion", null)).StatusCode);
        // still in Lobby (never started)

        var finalizar = await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/juego-actual/finalizacion", null);
        Assert.Equal(HttpStatusCode.Conflict, finalizar.StatusCode);
    }

    [Fact]
    public async Task Estado_for_unknown_partida_returns_404()
    {
        var response = await _client.GetAsync($"{Rutas.Base}/partidas/{Guid.NewGuid()}/estado");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Politicas_de_permisos_funcionales_estan_registradas()
    {
        using var scope = _factory.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IAuthorizationPolicyProvider>();

        var gestionar = await provider.GetPolicyAsync("GestionarPartidas");
        var participar = await provider.GetPolicyAsync("ParticiparEnPartidas");

        Assert.NotNull(gestionar);
        Assert.Contains(gestionar!.Requirements, r =>
            r is RolesAuthorizationRequirement roles && roles.AllowedRoles.Contains("GestionarPartidas"));
        Assert.NotNull(participar);
        Assert.Contains(participar!.Requirements, r =>
            r is RolesAuthorizationRequirement roles && roles.AllowedRoles.Contains("ParticiparEnPartidas"));
    }

    [Fact]
    public async Task Fallback_policy_es_fail_secure()
    {
        using var scope = _factory.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IAuthorizationPolicyProvider>();

        var fallback = await provider.GetFallbackPolicyAsync();

        Assert.NotNull(fallback);
        Assert.Contains(fallback!.Requirements, r => r is DenyAnonymousAuthorizationRequirement);
    }
}
