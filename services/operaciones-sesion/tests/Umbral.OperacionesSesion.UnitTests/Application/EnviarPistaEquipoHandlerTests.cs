using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class EnviarPistaEquipoHandlerTests
{
    private static readonly DateTime T0 = new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);

    private static SesionPartida SesionBdtEquipoIniciada(out Guid equipoA)
    {
        var lider = Guid.NewGuid();
        var equipoALocal = Guid.NewGuid();
        var etapas = new[] { new EtapaSnapshot(Guid.NewGuid(), 1, "QR-1", 50, 60) };
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, "Patio", etapas);
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap);
        var ins = sesion.PreinscribirEquipo(equipoALocal, true, lider, new[] { lider }, false, 0, T0);
        sesion.AceptarInscripcion(ins.Id.Valor, 0, T0); // HU-19: aceptar crea las convocatorias
        sesion.ResponderConvocatoria(ins.Convocatorias.Single().Id.Valor, lider, true, false, T0);
        sesion.Iniciar(T0);
        equipoA = equipoALocal;
        return sesion;
    }

    [Fact]
    public async Task Destino_equipo_publica_evento_con_equipo_y_participante_null()
    {
        var sesion = SesionBdtEquipoIniciada(out var equipoA);
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var events = new FakeSesionEventsPublisher();
        var handler = new EnviarPistaCommandHandler(repo, events, new FakeTimeProvider(T0.AddSeconds(5)));

        var resp = await handler.Handle(
            new EnviarPistaCommand(sesion.PartidaId, null, "Mira el mural norte", equipoA), CancellationToken.None);

        var evento = events.PistasEnviadas.Single();
        Assert.Equal(equipoA, evento.EquipoDestinoId);
        Assert.Null(evento.ParticipanteDestinoId);
        Assert.Equal("Mira el mural norte", evento.Texto);
        Assert.Equal(equipoA, resp.EquipoDestinoId);
        Assert.Null(resp.ParticipanteDestinoId);
        Assert.Equal(sesion.Juegos.Single().JuegoId, resp.JuegoId);
    }

    [Fact]
    public async Task Destino_participante_mantiene_flujo_individual_con_equipo_null()
    {
        var jugador = Guid.NewGuid();
        var etapas = new[] { new EtapaSnapshot(Guid.NewGuid(), 1, "QR-1", 50, 60) };
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, "Patio", etapas);
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10, new[] { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap);
        var inscJ = sesion.Inscribir(jugador, false, 0, T0);
        sesion.AceptarInscripcion(inscJ.Id.Valor, 0, T0); // HU-19: aceptar para que cuente en mínimos
        sesion.Iniciar(T0);
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var events = new FakeSesionEventsPublisher();
        var handler = new EnviarPistaCommandHandler(repo, events, new FakeTimeProvider(T0.AddSeconds(5)));

        var resp = await handler.Handle(
            new EnviarPistaCommand(sesion.PartidaId, jugador, "Mira el faro"), CancellationToken.None);

        var evento = events.PistasEnviadas.Single();
        Assert.Null(evento.EquipoDestinoId);
        Assert.Equal(jugador, evento.ParticipanteDestinoId);
        Assert.Null(resp.EquipoDestinoId);
        Assert.Equal(jugador, resp.ParticipanteDestinoId);
    }
}
