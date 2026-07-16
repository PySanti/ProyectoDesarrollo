using System;
using System.Linq;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Results;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

// Preinscribir el equipo ES la declaracion de intencion del lider (HU-15): no tiene que ademas
// convocarse a si mismo. Antes de esto, un equipo de solo el lider no podia arrancar nunca — su
// unica convocatoria, dirigida a el mismo, se quedaba Pendiente, el equipo contaba 0 para el
// minimo y AplicarInicio cancelaba la sesion entera.
public class AutoaceptarConvocatoriaLiderTests
{
    private static readonly DateTime T0 = new(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);

    private static SesionPartida SesionEquipoEnLobby(int minimos)
    {
        var pregunta = new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 60,
            new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true), new OpcionSnapshot(Guid.NewGuid(), "no", false) });
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { pregunta });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, minimos, 5, new[] { juego });
        return SesionPartida.Publicar(Guid.NewGuid(), snap);
    }

    [Fact]
    public void Equipo_de_solo_el_lider_inicia_la_partida_sin_pasos_extra()
    {
        var sesion = SesionEquipoEnLobby(minimos: 1);
        var lider = Guid.NewGuid();

        var insc = sesion.PreinscribirEquipo(Guid.NewGuid(), true, lider, new[] { lider }, false, 0, T0);
        sesion.AceptarInscripcion(insc.Id.Valor, 0, T0, liderPuedeAutoAceptar: true);

        var resultado = sesion.Iniciar(T0.AddSeconds(5));

        Assert.Equal(TipoResultadoInicio.Iniciada, resultado.Tipo);
        Assert.Equal(EstadoSesion.Iniciada, sesion.Estado);
    }

    [Fact]
    public void La_convocatoria_del_lider_nace_aceptada_y_la_del_resto_pendiente()
    {
        var sesion = SesionEquipoEnLobby(minimos: 1);
        var lider = Guid.NewGuid();
        var miembro = Guid.NewGuid();

        var insc = sesion.PreinscribirEquipo(Guid.NewGuid(), true, lider, new[] { lider, miembro }, false, 0, T0);
        sesion.AceptarInscripcion(insc.Id.Valor, 0, T0, liderPuedeAutoAceptar: true);

        Assert.True(insc.Convocatorias.Single(c => c.UsuarioId == lider).EstaAceptada);
        Assert.True(insc.Convocatorias.Single(c => c.UsuarioId == miembro).EstaPendiente);
    }

    // BR-G09: auto-aceptar no puede saltarse el guard que el camino manual si aplica. Si el lider
    // ya participa en otra partida, su convocatoria se queda Pendiente — el mismo estado en que
    // quedaria si intentara aceptarla a mano. El equipo puede jugar igual si otro miembro acepta.
    [Fact]
    public void Si_el_lider_ya_participa_en_otra_partida_su_convocatoria_queda_pendiente()
    {
        var sesion = SesionEquipoEnLobby(minimos: 1);
        var lider = Guid.NewGuid();

        var insc = sesion.PreinscribirEquipo(Guid.NewGuid(), true, lider, new[] { lider }, false, 0, T0);
        sesion.AceptarInscripcion(insc.Id.Valor, 0, T0, liderPuedeAutoAceptar: false);

        Assert.True(insc.Convocatorias.Single(c => c.UsuarioId == lider).EstaPendiente);
    }

    [Fact]
    public void Sin_convocatorias_aceptadas_la_sesion_sigue_cancelandose_bajo_minimos()
    {
        var sesion = SesionEquipoEnLobby(minimos: 1);
        var lider = Guid.NewGuid();

        var insc = sesion.PreinscribirEquipo(Guid.NewGuid(), true, lider, new[] { lider }, false, 0, T0);
        sesion.AceptarInscripcion(insc.Id.Valor, 0, T0, liderPuedeAutoAceptar: false);

        var resultado = sesion.Iniciar(T0.AddSeconds(5));

        Assert.Equal(TipoResultadoInicio.Cancelada, resultado.Tipo);
        Assert.Equal(EstadoSesion.Cancelada, sesion.Estado);
    }
}
