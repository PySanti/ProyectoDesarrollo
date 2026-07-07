using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.DTOs;
using Xunit;

namespace Umbral.OperacionesSesion.ContractTests;

/// <summary>
/// Contract tests e2e de reconexión (SP-3e T4).
/// Verifican que GET /mi-sesion: retorna 204 sin participación activa,
/// recupera estado en Lobby, recupera etapa BDT activa sin filtrar el QR esperado,
/// y recupera pregunta Trivia activa sin filtrar la opción correcta.
/// Cada test usa partidaId y jugador frescos para evitar colisiones en la BD in-memory.
/// </summary>
public class ReconexionEndpointsTests : IClassFixture<OperacionesSesionWebFactory>
{
    private readonly OperacionesSesionWebFactory _factory;
    private readonly HttpClient _operador;

    // Opciones de deserialización case-insensitive (JSON de ASP.NET Core es camelCase).
    private static readonly JsonSerializerOptions _jsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public ReconexionEndpointsTests(OperacionesSesionWebFactory factory)
    {
        _factory = factory;
        // Autenticado con identidad de test arbitraria: el fallback fail-secure de Program.cs
        // exige usuario autenticado en todo endpoint sin [AllowAnonymous] (la restricción por
        // rol/policy de operador la aplica la tarea 4).
        _operador = factory.CreateClientAs(Guid.NewGuid());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 1: sin participación activa → 204
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Sin_participacion_devuelve_204()
    {
        var cliente = _factory.CreateClientAs(Guid.NewGuid());
        var resp = await cliente.GetAsync($"{Rutas.Base}/mi-sesion");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 2: inscrito pero partida NO iniciada → devuelve estado Lobby sin JuegoActivo
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Reconexion_en_lobby_devuelve_estado_lobby()
    {
        var partidaId = Guid.NewGuid();
        var jugador = Guid.NewGuid();
        var jugadorClient = _factory.CreateClientAs(jugador);

        Assert.Equal(HttpStatusCode.Created,
            (await _operador.PostAsync($"{Rutas.Base}/partidas/{partidaId}/publicacion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Created,
            (await jugadorClient.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inscripciones", null)).StatusCode);
        // NO iniciar → la partida sigue en Lobby

        var dto = await jugadorClient.GetFromJsonAsync<MiSesionDto>($"{Rutas.Base}/mi-sesion", _jsonOpts);
        Assert.NotNull(dto);
        Assert.Equal(partidaId, dto!.PartidaId);
        Assert.Equal("Lobby", dto.EstadoPartida);
        Assert.Null(dto.JuegoActivo);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 3: partida BDT iniciada → recupera etapa activa + NO-LEAK del QR esperado
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Reconexion_recupera_etapa_bdt_activa_sin_filtrar_qr()
    {
        var partidaId = Guid.NewGuid();
        var jugador = Guid.NewGuid();
        _factory.Stub.Respuestas[partidaId] = BdtConfig("QR-SECRETO", 50);
        var jugadorClient = _factory.CreateClientAs(jugador);

        await _operador.PostAsync($"{Rutas.Base}/partidas/{partidaId}/publicacion", null);
        await jugadorClient.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inscripciones", null);
        await _operador.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inicio", null);

        var resp = await jugadorClient.GetAsync($"{Rutas.Base}/mi-sesion");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        // Leer como string primero para poder verificar el cuerpo crudo y deserializar una vez.
        var raw = await resp.Content.ReadAsStringAsync();
        var dto = JsonSerializer.Deserialize<MiSesionDto>(raw, _jsonOpts);

        Assert.NotNull(dto);
        Assert.Equal("Iniciada", dto!.EstadoPartida);
        Assert.NotNull(dto.JuegoActivo);
        Assert.Equal("BusquedaDelTesoro", dto.JuegoActivo!.TipoJuego);
        Assert.NotNull(dto.EtapaActual);
        Assert.Equal(1, dto.EtapaActual!.Orden);

        // NO-LEAK: el cuerpo crudo NO debe contener el QR esperado ni la clave codigoQR*.
        Assert.DoesNotContain("QR-SECRETO", raw);
        Assert.DoesNotContain("codigoQR", raw, StringComparison.OrdinalIgnoreCase);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 4: partida Trivia iniciada → recupera pregunta activa + NO-LEAK de opción correcta
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Reconexion_recupera_pregunta_trivia_activa_sin_filtrar_correcta()
    {
        var partidaId = Guid.NewGuid();
        var jugador = Guid.NewGuid();
        var pregId = Guid.NewGuid();
        var correctaId = Guid.NewGuid();
        _factory.Stub.Respuestas[partidaId] = TriviaConfig(1, (pregId, correctaId, 20));
        var jugadorClient = _factory.CreateClientAs(jugador);

        await _operador.PostAsync($"{Rutas.Base}/partidas/{partidaId}/publicacion", null);
        await jugadorClient.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inscripciones", null);
        await _operador.PostAsync($"{Rutas.Base}/partidas/{partidaId}/inicio", null);

        var resp = await jugadorClient.GetAsync($"{Rutas.Base}/mi-sesion");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var raw = await resp.Content.ReadAsStringAsync();
        var dto = JsonSerializer.Deserialize<MiSesionDto>(raw, _jsonOpts);

        Assert.NotNull(dto);
        Assert.Equal("Iniciada", dto!.EstadoPartida);
        Assert.NotNull(dto.JuegoActivo);
        Assert.Equal("Trivia", dto.JuegoActivo!.TipoJuego);
        Assert.NotNull(dto.PreguntaActual);
        Assert.Equal(false, dto.YaRespondioPreguntaActual);

        // NO-LEAK: el cuerpo crudo NO debe exponer la propiedad esCorrecta (OpcionPublicaDto la omite).
        Assert.DoesNotContain("esCorrecta", raw, StringComparison.OrdinalIgnoreCase);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers locales (private static — no importar de otras clases de test)
    // ─────────────────────────────────────────────────────────────────────────

    // Config BDT Individual con 1 etapa: replica la forma de BdtRuntimeEndpointsTests.BuildBdtConfig.
    private static ConfiguracionPartidaDto BdtConfig(string qr, int puntaje)
    {
        var etapa = new EtapaConfigDto(Guid.NewGuid(), 1, qr, puntaje, 3600);
        var juego = new JuegoResumenDto(Guid.NewGuid(), 1, "BusquedaDelTesoro",
            Trivia: null,
            Bdt: new BdtConfigDto("Plaza central", new List<EtapaConfigDto> { etapa }));
        return new ConfiguracionPartidaDto("Copa", "Individual", "Manual", null, 1, 10,
            new List<JuegoResumenDto> { juego });
    }

    // Config Trivia Individual: replica la forma de TriviaRuntimeEndpointsTests.BuildTriviaConfig.
    private static ConfiguracionPartidaDto TriviaConfig(
        int minParticipacion, params (Guid PregId, Guid CorrectaId, int Puntaje)[] preguntas)
    {
        var preguntaConfigs = new List<PreguntaConfigDto>();
        for (var i = 0; i < preguntas.Length; i++)
        {
            var (pregId, correctaId, puntaje) = preguntas[i];
            preguntaConfigs.Add(new PreguntaConfigDto(
                pregId,
                $"Pregunta {i + 1}",
                puntaje,
                3600,
                new List<OpcionConfigDto>
                {
                    new(correctaId, "Opcion correcta", true),
                    new(Guid.NewGuid(), "Opcion incorrecta", false)
                }));
        }
        var juego = new JuegoResumenDto(Guid.NewGuid(), 1, "Trivia",
            new TriviaConfigDto(preguntaConfigs));
        return new ConfiguracionPartidaDto("Copa", "Individual", "Manual", null, minParticipacion, 10,
            new List<JuegoResumenDto> { juego });
    }
}
