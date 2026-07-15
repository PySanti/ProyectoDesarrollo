using System;
using System.Linq;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class ResponderConvocatoriaGuardTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    private static SesionPartida SesionEquipoLobbyConUsuarioEnDosEquipos(
        Guid usuario, out Guid convocatoriaA, out Guid convocatoriaB)
    {
        var pregunta = new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 60,
            new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true), new OpcionSnapshot(Guid.NewGuid(), "no", false) });
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { pregunta });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap);

        var liderA = Guid.NewGuid(); var liderB = Guid.NewGuid();
        var insA = sesion.PreinscribirEquipo(Guid.NewGuid(), true, new[] { liderA, usuario }, false, 0, T0);
        var insB = sesion.PreinscribirEquipo(Guid.NewGuid(), true, new[] { liderB, usuario }, false, 1, T0);
        sesion.AceptarInscripcion(insA.Id.Valor, 0, T0); // HU-19: aceptar crea las convocatorias
        sesion.AceptarInscripcion(insB.Id.Valor, 1, T0);
        convocatoriaA = insA.Convocatorias.Single(c => c.UsuarioId == usuario).Id.Valor;
        convocatoriaB = insB.Convocatorias.Single(c => c.UsuarioId == usuario).Id.Valor;
        return sesion;
    }

    [Fact]
    public void Aceptar_segunda_convocatoria_en_la_misma_sesion_lanza_participacion_activa()
    {
        var usuario = Guid.NewGuid();
        var sesion = SesionEquipoLobbyConUsuarioEnDosEquipos(usuario, out var convA, out var convB);

        sesion.ResponderConvocatoria(convA, usuario, aceptar: true, false, T0);

        Assert.Throws<ParticipacionActivaExistenteException>(
            () => sesion.ResponderConvocatoria(convB, usuario, aceptar: true, false, T0.AddSeconds(1)));
    }

    [Fact]
    public void Aceptar_tras_rechazar_la_otra_es_valido()
    {
        var usuario = Guid.NewGuid();
        var sesion = SesionEquipoLobbyConUsuarioEnDosEquipos(usuario, out var convA, out var convB);

        sesion.ResponderConvocatoria(convA, usuario, aceptar: false, false, T0);
        var c = sesion.ResponderConvocatoria(convB, usuario, aceptar: true, false, T0.AddSeconds(1));

        Assert.True(c.EstaAceptada);
    }

    [Fact]
    public void Rechazar_no_esta_bloqueado_por_una_aceptada_previa()
    {
        var usuario = Guid.NewGuid();
        var sesion = SesionEquipoLobbyConUsuarioEnDosEquipos(usuario, out var convA, out var convB);

        sesion.ResponderConvocatoria(convA, usuario, aceptar: true, false, T0);
        var c = sesion.ResponderConvocatoria(convB, usuario, aceptar: false, false, T0.AddSeconds(1));

        Assert.False(c.EstaAceptada);
    }
}
