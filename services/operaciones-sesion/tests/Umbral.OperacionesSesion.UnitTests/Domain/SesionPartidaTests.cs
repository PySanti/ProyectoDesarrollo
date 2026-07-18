using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.Domain.Results;
using Umbral.OperacionesSesion.Domain.ValueObjects;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class SesionPartidaTests
{
    private static ConfiguracionSnapshot Snapshot(
        Modalidad modalidad = Modalidad.Individual, int min = 1, int max = 2, int juegos = 1)
    {
        var lista = Enumerable.Range(1, juegos)
            .Select(o => new JuegoResumen(Guid.NewGuid(), o, TipoJuego.Trivia))
            .ToList();
        return new ConfiguracionSnapshot("Copa", modalidad, ModoInicioPartida.Manual, null, min, max, lista);
    }

    private static readonly DateTime T0 = new(2026, 6, 26, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime TDue = new(2026, 6, 26, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime TBefore = new(2026, 6, 26, 11, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Publicar_with_at_least_one_contiguous_game_sets_lobby()
    {
        var partidaId = Guid.NewGuid();
        var sesion = SesionPartida.Publicar(partidaId, Snapshot(juegos: 2));

        Assert.Equal(EstadoSesion.Lobby, sesion.Estado);
        Assert.Equal(partidaId, sesion.PartidaId);
        Assert.True(sesion.Id.EsValido());
        Assert.Equal(2, sesion.Juegos.Count);
        Assert.Empty(sesion.Inscripciones);
    }

    [Fact]
    public void Publicar_without_games_throws()
    {
        var partidaId = Guid.NewGuid();
        var snapshot = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 2,
            new List<JuegoResumen>());

        Assert.Throws<PartidaNoPublicableException>(() => SesionPartida.Publicar(partidaId, snapshot));
    }

    [Fact]
    public void Publicar_with_non_contiguous_orden_throws()
    {
        var snapshot = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 2,
            new List<JuegoResumen> { new(Guid.NewGuid(), 1, TipoJuego.Trivia), new(Guid.NewGuid(), 3, TipoJuego.Trivia) });

        Assert.Throws<PartidaNoPublicableException>(() => SesionPartida.Publicar(Guid.NewGuid(), snapshot));
    }

    [Fact]
    public void Inscribir_happy_path_adds_active_inscription()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), Snapshot());
        var participante = Guid.NewGuid();

        var inscripcion = sesion.Inscribir(participante, tieneParticipacionActivaEnOtra: false, inscritosActivos: 0, T0);
        sesion.AceptarInscripcion(inscripcion.Id.Valor, 0, T0);

        Assert.Equal(EstadoInscripcion.Activa, inscripcion.Estado);
        Assert.Equal(participante, inscripcion.ParticipanteId);
        Assert.Equal(T0, inscripcion.FechaInscripcion);
        Assert.Single(sesion.Inscripciones);
    }

    [Fact]
    public void Inscribir_equipo_modality_throws()
    {
        var equipo = SesionPartida.Publicar(Guid.NewGuid(), Snapshot(Modalidad.Equipo));
        Assert.Throws<ModalidadNoSoportadaException>(
            () => equipo.Inscribir(Guid.NewGuid(), false, 0, T0));
    }

    [Fact]
    public void Inscribir_when_not_in_lobby_throws_SesionNoEnLobby()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), Snapshot());
        typeof(SesionPartida).GetProperty("Estado")!.SetValue(sesion, EstadoSesion.Iniciada);

        Assert.Throws<SesionNoEnLobbyException>(
            () => sesion.Inscribir(Guid.NewGuid(), false, 0, T0));
    }

    [Fact]
    public void CancelarInscripcion_when_not_in_lobby_throws()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), Snapshot(max: 5));
        var participante = Guid.NewGuid();
        sesion.Inscribir(participante, false, 0, T0);
        typeof(SesionPartida).GetProperty("Estado")!.SetValue(sesion, EstadoSesion.Iniciada);

        Assert.Throws<SesionNoEnLobbyException>(
            () => sesion.CancelarInscripcion(participante));
    }

    [Fact]
    public void Inscribir_duplicate_participant_throws()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), Snapshot(max: 5));
        var participante = Guid.NewGuid();
        sesion.Inscribir(participante, false, 0, T0);

        Assert.Throws<ParticipanteYaInscritoException>(
            () => sesion.Inscribir(participante, false, 1, T0));
    }

    [Fact]
    public void Inscribir_with_active_participation_elsewhere_throws()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), Snapshot());
        Assert.Throws<ParticipacionActivaExistenteException>(
            () => sesion.Inscribir(Guid.NewGuid(), tieneParticipacionActivaEnOtra: true, inscritosActivos: 0, T0));
    }

    [Fact]
    public void Inscribir_when_capacity_full_throws()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), Snapshot(max: 1));
        Assert.Throws<CupoLlenoException>(
            () => sesion.Inscribir(Guid.NewGuid(), false, inscritosActivos: 1, T0));
    }

    [Fact]
    public void CancelarInscripcion_marks_cancelled_and_frees_capacity()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), Snapshot(max: 1));
        var participante = Guid.NewGuid();
        var insc = sesion.Inscribir(participante, false, 0, T0);
        sesion.AceptarInscripcion(insc.Id.Valor, 0, T0);

        sesion.CancelarInscripcion(participante);

        Assert.Equal(EstadoInscripcion.Cancelada, sesion.Inscripciones.Single().Estado);
        // capacity freed → a different participant can now inscribe and be accepted (active count back to 0)
        var otra = sesion.Inscribir(Guid.NewGuid(), false, 0, T0);
        sesion.AceptarInscripcion(otra.Id.Valor, 0, T0);
        Assert.Equal(EstadoInscripcion.Activa, otra.Estado);
    }

    [Fact]
    public void CancelarInscripcion_without_active_inscription_throws()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), Snapshot());
        Assert.Throws<InscripcionNoEncontradaException>(() => sesion.CancelarInscripcion(Guid.NewGuid()));
    }

    [Fact]
    public void Publicar_creates_all_games_in_pendiente()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), Snapshot(juegos: 3));
        Assert.All(sesion.Juegos, j => Assert.Equal(EstadoJuego.Pendiente, j.Estado));
    }

    private static ConfiguracionSnapshot SnapshotModo(
        ModoInicioPartida modo, int min = 1, int max = 5, int juegos = 2, DateTime? tiempoInicio = null)
    {
        var lista = Enumerable.Range(1, juegos)
            .Select(o => new JuegoResumen(Guid.NewGuid(), o, TipoJuego.Trivia))
            .ToList();
        return new ConfiguracionSnapshot("Copa", Modalidad.Individual, modo, tiempoInicio, min, max, lista);
    }

    [Fact]
    public void Iniciar_with_minimums_met_starts_and_activates_first_game()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), Snapshot(min: 1, max: 5, juegos: 2));
        InscribirYAceptar(sesion, T0);

        var resultado = sesion.Iniciar(T0);

        Assert.Equal(TipoResultadoInicio.Iniciada, resultado.Tipo);
        Assert.Equal(EstadoSesion.Iniciada, sesion.Estado);
        Assert.Equal(T0, sesion.FechaInicio);
        Assert.Null(sesion.FechaFin);
        var ordenados = sesion.Juegos.OrderBy(j => j.Orden).ToList();
        Assert.Equal(EstadoJuego.Activo, ordenados[0].Estado);
        Assert.Equal(EstadoJuego.Pendiente, ordenados[1].Estado);
        Assert.Equal(1, resultado.JuegoActivado!.Orden);
    }

    // Manual start with unmet minimums rejects and stays in Lobby — the auto-cancellation
    // belongs to the time-based start (domain-model-summary.md §Partida). The operator must be
    // able to accept the pending requests and retry instead of losing the partida on one click.
    [Fact]
    public void Iniciar_with_minimums_not_met_rejects_and_stays_in_lobby()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), Snapshot(min: 2, max: 5, juegos: 1));
        InscribirYAceptar(sesion, T0); // only 1 < 2

        var ex = Assert.Throws<MinimosNoAlcanzadosException>(() => sesion.Iniciar(T0));

        Assert.Equal(EstadoSesion.Lobby, sesion.Estado);
        Assert.Null(sesion.FechaFin);
        Assert.All(sesion.Juegos, j => Assert.Equal(EstadoJuego.Pendiente, j.Estado));
        Assert.Contains("1", ex.Message); // confirmadas
        Assert.Contains("2", ex.Message); // mínimo exigido
    }

    [Fact]
    public void Iniciar_when_not_in_lobby_throws()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), Snapshot());
        InscribirYAceptar(sesion, T0);
        sesion.Iniciar(T0); // now Iniciada

        Assert.Throws<SesionNoEnLobbyException>(() => sesion.Iniciar(T0));
    }

    [Fact]
    public void Iniciar_when_mode_is_automatic_only_throws()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), SnapshotModo(ModoInicioPartida.Automatico));
        sesion.Inscribir(Guid.NewGuid(), false, 0, T0);

        Assert.Throws<ModoInicioNoCompatibleException>(() => sesion.Iniciar(T0));
    }

    [Fact]
    public void IntentarInicioAutomatico_when_due_and_minimums_met_starts()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), SnapshotModo(ModoInicioPartida.Automatico, min: 1, tiempoInicio: TDue));
        InscribirYAceptar(sesion, TBefore);

        var resultado = sesion.IntentarInicioAutomatico(TDue);

        Assert.Equal(TipoResultadoInicio.Iniciada, resultado.Tipo);
        Assert.Equal(EstadoSesion.Iniciada, sesion.Estado);
    }

    [Fact]
    public void IntentarInicioAutomatico_when_due_and_minimums_not_met_auto_cancels()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), SnapshotModo(ModoInicioPartida.ManualYAutomatico, min: 2, tiempoInicio: TDue));
        sesion.Inscribir(Guid.NewGuid(), false, 0, TBefore); // 1 < 2

        var resultado = sesion.IntentarInicioAutomatico(TDue);

        Assert.Equal(TipoResultadoInicio.Cancelada, resultado.Tipo);
        Assert.Equal(EstadoSesion.Cancelada, sesion.Estado);
    }

    [Fact]
    public void IntentarInicioAutomatico_before_tiempo_inicio_is_noop()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), SnapshotModo(ModoInicioPartida.Automatico, min: 1, tiempoInicio: TDue));
        sesion.Inscribir(Guid.NewGuid(), false, 0, TBefore);

        var resultado = sesion.IntentarInicioAutomatico(TBefore); // before due

        Assert.Equal(TipoResultadoInicio.NoCorresponde, resultado.Tipo);
        Assert.Equal(EstadoSesion.Lobby, sesion.Estado);
    }

    [Fact]
    public void IntentarInicioAutomatico_when_not_in_lobby_is_idempotent_noop()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), SnapshotModo(ModoInicioPartida.ManualYAutomatico, min: 1, tiempoInicio: TDue));
        InscribirYAceptar(sesion, TBefore);
        sesion.Iniciar(TDue); // now Iniciada (manual path allowed by ManualYAutomatico)

        var resultado = sesion.IntentarInicioAutomatico(TDue);

        Assert.Equal(TipoResultadoInicio.NoCorresponde, resultado.Tipo);
        Assert.Equal(EstadoSesion.Iniciada, sesion.Estado); // unchanged
    }

    [Fact]
    public void IntentarInicioAutomatico_when_mode_is_manual_only_throws()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), SnapshotModo(ModoInicioPartida.Manual, min: 1, tiempoInicio: TDue));
        sesion.Inscribir(Guid.NewGuid(), false, 0, TBefore);

        Assert.Throws<ModoInicioNoCompatibleException>(() => sesion.IntentarInicioAutomatico(TDue));
    }

    // HU-19: la inscripción nace Pendiente; para que cuente en mínimos hay que aceptarla.
    private static void InscribirYAceptar(SesionPartida sesion, DateTime fecha)
    {
        var insc = sesion.Inscribir(Guid.NewGuid(), false, 0, fecha);
        sesion.AceptarInscripcion(insc.Id.Valor, 0, fecha);
    }

    private static SesionPartida Iniciada(int juegos)
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), Snapshot(min: 1, max: 5, juegos: juegos));
        InscribirYAceptar(sesion, T0);
        sesion.Iniciar(T0);
        return sesion;
    }

    [Fact]
    public void FinalizarJuegoActual_advances_to_next_game()
    {
        var sesion = Iniciada(juegos: 3);

        var resultado = sesion.FinalizarJuegoActual(T0);

        Assert.Equal(TipoResultadoAvance.Avanzado, resultado.Tipo);
        Assert.False(resultado.Terminada());
        var ordenados = sesion.Juegos.OrderBy(j => j.Orden).ToList();
        Assert.Equal(EstadoJuego.Finalizado, ordenados[0].Estado);
        Assert.Equal(EstadoJuego.Activo, ordenados[1].Estado);
        Assert.Equal(EstadoJuego.Pendiente, ordenados[2].Estado);
        Assert.Equal(1, resultado.JuegoFinalizado.Orden);
        Assert.Equal(2, resultado.JuegoActivado!.Orden);
        Assert.Equal(EstadoSesion.Iniciada, sesion.Estado);
    }

    [Fact]
    public void FinalizarJuegoActual_on_last_game_terminates_partida()
    {
        var sesion = Iniciada(juegos: 1);

        var resultado = sesion.FinalizarJuegoActual(T0);

        Assert.Equal(TipoResultadoAvance.Terminada, resultado.Tipo);
        Assert.True(resultado.Terminada());
        Assert.Equal(EstadoSesion.Terminada, sesion.Estado);
        Assert.Equal(T0, sesion.FechaFin);
        Assert.Null(resultado.JuegoActivado);
        Assert.Equal(EstadoJuego.Finalizado, sesion.Juegos.Single().Estado);
    }

    [Fact]
    public void FinalizarJuegoActual_runs_full_sequence_to_terminada()
    {
        var sesion = Iniciada(juegos: 2);

        Assert.Equal(TipoResultadoAvance.Avanzado, sesion.FinalizarJuegoActual(T0).Tipo);
        Assert.Equal(TipoResultadoAvance.Terminada, sesion.FinalizarJuegoActual(T0).Tipo);
        Assert.Equal(EstadoSesion.Terminada, sesion.Estado);
        Assert.All(sesion.Juegos, j => Assert.Equal(EstadoJuego.Finalizado, j.Estado));
    }

    [Fact]
    public void FinalizarJuegoActual_when_not_iniciada_throws()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), Snapshot()); // Lobby
        Assert.Throws<SesionNoIniciadaException>(() => sesion.FinalizarJuegoActual(T0));
    }

    // HU-40: cancelación manual por el operador.
    [Fact]
    public void Cancelar_from_lobby_sets_cancelada_and_fecha_fin()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), Snapshot()); // Lobby

        sesion.Cancelar(T0);

        Assert.Equal(EstadoSesion.Cancelada, sesion.Estado);
        Assert.Equal(T0, sesion.FechaFin);
    }

    [Fact]
    public void Cancelar_from_iniciada_sets_cancelada_and_fecha_fin()
    {
        var sesion = Iniciada(juegos: 2);

        sesion.Cancelar(T0);

        Assert.Equal(EstadoSesion.Cancelada, sesion.Estado);
        Assert.Equal(T0, sesion.FechaFin);
        // 7c review (Important): el juego que estaba Activo no puede quedar "vivo" para siempre;
        // se cierra (Finalizado). El siguiente juego, todavía Pendiente, no se toca.
        var ordenados = sesion.Juegos.OrderBy(j => j.Orden).ToList();
        Assert.Equal(EstadoJuego.Finalizado, ordenados[0].Estado);
        Assert.Equal(EstadoJuego.Pendiente, ordenados[1].Estado);
    }

    // 7c review (Important): cancelar con Trivia activa debe cerrar la pregunta activa del
    // juego activo (no solo mutar Estado/FechaFin de la sesión), reusando el mecanismo de
    // cierre interno de PreguntaSnapshot — igual camino que CerrarActividadVencida.
    private static PreguntaSnapshot PreguntaConOpciones(int orden = 1, int limite = 60) =>
        new(Guid.NewGuid(), orden, $"Q{orden}", 10, limite,
            new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true), new OpcionSnapshot(Guid.NewGuid(), "no", false) });

    private static SesionPartida TriviaIniciadaConPreguntaActiva()
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { PreguntaConOpciones() });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var s = SesionPartida.Publicar(Guid.NewGuid(), snap);
        InscribirYAceptar(s, T0);
        s.Iniciar(T0);
        return s;
    }

    private static SesionPartida BdtIniciadaConEtapaActiva()
    {
        var etapa = new EtapaSnapshot(Guid.NewGuid(), 1, "QR-1", 50, 60);
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, "Área", new[] { etapa });
        var snap = new ConfiguracionSnapshot("Copa BDT", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var s = SesionPartida.Publicar(Guid.NewGuid(), snap);
        InscribirYAceptar(s, T0);
        s.Iniciar(T0);
        return s;
    }

    [Fact]
    public void Cancelar_from_iniciada_con_trivia_activa_cierra_pregunta_y_finaliza_juego()
    {
        var sesion = TriviaIniciadaConPreguntaActiva();
        var juego = sesion.Juegos.Single();
        Assert.NotNull(juego.PreguntaActiva); // precondición: hay pregunta viva antes de cancelar

        sesion.Cancelar(T0);

        Assert.Equal(EstadoSesion.Cancelada, sesion.Estado);
        Assert.Equal(EstadoJuego.Finalizado, juego.Estado);
        Assert.Null(juego.PreguntaActiva);
        Assert.Equal(EstadoPregunta.Cerrada, juego.Preguntas.Single().Estado);
    }

    [Fact]
    public void Cancelar_from_iniciada_con_bdt_activa_cierra_etapa_y_finaliza_juego()
    {
        var sesion = BdtIniciadaConEtapaActiva();
        var juego = sesion.Juegos.Single();
        Assert.NotNull(juego.EtapaActiva); // precondición: hay etapa viva antes de cancelar

        sesion.Cancelar(T0);

        Assert.Equal(EstadoSesion.Cancelada, sesion.Estado);
        Assert.Equal(EstadoJuego.Finalizado, juego.Estado);
        Assert.Null(juego.EtapaActiva);
        Assert.Equal(EstadoEtapa.Cerrada, juego.Etapas.Single().Estado);
    }

    [Fact]
    public void Cancelar_from_terminada_throws()
    {
        var sesion = Iniciada(juegos: 1);
        sesion.FinalizarJuegoActual(T0); // now Terminada

        Assert.Throws<PartidaNoCancelableException>(() => sesion.Cancelar(T0));
    }

    [Fact]
    public void Cancelar_from_cancelada_throws()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), Snapshot()); // Lobby
        sesion.Cancelar(T0);

        Assert.Throws<PartidaNoCancelableException>(() => sesion.Cancelar(T0));
    }
}
