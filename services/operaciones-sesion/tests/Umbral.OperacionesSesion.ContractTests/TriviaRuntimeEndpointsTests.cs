using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.DTOs;

namespace Umbral.OperacionesSesion.ContractTests;

/// <summary>
/// End-to-end contract tests for the Trivia runtime endpoints (SP-3c T15).
/// Covers RF-21 (answer validation) and RF-22 (correct answer auto-advances to next question).
/// Each test uses a fresh partidaId (Guid.NewGuid()) to avoid shared in-memory database collisions.
/// </summary>
public class TriviaRuntimeEndpointsTests : IClassFixture<OperacionesSesionWebFactory>
{
    private readonly OperacionesSesionWebFactory _factory;

    // Client authenticated with an arbitrary test identity – used for operator endpoints
    // (publicacion, inicio, finalizacion, avance). Program.cs' fail-secure fallback requires an
    // authenticated user on every endpoint without [AllowAnonymous]; operator-specific
    // role/policy restriction is applied by task 4.
    private readonly HttpClient _client;

    public TriviaRuntimeEndpointsTests(OperacionesSesionWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClientAs(Guid.NewGuid());
    }

    // Builds a ConfiguracionPartidaDto with one Trivia game whose questions are specified as
    // (PreguntaId, CorrectaOpcionId, PuntajeAsignado) tuples in sequential order.
    // minParticipacion = 1 so a single inscription satisfies the minimum.
    private static ConfiguracionPartidaDto BuildTriviaConfig(
        int minParticipacion,
        params (Guid PregId, Guid CorrectaId, int Puntaje)[] preguntas)
    {
        var juegoId = Guid.NewGuid();
        var preguntaConfigs = new List<PreguntaConfigDto>();
        for (var i = 0; i < preguntas.Length; i++)
        {
            var (pregId, correctaId, puntaje) = preguntas[i];
            preguntaConfigs.Add(new PreguntaConfigDto(
                pregId,
                $"Pregunta {i + 1}",
                puntaje,
                3600, // generous time limit so tests never expire
                new List<OpcionConfigDto>
                {
                    new(correctaId, "Opcion correcta", true),
                    new(Guid.NewGuid(), "Opcion incorrecta", false)
                }));
        }

        var juego = new JuegoResumenDto(juegoId, 1, "Trivia", new TriviaConfigDto(preguntaConfigs));
        return new ConfiguracionPartidaDto(
            "Copa", "Individual", "Manual", null, minParticipacion, 10,
            new List<JuegoResumenDto> { juego });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 1: Single question → correct answer → finalize → Terminada
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Single_question_flow_answer_correct_then_finalize_terminada()
    {
        var partidaId = Guid.NewGuid();
        var jugadorId = Guid.NewGuid();
        var pregId = Guid.NewGuid();
        var correctaId = Guid.NewGuid();

        _factory.Stub.Respuestas[partidaId] = BuildTriviaConfig(1, (pregId, correctaId, 10));
        var jugadorClient = _factory.CreateClientAs(jugadorId);

        // Publicar
        var publish = await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/publicacion", null);
        Assert.Equal(HttpStatusCode.Created, publish.StatusCode);

        // Inscribir jugador
        Assert.Equal(HttpStatusCode.Created,
            (await jugadorClient.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inscripciones", null)).StatusCode);

        // Iniciar partida
        var start = await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inicio", null);
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        var inicio = await start.Content.ReadFromJsonAsync<InicioPartidaResponse>();
        Assert.Equal("Iniciada", inicio!.Estado);
        Assert.Equal(1, inicio.JuegoActivadoOrden);

        // GET pregunta-actual: debe devolver Q1, con 2 opciones y SIN exponer la opción correcta
        var pregunta = await jugadorClient.GetFromJsonAsync<PreguntaActualDto>(
            $"{Rutas.Base}/partidas/{partidaId}/pregunta-actual");
        Assert.Equal(1, pregunta!.Orden);
        Assert.Equal(2, pregunta.Opciones.Count);
        // OpcionPublicaDto only exposes OpcionId + Texto – no EsCorrecta field on the DTO
        Assert.All(pregunta.Opciones, o => Assert.NotEqual(Guid.Empty, o.OpcionId));

        // Responder con la opción correcta (el jugador sabe el correctaId out-of-band desde la config)
        var responderResp = await jugadorClient.PostAsJsonAsync(
            $"{Rutas.Base}/partidas/{partidaId}/pregunta-actual/respuesta",
            new ResponderPreguntaRequest(correctaId));
        Assert.Equal(HttpStatusCode.OK, responderResp.StatusCode);
        var respuesta = await responderResp.Content.ReadFromJsonAsync<RespuestaTriviaResponse>();
        Assert.True(respuesta!.EsCorrecta);
        Assert.True(respuesta.CerroPregunta);
        Assert.Equal(10, respuesta.Puntaje);

        // Finalizar juego: Q1 cerrada, no hay preguntas abiertas → OK, partida Terminada
        // (NOT calling /pregunta-actual/avance – there is no active question after the correct answer)
        var finalizar = await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/juego-actual/finalizacion", null);
        Assert.Equal(HttpStatusCode.OK, finalizar.StatusCode);
        var avance = await finalizar.Content.ReadFromJsonAsync<AvanceJuegoResponse>();
        Assert.True(avance!.Terminada);
        Assert.Equal("Terminada", avance.Estado);

        // GET estado: confirma estado Terminada y ningún juego activo
        var estado = await _client.GetFromJsonAsync<EstadoSesionDto>($"{Rutas.Base}/partidas/{partidaId}/estado");
        Assert.Equal("Terminada", estado!.Estado);
        Assert.Null(estado.JuegoActualOrden);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 2: 2-question game → correct answer on Q1 → GET pregunta-actual returns Q2 (RF-22)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Multi_question_correct_answer_auto_advances_to_next()
    {
        var partidaId = Guid.NewGuid();
        var jugadorId = Guid.NewGuid();
        var preg1Id = Guid.NewGuid();
        var correcta1Id = Guid.NewGuid();
        var preg2Id = Guid.NewGuid();
        var correcta2Id = Guid.NewGuid();

        _factory.Stub.Respuestas[partidaId] = BuildTriviaConfig(1,
            (preg1Id, correcta1Id, 10),
            (preg2Id, correcta2Id, 20));
        var jugadorClient = _factory.CreateClientAs(jugadorId);

        // Publish → inscribe → inicio
        Assert.Equal(HttpStatusCode.Created,
            (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/publicacion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Created,
            (await jugadorClient.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inscripciones", null)).StatusCode);
        var start = await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inicio", null);
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);

        // GET pregunta-actual → Orden == 1
        var preg1 = await jugadorClient.GetFromJsonAsync<PreguntaActualDto>(
            $"{Rutas.Base}/partidas/{partidaId}/pregunta-actual");
        Assert.Equal(1, preg1!.Orden);

        // Responder Q1 correctamente → cierra Q1 y auto-activa Q2 (RF-22)
        var resp1 = await jugadorClient.PostAsJsonAsync(
            $"{Rutas.Base}/partidas/{partidaId}/pregunta-actual/respuesta",
            new ResponderPreguntaRequest(correcta1Id));
        Assert.Equal(HttpStatusCode.OK, resp1.StatusCode);
        var r1 = await resp1.Content.ReadFromJsonAsync<RespuestaTriviaResponse>();
        Assert.True(r1!.EsCorrecta);
        Assert.True(r1.CerroPregunta);

        // GET pregunta-actual → ahora debe devolver Q2 (auto-avance RF-22)
        var preg2 = await jugadorClient.GetFromJsonAsync<PreguntaActualDto>(
            $"{Rutas.Base}/partidas/{partidaId}/pregunta-actual");
        Assert.Equal(2, preg2!.Orden);

        // Responder Q2 correctamente
        var resp2 = await jugadorClient.PostAsJsonAsync(
            $"{Rutas.Base}/partidas/{partidaId}/pregunta-actual/respuesta",
            new ResponderPreguntaRequest(correcta2Id));
        Assert.Equal(HttpStatusCode.OK, resp2.StatusCode);
        var r2 = await resp2.Content.ReadFromJsonAsync<RespuestaTriviaResponse>();
        Assert.True(r2!.EsCorrecta);
        Assert.True(r2.CerroPregunta);

        // Finalizar → Terminada (no hay preguntas abiertas)
        var finalizar = await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/juego-actual/finalizacion", null);
        Assert.Equal(HttpStatusCode.OK, finalizar.StatusCode);
        var avance = await finalizar.Content.ReadFromJsonAsync<AvanceJuegoResponse>();
        Assert.True(avance!.Terminada);
        Assert.Equal("Terminada", avance.Estado);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 3: Operator advance skips open questions without any answer
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Operator_advance_skips_open_question()
    {
        var partidaId = Guid.NewGuid();
        var jugadorId = Guid.NewGuid();
        var preg1Id = Guid.NewGuid();
        var correcta1Id = Guid.NewGuid();

        // 2-question Trivia game; correct IDs are known but not used (operator skips both)
        _factory.Stub.Respuestas[partidaId] = BuildTriviaConfig(1,
            (preg1Id, correcta1Id, 10),
            (Guid.NewGuid(), Guid.NewGuid(), 10));
        var jugadorClient = _factory.CreateClientAs(jugadorId);

        // Publish → inscribe → inicio
        Assert.Equal(HttpStatusCode.Created,
            (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/publicacion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Created,
            (await jugadorClient.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inscripciones", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inicio", null)).StatusCode);

        // Operador avanza (salta Q1 sin respuesta)
        var avance1Resp = await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/pregunta-actual/avance", null);
        Assert.Equal(HttpStatusCode.OK, avance1Resp.StatusCode);
        var avance1 = await avance1Resp.Content.ReadFromJsonAsync<AvancePreguntaResponse>();
        Assert.False(avance1!.SinMasPreguntas);
        Assert.Equal(2, avance1.PreguntaActivadaOrden);

        // Operador avanza de nuevo (salta Q2 – última pregunta)
        var avance2Resp = await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/pregunta-actual/avance", null);
        Assert.Equal(HttpStatusCode.OK, avance2Resp.StatusCode);
        var avance2 = await avance2Resp.Content.ReadFromJsonAsync<AvancePreguntaResponse>();
        Assert.True(avance2!.SinMasPreguntas);
        Assert.Null(avance2.PreguntaActivadaOrden);

        // Finalizar → OK (todas las preguntas cerradas por avance del operador)
        var finalizar = await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/juego-actual/finalizacion", null);
        Assert.Equal(HttpStatusCode.OK, finalizar.StatusCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 4: Participant without active inscription → POST respuesta → 403
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Answer_without_inscription_returns_403()
    {
        var partidaId = Guid.NewGuid();
        var jugadorId = Guid.NewGuid(); // inscribed participant
        var intrusoId = Guid.NewGuid(); // NOT inscribed
        var pregId = Guid.NewGuid();
        var correctaId = Guid.NewGuid();

        _factory.Stub.Respuestas[partidaId] = BuildTriviaConfig(1, (pregId, correctaId, 10));
        var jugadorClient = _factory.CreateClientAs(jugadorId);
        var intrusoClient = _factory.CreateClientAs(intrusoId);

        // Publish → jugador inscribes (meets min=1) → inicio
        Assert.Equal(HttpStatusCode.Created,
            (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/publicacion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Created,
            (await jugadorClient.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inscripciones", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inicio", null)).StatusCode);

        // Intruso (valid identity but never inscribed) tries to answer → 403
        var response = await intrusoClient.PostAsJsonAsync(
            $"{Rutas.Base}/partidas/{partidaId}/pregunta-actual/respuesta",
            new ResponderPreguntaRequest(correctaId));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 5: Finalize while Trivia game has an open/active question → 409
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Finalize_with_open_question_returns_409()
    {
        var partidaId = Guid.NewGuid();
        var jugadorId = Guid.NewGuid();
        var pregId = Guid.NewGuid();
        var correctaId = Guid.NewGuid();

        _factory.Stub.Respuestas[partidaId] = BuildTriviaConfig(1, (pregId, correctaId, 10));
        var jugadorClient = _factory.CreateClientAs(jugadorId);

        // Publish → inscribe → inicio (Q1 becomes Activa)
        Assert.Equal(HttpStatusCode.Created,
            (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/publicacion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Created,
            (await jugadorClient.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inscripciones", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inicio", null)).StatusCode);

        // Attempt to finalize while Q1 is still active (unanswered) → JuegoConPreguntasPendientesException → 409
        var finalizar = await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/juego-actual/finalizacion", null);
        Assert.Equal(HttpStatusCode.Conflict, finalizar.StatusCode);
    }
}
