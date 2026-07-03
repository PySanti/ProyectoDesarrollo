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
        sesion.Inscribir(participante, false, 0, T0);

        sesion.CancelarInscripcion(participante);

        Assert.Equal(EstadoInscripcion.Cancelada, sesion.Inscripciones.Single().Estado);
        // capacity freed → a different participant can now inscribe (active count back to 0)
        var otra = sesion.Inscribir(Guid.NewGuid(), false, 0, T0);
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
        sesion.Inscribir(Guid.NewGuid(), false, 0, T0);

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

    [Fact]
    public void Iniciar_with_minimums_not_met_auto_cancels()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), Snapshot(min: 2, max: 5, juegos: 1));
        sesion.Inscribir(Guid.NewGuid(), false, 0, T0); // only 1 < 2

        var resultado = sesion.Iniciar(T0);

        Assert.Equal(TipoResultadoInicio.Cancelada, resultado.Tipo);
        Assert.Equal(EstadoSesion.Cancelada, sesion.Estado);
        Assert.Equal(T0, sesion.FechaFin);
        Assert.Null(resultado.JuegoActivado);
        Assert.All(sesion.Juegos, j => Assert.Equal(EstadoJuego.Pendiente, j.Estado));
    }

    [Fact]
    public void Iniciar_when_not_in_lobby_throws()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), Snapshot());
        sesion.Inscribir(Guid.NewGuid(), false, 0, T0);
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
        sesion.Inscribir(Guid.NewGuid(), false, 0, TBefore);

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
        sesion.Inscribir(Guid.NewGuid(), false, 0, TBefore);
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

    private static SesionPartida Iniciada(int juegos)
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), Snapshot(min: 1, max: 5, juegos: juegos));
        sesion.Inscribir(Guid.NewGuid(), false, 0, T0);
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
}
