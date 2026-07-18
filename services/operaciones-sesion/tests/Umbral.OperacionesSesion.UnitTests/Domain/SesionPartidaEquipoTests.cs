using System;
using System.Collections.Generic;
using System.Linq;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.Domain.Results;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class SesionPartidaEquipoTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);

    private static SesionPartida PartidaEquipoEnLobby(int minimos = 1, int maximos = 5)
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[]
        {
            new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30,
                new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true) })
        });
        var snap = new ConfiguracionSnapshot(
            "Copa Equipos", Modalidad.Equipo, ModoInicioPartida.Manual, null, minimos, maximos,
            new List<JuegoResumen> { juego });
        return SesionPartida.Publicar(Guid.NewGuid(), snap);
    }

    [Fact]
    public void PreinscribirEquipo_feliz_crea_inscripcion_y_convocatorias()
    {
        var sesion = PartidaEquipoEnLobby();
        var equipoId = Guid.NewGuid();
        var lider = Guid.NewGuid();
        var miembros = new List<Guid> { lider, Guid.NewGuid() };

        var insc = sesion.PreinscribirEquipo(equipoId, callerEsLider: true, lider, miembros, false, 0, T0);
        sesion.AceptarInscripcion(insc.Id.Valor, 0, T0); // HU-19: aceptar crea las convocatorias

        Assert.Equal(Modalidad.Equipo, insc.Modalidad);
        Assert.Equal(equipoId, insc.EquipoId);
        Assert.Equal(2, insc.Convocatorias.Count);
        Assert.Single(sesion.Inscripciones);
    }

    [Fact]
    public void PreinscribirEquipo_no_lider_lanza()
    {
        var sesion = PartidaEquipoEnLobby();
        Assert.Throws<NoEsLiderEquipoException>(() =>
            sesion.PreinscribirEquipo(Guid.NewGuid(), callerEsLider: false, Guid.NewGuid(), new[] { Guid.NewGuid() }, false, 0, T0));
    }

    [Fact]
    public void PreinscribirEquipo_equipo_ya_inscrito_lanza()
    {
        var sesion = PartidaEquipoEnLobby();
        var equipoId = Guid.NewGuid();
        sesion.PreinscribirEquipo(equipoId, true, Guid.NewGuid(), new[] { Guid.NewGuid() }, false, 0, T0);

        Assert.Throws<EquipoYaInscritoException>(() =>
            sesion.PreinscribirEquipo(equipoId, true, Guid.NewGuid(), new[] { Guid.NewGuid() }, false, 1, T0));
    }

    [Fact]
    public void PreinscribirEquipo_participacion_activa_en_otra_lanza()
    {
        var sesion = PartidaEquipoEnLobby();
        Assert.Throws<ParticipacionActivaExistenteException>(() =>
            sesion.PreinscribirEquipo(Guid.NewGuid(), true, Guid.NewGuid(), new[] { Guid.NewGuid() },
                equipoTieneParticipacionActivaEnOtra: true, 0, T0));
    }

    [Fact]
    public void PreinscribirEquipo_cupo_lleno_lanza()
    {
        var sesion = PartidaEquipoEnLobby(minimos: 1, maximos: 2);
        Assert.Throws<CupoLlenoException>(() =>
            sesion.PreinscribirEquipo(Guid.NewGuid(), true, Guid.NewGuid(), new[] { Guid.NewGuid() }, false, equiposActivos: 2, T0));
    }

    [Fact]
    public void PreinscribirEquipo_en_partida_individual_lanza_modalidad()
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[]
        {
            new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30,
                new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true) })
        });
        var snap = new ConfiguracionSnapshot("Individual", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5,
            new List<JuegoResumen> { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap);

        Assert.Throws<ModalidadNoSoportadaException>(() =>
            sesion.PreinscribirEquipo(Guid.NewGuid(), true, Guid.NewGuid(), new[] { Guid.NewGuid() }, false, 0, T0));
    }

    private static (SesionPartida sesion, Guid convocatoriaId, Guid usuario) EquipoConUnaConvocatoria()
    {
        var sesion = PartidaEquipoEnLobby();
        var usuario = Guid.NewGuid();
        var insc = sesion.PreinscribirEquipo(Guid.NewGuid(), true, usuario, new[] { usuario }, false, 0, T0);
        sesion.AceptarInscripcion(insc.Id.Valor, 0, T0); // HU-19: aceptar crea las convocatorias
        return (sesion, insc.Convocatorias[0].Id.Valor, usuario);
    }

    [Fact]
    public void ResponderConvocatoria_aceptar_marca_aceptada()
    {
        var (sesion, convocatoriaId, usuario) = EquipoConUnaConvocatoria();

        var c = sesion.ResponderConvocatoria(convocatoriaId, usuario, aceptar: true, false, T0.AddMinutes(1));

        Assert.True(c.EstaAceptada);
    }

    [Fact]
    public void ResponderConvocatoria_rechazar_marca_rechazada()
    {
        var (sesion, convocatoriaId, usuario) = EquipoConUnaConvocatoria();

        var c = sesion.ResponderConvocatoria(convocatoriaId, usuario, aceptar: false, false, T0.AddMinutes(1));

        Assert.Equal(EstadoConvocatoria.Rechazada, c.Estado);
    }

    [Fact]
    public void ResponderConvocatoria_aceptar_con_participacion_activa_en_otra_lanza()
    {
        var (sesion, convocatoriaId, usuario) = EquipoConUnaConvocatoria();

        Assert.Throws<ParticipacionActivaExistenteException>(() =>
            sesion.ResponderConvocatoria(convocatoriaId, usuario, aceptar: true,
                participanteTieneParticipacionActivaEnOtra: true, T0));
    }

    [Fact]
    public void ResponderConvocatoria_id_inexistente_lanza()
    {
        var (sesion, _, usuario) = EquipoConUnaConvocatoria();

        Assert.Throws<ConvocatoriaNoEncontradaException>(() =>
            sesion.ResponderConvocatoria(Guid.NewGuid(), usuario, true, false, T0));
    }

    [Fact]
    public void ResponderConvocatoria_usuario_distinto_lanza()
    {
        var (sesion, convocatoriaId, _) = EquipoConUnaConvocatoria();

        Assert.Throws<ConvocatoriaNoEncontradaException>(() =>
            sesion.ResponderConvocatoria(convocatoriaId, Guid.NewGuid(), true, false, T0));
    }

    [Fact]
    public void ResponderConvocatoria_ya_respondida_lanza()
    {
        var (sesion, convocatoriaId, usuario) = EquipoConUnaConvocatoria();
        sesion.ResponderConvocatoria(convocatoriaId, usuario, true, false, T0);

        Assert.Throws<ConvocatoriaNoEncontradaException>(() =>
            sesion.ResponderConvocatoria(convocatoriaId, usuario, false, false, T0));
    }

    [Fact]
    public void CancelarInscripcionEquipo_lider_cancela()
    {
        var sesion = PartidaEquipoEnLobby();
        var equipoId = Guid.NewGuid();
        var insc = sesion.PreinscribirEquipo(equipoId, true, Guid.NewGuid(), new[] { Guid.NewGuid() }, false, 0, T0);
        sesion.AceptarInscripcion(insc.Id.Valor, 0, T0); // HU-19: aceptar para tener inscripción activa que cancelar

        sesion.CancelarInscripcionEquipo(equipoId, callerEsLider: true);

        Assert.DoesNotContain(sesion.Inscripciones, i => i.EsActiva);
    }

    [Fact]
    public void CancelarInscripcionEquipo_no_lider_lanza()
    {
        var sesion = PartidaEquipoEnLobby();
        var equipoId = Guid.NewGuid();
        sesion.PreinscribirEquipo(equipoId, true, Guid.NewGuid(), new[] { Guid.NewGuid() }, false, 0, T0);

        Assert.Throws<NoEsLiderEquipoException>(() =>
            sesion.CancelarInscripcionEquipo(equipoId, callerEsLider: false));
    }

    [Fact]
    public void CancelarInscripcionEquipo_equipo_no_inscrito_lanza_mensaje_de_equipo()
    {
        var sesion = PartidaEquipoEnLobby();
        var equipoId = Guid.NewGuid();

        var ex = Assert.Throws<InscripcionNoEncontradaException>(
            () => sesion.CancelarInscripcionEquipo(equipoId, callerEsLider: true));

        Assert.Contains("equipo", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // El inicio MANUAL con mínimos incumplidos rechaza y deja la partida en Lobby: la
    // cancelación automática es del inicio por tiempo (domain-model-summary.md §Partida).
    // Antes cancelaba: el operador veía la solicitud del equipo en el lobby, pulsaba Iniciar
    // y perdía la partida sin poder aceptarla.
    [Fact]
    public void Iniciar_equipo_sin_aceptados_rechaza_y_sigue_en_lobby()
    {
        var sesion = PartidaEquipoEnLobby(minimos: 1);
        sesion.PreinscribirEquipo(Guid.NewGuid(), true, Guid.NewGuid(), new[] { Guid.NewGuid() }, false, 0, T0);
        // nadie aceptó → 0 equipos participantes < mínimo 1

        Assert.Throws<MinimosNoAlcanzadosException>(() => sesion.Iniciar(T0));

        Assert.Equal(EstadoSesion.Lobby, sesion.Estado);
        Assert.Null(sesion.FechaFin);
    }

    // El lobby contaba inscripciones activas y el inicio exigía convocatoria aceptada: el
    // operador leía "1 inscrito" y el inicio veía quórum 0. ParticipacionesConfirmadas es
    // el número que el inicio usa de verdad.
    [Fact]
    public void ParticipacionesConfirmadas_no_cuenta_equipo_activo_sin_convocatoria_aceptada()
    {
        var sesion = PartidaEquipoEnLobby(minimos: 1);
        var insc = sesion.PreinscribirEquipo(
            Guid.NewGuid(), true, Guid.NewGuid(), new[] { Guid.NewGuid() }, false, 0, T0);
        sesion.AceptarInscripcion(insc.Id.Valor, 0, T0); // activa, pero nadie aceptó su convocatoria

        Assert.Single(sesion.Inscripciones.Where(i => i.EsActiva));
        Assert.Equal(0, sesion.ParticipacionesConfirmadas);
    }

    [Fact]
    public void ParticipacionesConfirmadas_cuenta_equipo_con_convocatoria_aceptada()
    {
        var sesion = PartidaEquipoEnLobby(minimos: 1);
        var usuario = Guid.NewGuid();
        var insc = sesion.PreinscribirEquipo(Guid.NewGuid(), true, usuario, new[] { usuario }, false, 0, T0);
        sesion.AceptarInscripcion(insc.Id.Valor, 0, T0);
        sesion.ResponderConvocatoria(insc.Convocatorias[0].Id.Valor, usuario, true, false, T0);

        Assert.Equal(1, sesion.ParticipacionesConfirmadas);
    }

    [Fact]
    public void Iniciar_equipo_con_un_aceptado_inicia()
    {
        var sesion = PartidaEquipoEnLobby(minimos: 1);
        var usuario = Guid.NewGuid();
        var insc = sesion.PreinscribirEquipo(Guid.NewGuid(), true, usuario, new[] { usuario }, false, 0, T0);
        sesion.AceptarInscripcion(insc.Id.Valor, 0, T0); // HU-19: aceptar crea las convocatorias
        sesion.ResponderConvocatoria(insc.Convocatorias[0].Id.Valor, usuario, true, false, T0);

        var r = sesion.Iniciar(T0);

        Assert.Equal(EstadoSesion.Iniciada, sesion.Estado);
        Assert.NotEqual(ResultadoInicio.Cancelada, r);
    }
}
