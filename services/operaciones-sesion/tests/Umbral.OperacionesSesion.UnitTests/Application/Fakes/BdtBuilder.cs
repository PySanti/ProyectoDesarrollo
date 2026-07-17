using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;

namespace Umbral.OperacionesSesion.UnitTests.Application.Fakes;

public static class BdtBuilder
{
    private static readonly DateTime T0 = new(2026, 6, 28, 10, 0, 0, DateTimeKind.Utc);
    private const int PuntajeEtapa = 50;

    /// <summary>
    /// Construye una sesión BDT publicada (en Lobby) con las etapas indicadas y un jugador inscrito,
    /// sin iniciar. Devuelve la tupla (repo, uow, fake, partidaId).
    /// </summary>
    public static (FakeSesionPartidaRepository repo, FakeOperacionesSesionUnitOfWork uow,
        FakeSesionEventsPublisher fake, Guid partidaId)
        SesionEnLobbyConInscrito(params (string qr, int tiempoSeg)[] etapas)
    {
        var juegoId = Guid.NewGuid();
        var snapEtapas = etapas
            .Select((e, i) => new EtapaSnapshot(Guid.NewGuid(), i + 1, e.qr, PuntajeEtapa, e.tiempoSeg))
            .ToArray();
        var juego = new JuegoResumen(juegoId, 1, TipoJuego.BusquedaDelTesoro, "Área test", snapEtapas);
        var snap = new ConfiguracionSnapshot(
            "Copa BDT", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5,
            new[] { juego });

        var partidaId = Guid.NewGuid();
        var sesion = SesionPartida.Publicar(partidaId, snap);
        var jugadorId = Guid.NewGuid();
        var insc = sesion.Inscribir(jugadorId, false, 0, T0);
        sesion.AceptarInscripcion(insc.Id.Valor, 0, T0); // HU-19: aceptar para inscripción activa

        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var uow = new FakeOperacionesSesionUnitOfWork();
        var fake = new FakeSesionEventsPublisher();

        return (repo, uow, fake, partidaId);
    }

    /// <summary>
    /// Construye una sesión BDT iniciada con las etapas indicadas (qr, tiempoLimiteSegundos),
    /// un jugador inscrito, y la devuelve en la tupla con los fakes de repo/uow/publisher.
    /// </summary>
    public static (FakeSesionPartidaRepository repo, FakeOperacionesSesionUnitOfWork uow,
        FakeSesionEventsPublisher fake, Guid partidaId, Guid jugadorId)
        SesionIniciada(params (string qr, int tiempoSeg)[] etapas)
    {
        var juegoId = Guid.NewGuid();
        var snapEtapas = etapas
            .Select((e, i) => new EtapaSnapshot(Guid.NewGuid(), i + 1, e.qr, PuntajeEtapa, e.tiempoSeg))
            .ToArray();
        var juego = new JuegoResumen(juegoId, 1, TipoJuego.BusquedaDelTesoro, "Área test", snapEtapas);
        var snap = new ConfiguracionSnapshot(
            "Copa BDT", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5,
            new[] { juego });

        var partidaId = Guid.NewGuid();
        var sesion = SesionPartida.Publicar(partidaId, snap);
        var jugadorId = Guid.NewGuid();
        var insc = sesion.Inscribir(jugadorId, false, 0, T0);
        sesion.AceptarInscripcion(insc.Id.Valor, 0, T0); // HU-19: aceptar para que cuente en mínimos
        sesion.Iniciar(T0);

        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var uow = new FakeOperacionesSesionUnitOfWork();
        var fake = new FakeSesionEventsPublisher();

        return (repo, uow, fake, partidaId, jugadorId);
    }
}
