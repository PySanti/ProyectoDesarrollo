using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.DTOs;

namespace Umbral.OperacionesSesion.ContractTests;

/// <summary>
/// Contract tests end-to-end del lifecycle BDT (SP-3d T16).
/// Ejercen el pipeline HTTP real a través de OperacionesSesionWebFactory con el stub IQrDecoder.
/// Cada test usa un partidaId fresco (Guid.NewGuid()) para evitar colisiones en la base in-memory.
/// </summary>
public class BdtRuntimeEndpointsTests : IClassFixture<OperacionesSesionWebFactory>
{
    private readonly OperacionesSesionWebFactory _factory;

    // Cliente sin X-Test-Sub: usado para endpoints de operador (publicacion, inicio, avance, finalizacion)
    // que no requieren identity de participante.
    private readonly HttpClient _client;

    public BdtRuntimeEndpointsTests(OperacionesSesionWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // Codifica el texto del QR como base64 de sus bytes UTF-8 — lo mismo que haría la app móvil
    // al enviar la imagen del QR (el stub ContractTestQrDecoder los decodifica de vuelta a string).
    private static string Tesoro(string texto) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(texto));

    // Crea una ConfiguracionPartidaDto BDT Individual con las etapas especificadas.
    // minParticipacion = 1 para que una sola inscripción satisfaga el mínimo.
    private static ConfiguracionPartidaDto BuildBdtConfig(
        int minParticipacion, params (string Qr, int Puntaje)[] etapas)
    {
        var juegoId = Guid.NewGuid();
        var etapaConfigs = new List<EtapaConfigDto>();
        for (var i = 0; i < etapas.Length; i++)
            etapaConfigs.Add(new EtapaConfigDto(Guid.NewGuid(), i + 1, etapas[i].Qr, etapas[i].Puntaje, 3600));

        var juego = new JuegoResumenDto(juegoId, 1, "BusquedaDelTesoro",
            Trivia: null,
            Bdt: new BdtConfigDto("Plaza central", etapaConfigs));

        return new ConfiguracionPartidaDto(
            "Copa", "Individual", "Manual", null, minParticipacion, 10,
            new List<JuegoResumenDto> { juego });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 1: validar gana → auto-avance → validar etapa 2 → finalizar → Terminada
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Lifecycle_validate_wins_auto_advances_finalize_terminada()
    {
        var partidaId = Guid.NewGuid();
        var jugador = Guid.NewGuid();
        _factory.Stub.Respuestas[partidaId] = BuildBdtConfig(1, ("QR-1", 50), ("QR-2", 70));
        var jugadorClient = _factory.CreateClientAs(jugador);

        Assert.Equal(HttpStatusCode.Created,
            (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/publicacion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Created,
            (await jugadorClient.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inscripciones", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inicio", null)).StatusCode);

        // GET etapa-actual → Orden 1 (sin exponer el QR esperado)
        var etapa1 = await jugadorClient.GetFromJsonAsync<EtapaActualDto>($"{Rutas.Base}/partidas/{partidaId}/etapa-actual");
        Assert.Equal(1, etapa1!.Orden);

        // Validar tesoro correcto → gana + auto-avance al dominio activa etapa 2
        var val1 = await jugadorClient.PostAsJsonAsync(
            $"{Rutas.Base}/partidas/{partidaId}/etapa-actual/tesoro",
            new ValidarTesoroRequest(Tesoro("QR-1")));
        Assert.Equal(HttpStatusCode.OK, val1.StatusCode);
        var r1 = await val1.Content.ReadFromJsonAsync<ValidacionTesoroResponse>();
        Assert.True(r1!.Gano);
        Assert.Equal(50, r1.Puntaje);

        // GET etapa-actual → ahora Orden 2 (auto-avance)
        var etapa2 = await jugadorClient.GetFromJsonAsync<EtapaActualDto>($"{Rutas.Base}/partidas/{partidaId}/etapa-actual");
        Assert.Equal(2, etapa2!.Orden);

        // Validar etapa 2 correcto
        var val2 = await jugadorClient.PostAsJsonAsync(
            $"{Rutas.Base}/partidas/{partidaId}/etapa-actual/tesoro",
            new ValidarTesoroRequest(Tesoro("QR-2")));
        Assert.Equal(HttpStatusCode.OK, val2.StatusCode);

        // Finalizar → Terminada (no quedan etapas abiertas)
        var fin = await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/juego-actual/finalizacion", null);
        Assert.Equal(HttpStatusCode.OK, fin.StatusCode);
        var avance = await fin.Content.ReadFromJsonAsync<AvanceJuegoResponse>();
        Assert.True(avance!.Terminada);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 2: tesoro inválido registra sin ganar (Resultado "Invalido", Gano false)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Invalid_treasure_registers_without_winning()
    {
        var partidaId = Guid.NewGuid();
        var jugador = Guid.NewGuid();
        _factory.Stub.Respuestas[partidaId] = BuildBdtConfig(1, ("QR-1", 50));
        var jugadorClient = _factory.CreateClientAs(jugador);

        Assert.Equal(HttpStatusCode.Created,
            (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/publicacion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Created,
            (await jugadorClient.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inscripciones", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inicio", null)).StatusCode);

        var val = await jugadorClient.PostAsJsonAsync(
            $"{Rutas.Base}/partidas/{partidaId}/etapa-actual/tesoro",
            new ValidarTesoroRequest(Tesoro("QR-EQUIVOCADO")));
        Assert.Equal(HttpStatusCode.OK, val.StatusCode);
        var r = await val.Content.ReadFromJsonAsync<ValidacionTesoroResponse>();
        Assert.False(r!.Gano);
        Assert.Equal("Invalido", r.Resultado);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 3: avance operador salta etapa abierta (SinMasEtapas false → true)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Operator_advance_skips_open_stage()
    {
        var partidaId = Guid.NewGuid();
        var jugador = Guid.NewGuid();
        _factory.Stub.Respuestas[partidaId] = BuildBdtConfig(1, ("QR-1", 50), ("QR-2", 70));
        var jugadorClient = _factory.CreateClientAs(jugador);

        Assert.Equal(HttpStatusCode.Created,
            (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/publicacion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Created,
            (await jugadorClient.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inscripciones", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inicio", null)).StatusCode);

        // Primer avance: salta etapa 1 (activa), activa etapa 2
        var av1Resp = await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/etapa-actual/avance", null);
        Assert.Equal(HttpStatusCode.OK, av1Resp.StatusCode);
        var a1 = await av1Resp.Content.ReadFromJsonAsync<AvanceEtapaResponse>();
        Assert.False(a1!.SinMasEtapas);
        Assert.Equal(2, a1.EtapaActivadaOrden);

        // Segundo avance: salta etapa 2 (última) → sin más etapas
        var av2Resp = await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/etapa-actual/avance", null);
        Assert.Equal(HttpStatusCode.OK, av2Resp.StatusCode);
        var a2 = await av2Resp.Content.ReadFromJsonAsync<AvanceEtapaResponse>();
        Assert.True(a2!.SinMasEtapas);

        // Finalizar → OK (todas las etapas cerradas por avance operador)
        Assert.Equal(HttpStatusCode.OK,
            (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/juego-actual/finalizacion", null)).StatusCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 4: validar sin inscripción → 403
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_without_inscription_returns_403()
    {
        var partidaId = Guid.NewGuid();
        var jugador = Guid.NewGuid();
        var intruso = Guid.NewGuid();
        _factory.Stub.Respuestas[partidaId] = BuildBdtConfig(1, ("QR-1", 50));
        var jugadorClient = _factory.CreateClientAs(jugador);
        var intrusoClient = _factory.CreateClientAs(intruso);

        Assert.Equal(HttpStatusCode.Created,
            (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/publicacion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Created,
            (await jugadorClient.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inscripciones", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inicio", null)).StatusCode);

        // Intruso autenticado pero sin inscripción → ParticipanteNoInscritoException → 403
        var resp = await intrusoClient.PostAsJsonAsync(
            $"{Rutas.Base}/partidas/{partidaId}/etapa-actual/tesoro",
            new ValidarTesoroRequest(Tesoro("QR-1")));
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 5: finalizar con etapa abierta → 409
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Finalize_with_open_stage_returns_409()
    {
        var partidaId = Guid.NewGuid();
        var jugador = Guid.NewGuid();
        _factory.Stub.Respuestas[partidaId] = BuildBdtConfig(1, ("QR-1", 50));
        var jugadorClient = _factory.CreateClientAs(jugador);

        Assert.Equal(HttpStatusCode.Created,
            (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/publicacion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Created,
            (await jugadorClient.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inscripciones", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inicio", null)).StatusCode);

        // Intentar finalizar con etapa activa (no cerrada) → JuegoConEtapasPendientesException → 409
        var fin = await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/juego-actual/finalizacion", null);
        Assert.Equal(HttpStatusCode.Conflict, fin.StatusCode);
    }
}
