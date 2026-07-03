using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class CancelarInscripcionCommandHandlerTests
{
    private static SesionPartida PublishedSession(Guid partidaId)
    {
        var snapshot = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5,
            new[] { new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia) });
        return SesionPartida.Publicar(partidaId, snapshot);
    }

    [Fact]
    public async Task Cancels_active_inscription_and_saves()
    {
        var partidaId = Guid.NewGuid();
        var participante = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository();
        var sesion = PublishedSession(partidaId);
        sesion.Inscribir(participante, false, 0, DateTime.UtcNow);
        repo.Add(sesion);
        var uow = new FakeOperacionesSesionUnitOfWork();
        var handler = new CancelarInscripcionCommandHandler(repo, uow);

        await handler.Handle(new CancelarInscripcionCommand(partidaId, participante), CancellationToken.None);

        Assert.Equal(EstadoInscripcion.Cancelada, repo.Store[partidaId].Inscripciones.Single().Estado);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task Throws_when_session_not_found()
    {
        var handler = new CancelarInscripcionCommandHandler(
            new FakeSesionPartidaRepository(), new FakeOperacionesSesionUnitOfWork());

        await Assert.ThrowsAsync<SesionNoEncontradaException>(
            () => handler.Handle(new CancelarInscripcionCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Throws_when_no_active_inscription()
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository();
        repo.Add(PublishedSession(partidaId));
        var handler = new CancelarInscripcionCommandHandler(repo, new FakeOperacionesSesionUnitOfWork());

        await Assert.ThrowsAsync<InscripcionNoEncontradaException>(
            () => handler.Handle(new CancelarInscripcionCommand(partidaId, Guid.NewGuid()), CancellationToken.None));
    }
}
