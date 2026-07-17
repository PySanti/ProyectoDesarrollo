using Umbral.Puntuaciones.Application.Exceptions;
using Umbral.Puntuaciones.Application.Handlers.Queries;
using Umbral.Puntuaciones.Application.Queries;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;
using Umbral.Puntuaciones.UnitTests.Application.Fakes;

namespace Umbral.Puntuaciones.UnitTests.Application;

public class ObtenerHistorialPartidaQueryHandlerTests
{
    private static readonly DateTime Ahora = new(2026, 7, 6, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeProyeccionesRepository _proyecciones = new();
    private readonly FakeHistorialRepository _historial = new();

    private ObtenerHistorialPartidaQueryHandler Handler() => new(_proyecciones, _historial);

    private Guid SembrarPartida()
    {
        var partidaId = Guid.NewGuid();
        _proyecciones.AddPartida(PartidaProyectada.DesdePublicacion(partidaId, Guid.NewGuid(), Modalidad.Individual));
        return partidaId;
    }

    private void SembrarEvento(Guid partidaId, string tipo, DateTime occurredAt, string detalle = """{"orden":1}""")
        => _historial.AddEvento(EventoHistorial.Registrar(
            Guid.NewGuid(), partidaId, null, tipo, occurredAt, null, null, detalle));

    private void SembrarEventoDeJuego(Guid partidaId, Guid juegoId, string tipo, DateTime occurredAt)
        => _historial.AddEvento(EventoHistorial.Registrar(
            Guid.NewGuid(), partidaId, juegoId, tipo, occurredAt, null, null, """{}"""));

    [Fact]
    public async Task Devuelve_entradas_en_orden_cronologico_con_total_y_detalle()
    {
        var partidaId = SembrarPartida();
        SembrarEvento(partidaId, "PartidaIniciada", Ahora.AddMinutes(1));
        SembrarEvento(partidaId, "PartidaPublicadaEnLobby", Ahora);

        var response = await Handler().Handle(
            new ObtenerHistorialPartidaQuery(partidaId, 100, 0, null), CancellationToken.None);

        Assert.Equal(partidaId, response.PartidaId);
        Assert.Equal(2, response.Total);
        Assert.Equal(new[] { "PartidaPublicadaEnLobby", "PartidaIniciada" },
            response.Entradas.Select(e => e.TipoEvento).ToArray());
        Assert.Equal(1, response.Entradas[0].Detalle.GetProperty("orden").GetInt32());
    }

    [Fact]
    public async Task Paginacion_respeta_limit_y_offset_con_total_completo()
    {
        var partidaId = SembrarPartida();
        for (var i = 0; i < 5; i++)
        {
            SembrarEvento(partidaId, "UbicacionActualizada", Ahora.AddMinutes(i));
        }

        var response = await Handler().Handle(
            new ObtenerHistorialPartidaQuery(partidaId, 2, 3, null), CancellationToken.None);

        Assert.Equal(5, response.Total);
        Assert.Equal(2, response.Entradas.Count);
        Assert.Equal(Ahora.AddMinutes(3), response.Entradas[0].OccurredAt);
    }

    [Fact]
    public async Task Filtro_por_tipo_afecta_entradas_y_total()
    {
        var partidaId = SembrarPartida();
        SembrarEvento(partidaId, "UbicacionActualizada", Ahora);
        SembrarEvento(partidaId, "EtapaBDTGanada", Ahora.AddMinutes(1));

        var response = await Handler().Handle(
            new ObtenerHistorialPartidaQuery(partidaId, 100, 0, "EtapaBDTGanada"), CancellationToken.None);

        Assert.Equal(1, response.Total);
        Assert.Equal("EtapaBDTGanada", Assert.Single(response.Entradas).TipoEvento);
    }

    [Fact]
    public async Task Partida_desconocida_lanza_PartidaNoEncontrada()
        => await Assert.ThrowsAsync<PartidaNoEncontradaException>(() => Handler().Handle(
            new ObtenerHistorialPartidaQuery(Guid.NewGuid(), 100, 0, null), CancellationToken.None));

    [Fact]
    public async Task Partida_conocida_sin_eventos_devuelve_lista_vacia()
    {
        var partidaId = SembrarPartida();

        var response = await Handler().Handle(
            new ObtenerHistorialPartidaQuery(partidaId, 100, 0, null), CancellationToken.None);

        Assert.Equal(0, response.Total);
        Assert.Empty(response.Entradas);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(501, 0)]
    [InlineData(100, -1)]
    public async Task Limit_u_offset_invalidos_lanzan_ArgumentException(int limit, int offset)
    {
        var partidaId = SembrarPartida();

        await Assert.ThrowsAsync<ArgumentException>(() => Handler().Handle(
            new ObtenerHistorialPartidaQuery(partidaId, limit, offset, null), CancellationToken.None));
    }

    [Fact]
    public async Task Entrada_con_juego_proyectado_trae_orden_y_tipo()
    {
        var partidaId = SembrarPartida();
        var juegoId = Guid.NewGuid();
        _proyecciones.AddJuego(JuegoProyectado.Desde(juegoId, partidaId, 2, TipoJuego.Trivia));
        SembrarEventoDeJuego(partidaId, juegoId, "RespuestaTriviaValidada", Ahora);

        var response = await Handler().Handle(
            new ObtenerHistorialPartidaQuery(partidaId, 100, 0, null), CancellationToken.None);

        var entrada = Assert.Single(response.Entradas);
        Assert.Equal(2, entrada.JuegoOrden);
        Assert.Equal(TipoJuego.Trivia, entrada.TipoJuego);
    }

    [Fact]
    public async Task Evento_de_partida_sin_juego_deja_orden_y_tipo_en_null()
    {
        var partidaId = SembrarPartida();
        SembrarEvento(partidaId, "PartidaIniciada", Ahora);

        var response = await Handler().Handle(
            new ObtenerHistorialPartidaQuery(partidaId, 100, 0, null), CancellationToken.None);

        var entrada = Assert.Single(response.Entradas);
        Assert.Null(entrada.JuegoId);
        Assert.Null(entrada.JuegoOrden);
        Assert.Null(entrada.TipoJuego);
    }

    [Fact]
    public async Task Evento_con_juegoId_sin_proyeccion_deja_orden_null_sin_lanzar()
    {
        // Lag de proyeccion o evento perdido: el juego existe pero Puntuaciones no lo tiene.
        // El cliente cae al GUID corto; el handler no debe explotar.
        var partidaId = SembrarPartida();
        var juegoId = Guid.NewGuid();
        SembrarEventoDeJuego(partidaId, juegoId, "JuegoActivado", Ahora);

        var response = await Handler().Handle(
            new ObtenerHistorialPartidaQuery(partidaId, 100, 0, null), CancellationToken.None);

        var entrada = Assert.Single(response.Entradas);
        Assert.Equal(juegoId, entrada.JuegoId);
        Assert.Null(entrada.JuegoOrden);
        Assert.Null(entrada.TipoJuego);
    }
}
