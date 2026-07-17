using Umbral.Puntuaciones.Application.Handlers.Queries;
using Umbral.Puntuaciones.Application.Queries;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;
using Umbral.Puntuaciones.UnitTests.Application.Fakes;

namespace Umbral.Puntuaciones.UnitTests.Application;

public class ObtenerHistorialPartidasQueryHandlerTests
{
    private static readonly DateTime Ahora = new(2026, 7, 6, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeProyeccionesRepository _proyecciones = new();

    // La pertenencia al equipo se resuelve por convocatoria aceptada, no por el relato de auditoría:
    // el handler ya no depende de IHistorialRepository.
    private ObtenerHistorialPartidasQueryHandler Handler() => new(_proyecciones);

    private void SembrarConvocatoria(Guid partidaId, Guid equipoId, Guid usuarioId, bool? aceptada)
    {
        var convocatoria = ConvocatoriaProyectada.Nueva(Guid.NewGuid(), partidaId, equipoId, usuarioId);
        if (aceptada is not null)
        {
            convocatoria.Responder(aceptada.Value);
        }
        _proyecciones.AddConvocatoria(convocatoria);
    }

    private Guid SembrarPartidaTerminada(Modalidad modalidad, DateTime fechaFin, out Guid juegoId)
    {
        var partidaId = Guid.NewGuid();
        juegoId = Guid.NewGuid();
        var partida = PartidaProyectada.DesdePublicacion(partidaId, Guid.NewGuid(), modalidad);
        partida.MarcarTerminada(fechaFin);
        _proyecciones.AddPartida(partida);
        _proyecciones.AddJuego(JuegoProyectado.Desde(juegoId, partidaId, 1, TipoJuego.Trivia));
        return partidaId;
    }

    private void SembrarMarcador(Guid partidaId, Guid juegoId, Guid competidorId, TipoCompetidor tipo, int puntos, long tiempoMs)
    {
        var marcador = Marcador.Nuevo(juegoId, competidorId, partidaId, tipo);
        marcador.Acreditar(puntos, tiempoMs);
        _proyecciones.AddMarcador(marcador);
    }

    [Fact]
    public async Task Individual_lista_partida_con_posicion_puntos_y_juegos()
    {
        var participanteId = Guid.NewGuid();
        var rival = Guid.NewGuid();
        var partidaId = SembrarPartidaTerminada(Modalidad.Individual, Ahora, out var juegoId);
        SembrarMarcador(partidaId, juegoId, participanteId, TipoCompetidor.Participante, 10, 1000);
        SembrarMarcador(partidaId, juegoId, rival, TipoCompetidor.Participante, 20, 900);

        var response = await Handler().Handle(new ObtenerHistorialPartidasQuery(participanteId), CancellationToken.None);

        Assert.Equal(participanteId, response.ParticipanteId);
        var partida = Assert.Single(response.Partidas);
        Assert.Equal(partidaId, partida.PartidaId);
        Assert.Equal(Modalidad.Individual, partida.Modalidad);
        Assert.Null(partida.EquipoId);
        Assert.Equal(10, partida.PuntosTotales);
        Assert.Equal(2, partida.Posicion);
        Assert.False(partida.Gano);
        var juego = Assert.Single(partida.Juegos);
        Assert.Equal(juegoId, juego.JuegoId);
        Assert.Equal(1, juego.Orden);
        Assert.Equal(TipoJuego.Trivia, juego.TipoJuego);
        Assert.Equal(10, juego.Puntos);
    }

    [Fact]
    public async Task Equipo_resuelto_por_convocatoria_aceptada_muestra_puntuacion_y_posicion_del_equipo()
    {
        var participanteId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var rival = Guid.NewGuid();
        var partidaId = SembrarPartidaTerminada(Modalidad.Equipo, Ahora, out var juegoId);
        SembrarMarcador(partidaId, juegoId, equipoId, TipoCompetidor.Equipo, 30, 1000);
        SembrarMarcador(partidaId, juegoId, rival, TipoCompetidor.Equipo, 20, 900);
        SembrarConvocatoria(partidaId, equipoId, participanteId, aceptada: true);

        var response = await Handler().Handle(new ObtenerHistorialPartidasQuery(participanteId), CancellationToken.None);

        var partida = Assert.Single(response.Partidas);
        Assert.Equal(equipoId, partida.EquipoId);
        Assert.Equal(30, partida.PuntosTotales);
        Assert.Equal(1, partida.Posicion);
        Assert.True(partida.Gano);
    }

    [Fact]
    public async Task Convocatoria_pendiente_de_responder_no_lista_la_partida()
    {
        var participanteId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var partidaId = SembrarPartidaTerminada(Modalidad.Equipo, Ahora, out var juegoId);
        SembrarMarcador(partidaId, juegoId, equipoId, TipoCompetidor.Equipo, 30, 1000);
        SembrarConvocatoria(partidaId, equipoId, participanteId, aceptada: null);

        var response = await Handler().Handle(new ObtenerHistorialPartidasQuery(participanteId), CancellationToken.None);

        // Que te convoquen no es jugar: hace falta aceptar.
        Assert.Empty(response.Partidas);
    }

    [Fact]
    public async Task Miembro_con_convocatoria_rechazada_no_ve_la_partida()
    {
        var participanteId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var partidaId = SembrarPartidaTerminada(Modalidad.Equipo, Ahora, out var juegoId);
        SembrarMarcador(partidaId, juegoId, equipoId, TipoCompetidor.Equipo, 30, 1000);
        SembrarConvocatoria(partidaId, equipoId, participanteId, aceptada: false);

        var response = await Handler().Handle(new ObtenerHistorialPartidasQuery(participanteId), CancellationToken.None);

        Assert.Empty(response.Partidas);
    }

    [Fact]
    public async Task Miembro_que_acepto_convocatoria_y_no_actuo_ve_la_partida()
    {
        var participanteId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var partidaId = SembrarPartidaTerminada(Modalidad.Equipo, Ahora, out _);
        _proyecciones.AddParticipacion(ParticipacionProyectada.Nueva(partidaId, equipoId, TipoCompetidor.Equipo));
        SembrarConvocatoria(partidaId, equipoId, participanteId, aceptada: true);

        var response = await Handler().Handle(new ObtenerHistorialPartidasQuery(participanteId), CancellationToken.None);

        // Retira la limitación: antes el integrante solo veía la partida si el equipo había anotado
        // y él mismo había autorado alguna acción de juego.
        var partida = Assert.Single(response.Partidas);
        Assert.Equal(equipoId, partida.EquipoId);
        Assert.Equal(0, partida.PuntosTotales);
    }

    [Fact]
    public async Task Equipo_sin_participacion_proyectada_no_rompe_al_miembro()
    {
        var participanteId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var partidaId = SembrarPartidaTerminada(Modalidad.Equipo, Ahora, out _);
        // Sin participación ni marcador del equipo (InscripcionAceptada perdida, best-effort ADR-0012).
        SembrarConvocatoria(partidaId, equipoId, participanteId, aceptada: true);

        var response = await Handler().Handle(new ObtenerHistorialPartidasQuery(participanteId), CancellationToken.None);

        // Debe omitir la partida, no lanzar: sin el guard, entradas.First(...) reventaría.
        Assert.Empty(response.Partidas);
    }

    [Fact]
    public async Task Incluye_partida_individual_terminada_donde_no_puntuo()
    {
        var participanteId = Guid.NewGuid();
        var rival = Guid.NewGuid();
        var partidaId = SembrarPartidaTerminada(Modalidad.Individual, Ahora, out var juegoId);
        _proyecciones.AddParticipacion(
            ParticipacionProyectada.Nueva(partidaId, participanteId, TipoCompetidor.Participante));
        SembrarMarcador(partidaId, juegoId, rival, TipoCompetidor.Participante, 20, 900);

        var response = await Handler().Handle(new ObtenerHistorialPartidasQuery(participanteId), CancellationToken.None);

        // Antes: sin marcador la partida no aparecía; jugar y no acertar era no haber jugado.
        var partida = Assert.Single(response.Partidas);
        Assert.Equal(partidaId, partida.PartidaId);
        Assert.Equal(0, partida.PuntosTotales);
        Assert.Equal(2, partida.Posicion);
        Assert.False(partida.Gano);
    }

    [Fact]
    public async Task Partida_anterior_al_slice_sin_participacion_proyectada_sigue_en_el_historial()
    {
        var participanteId = Guid.NewGuid();
        var partidaId = SembrarPartidaTerminada(Modalidad.Individual, Ahora, out var juegoId);
        // Sin fila de participación: no hay backfill. El marcador prueba que jugó.
        SembrarMarcador(partidaId, juegoId, participanteId, TipoCompetidor.Participante, 10, 1000);

        var response = await Handler().Handle(new ObtenerHistorialPartidasQuery(participanteId), CancellationToken.None);

        var partida = Assert.Single(response.Partidas);
        Assert.Equal(10, partida.PuntosTotales);
    }

    [Fact]
    public async Task Partida_no_terminada_o_cancelada_no_se_lista()
    {
        var participanteId = Guid.NewGuid();
        var enCurso = Guid.NewGuid();
        var juegoEnCurso = Guid.NewGuid();
        var partidaEnCurso = PartidaProyectada.DesdePublicacion(enCurso, Guid.NewGuid(), Modalidad.Individual);
        partidaEnCurso.MarcarIniciada(Ahora);
        _proyecciones.AddPartida(partidaEnCurso);
        SembrarMarcador(enCurso, juegoEnCurso, participanteId, TipoCompetidor.Participante, 10, 1000);

        var cancelada = Guid.NewGuid();
        var juegoCancelado = Guid.NewGuid();
        var partidaCancelada = PartidaProyectada.DesdePublicacion(cancelada, Guid.NewGuid(), Modalidad.Individual);
        partidaCancelada.MarcarCancelada(Ahora);
        _proyecciones.AddPartida(partidaCancelada);
        SembrarMarcador(cancelada, juegoCancelado, participanteId, TipoCompetidor.Participante, 5, 500);

        var response = await Handler().Handle(new ObtenerHistorialPartidasQuery(participanteId), CancellationToken.None);

        Assert.Empty(response.Partidas);
    }

    [Fact]
    public async Task Ordena_por_fechaFin_descendente()
    {
        var participanteId = Guid.NewGuid();
        var vieja = SembrarPartidaTerminada(Modalidad.Individual, Ahora.AddDays(-2), out var juegoViejo);
        var reciente = SembrarPartidaTerminada(Modalidad.Individual, Ahora, out var juegoReciente);
        SembrarMarcador(vieja, juegoViejo, participanteId, TipoCompetidor.Participante, 10, 1000);
        SembrarMarcador(reciente, juegoReciente, participanteId, TipoCompetidor.Participante, 10, 1000);

        var response = await Handler().Handle(new ObtenerHistorialPartidasQuery(participanteId), CancellationToken.None);

        Assert.Equal(new[] { reciente, vieja }, response.Partidas.Select(p => p.PartidaId).ToArray());
    }

    [Fact]
    public async Task Participante_sin_partidas_devuelve_lista_vacia()
    {
        var response = await Handler().Handle(new ObtenerHistorialPartidasQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Empty(response.Partidas);
    }

    [Fact]
    public async Task Juego_sin_marcador_propio_aparece_con_cero_puntos()
    {
        var participanteId = Guid.NewGuid();
        var partidaId = SembrarPartidaTerminada(Modalidad.Individual, Ahora, out var juego1);
        var juego2 = Guid.NewGuid();
        _proyecciones.AddJuego(JuegoProyectado.Desde(juego2, partidaId, 2, TipoJuego.BusquedaDelTesoro));
        SembrarMarcador(partidaId, juego1, participanteId, TipoCompetidor.Participante, 10, 1000);

        var response = await Handler().Handle(new ObtenerHistorialPartidasQuery(participanteId), CancellationToken.None);

        var partida = Assert.Single(response.Partidas);
        Assert.Equal(2, partida.Juegos.Count);
        Assert.Equal(0, partida.Juegos.Single(j => j.JuegoId == juego2).Puntos);
    }
}
