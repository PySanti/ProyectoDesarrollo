using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class InscribirParticipanteCommandHandlerTests
{
    private static readonly TimeProvider Clock = TimeProvider.System;

    private static SesionPartida PublishedSession(Guid partidaId, Modalidad modalidad = Modalidad.Individual, int max = 2)
    {
        var snapshot = new ConfiguracionSnapshot("Copa", modalidad, ModoInicioPartida.Manual, null, 1, max,
            new[] { new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia) });
        return SesionPartida.Publicar(partidaId, snapshot);
    }

    [Fact]
    public async Task Inscribes_participant_and_saves()
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository();
        repo.Add(PublishedSession(partidaId));
        var uow = new FakeOperacionesSesionUnitOfWork();
        var handler = new InscribirParticipanteCommandHandler(repo, uow, Clock);
        var participante = Guid.NewGuid();

        var response = await handler.Handle(new InscribirParticipanteCommand(partidaId, participante), CancellationToken.None);

        Assert.Equal(participante, response.ParticipanteId);
        Assert.Equal(partidaId, response.PartidaId);
        Assert.NotEqual(Guid.Empty, response.InscripcionId);
        Assert.Equal(1, uow.SaveCount);
        Assert.Single(repo.Store[partidaId].Inscripciones);
    }

    [Fact]
    public async Task Throws_when_session_not_found()
    {
        var handler = new InscribirParticipanteCommandHandler(
            new FakeSesionPartidaRepository(), new FakeOperacionesSesionUnitOfWork(), Clock);

        await Assert.ThrowsAsync<SesionNoEncontradaException>(
            () => handler.Handle(new InscribirParticipanteCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Throws_when_modalidad_is_equipo()
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository();
        repo.Add(PublishedSession(partidaId, Modalidad.Equipo));
        var handler = new InscribirParticipanteCommandHandler(repo, new FakeOperacionesSesionUnitOfWork(), Clock);

        await Assert.ThrowsAsync<ModalidadNoSoportadaException>(
            () => handler.Handle(new InscribirParticipanteCommand(partidaId, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Throws_when_participant_active_elsewhere()
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository { ParticipacionActivaEnOtra = true };
        repo.Add(PublishedSession(partidaId));
        var handler = new InscribirParticipanteCommandHandler(repo, new FakeOperacionesSesionUnitOfWork(), Clock);

        await Assert.ThrowsAsync<ParticipacionActivaExistenteException>(
            () => handler.Handle(new InscribirParticipanteCommand(partidaId, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Throws_when_capacity_full()
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository();
        var sesion = PublishedSession(partidaId, max: 1);
        sesion.Inscribir(Guid.NewGuid(), false, 0, DateTime.UtcNow); // fill the single slot
        repo.Add(sesion);
        var handler = new InscribirParticipanteCommandHandler(repo, new FakeOperacionesSesionUnitOfWork(), Clock);

        await Assert.ThrowsAsync<CupoLlenoException>(
            () => handler.Handle(new InscribirParticipanteCommand(partidaId, Guid.NewGuid()), CancellationToken.None));
    }
}
