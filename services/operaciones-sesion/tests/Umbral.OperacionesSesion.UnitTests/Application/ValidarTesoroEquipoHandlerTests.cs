using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Domain.Abstractions;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class ValidarTesoroEquipoHandlerTests
{
    private static readonly DateTime T0 = new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);

    private sealed class TextoQrDecoder : IQrDecoder
    {
        public string? Decodificar(byte[] imagen) =>
            imagen.Length == 0 ? null : Encoding.UTF8.GetString(imagen);
    }

    private static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s));

    private static SesionPartida SesionBdtEquipoIniciada(out Guid liderA, out Guid equipoA)
    {
        var liderALocal = Guid.NewGuid();
        var equipoALocal = Guid.NewGuid();
        var etapas = new[] { new EtapaSnapshot(Guid.NewGuid(), 1, "QR-1", 50, 60) };
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, "Patio", etapas);
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap);
        var ins = sesion.PreinscribirEquipo(equipoALocal, true, new[] { liderALocal }, false, 0, T0);
        sesion.AceptarInscripcion(ins.Id.Valor, 0, T0); // HU-19: aceptar crea las convocatorias
        sesion.ResponderConvocatoria(ins.Convocatorias.Single(c => c.UsuarioId == liderALocal).Id.Valor, liderALocal, true, false, T0);
        sesion.Iniciar(T0);
        liderA = liderALocal; equipoA = equipoALocal;
        return sesion;
    }

    [Fact]
    public async Task En_equipo_los_eventos_bdt_portan_el_equipo()
    {
        var sesion = SesionBdtEquipoIniciada(out var liderA, out var equipoA);
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var uow = new FakeOperacionesSesionUnitOfWork();
        var events = new FakeSesionEventsPublisher();
        var handler = new ValidarTesoroCommandHandler(repo, uow, events, new FakeTimeProvider(T0.AddSeconds(5)), new TextoQrDecoder());

        await handler.Handle(new ValidarTesoroCommand(sesion.PartidaId, liderA, B64("QR-1")), CancellationToken.None);

        var validado = events.TesorosValidados.Single();
        Assert.Equal(equipoA, validado.EquipoId);
        Assert.Equal(liderA, validado.ParticipanteId);
        var ganada = events.EtapasGanadas.Single();
        Assert.Equal(equipoA, ganada.EquipoId);
        var cerrada = events.EtapasCerradas.Single();
        Assert.Equal(equipoA, cerrada.GanadorEquipoId);
        Assert.Equal(liderA, cerrada.GanadorParticipanteId);
    }

    [Fact]
    public async Task En_individual_los_eventos_bdt_llevan_equipo_null()
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
        var uow = new FakeOperacionesSesionUnitOfWork();
        var events = new FakeSesionEventsPublisher();
        var handler = new ValidarTesoroCommandHandler(repo, uow, events, new FakeTimeProvider(T0.AddSeconds(5)), new TextoQrDecoder());

        await handler.Handle(new ValidarTesoroCommand(sesion.PartidaId, jugador, B64("QR-1")), CancellationToken.None);

        Assert.Null(events.TesorosValidados.Single().EquipoId);
        Assert.Null(events.EtapasGanadas.Single().EquipoId);
        Assert.Null(events.EtapasCerradas.Single().GanadorEquipoId);
    }
}
