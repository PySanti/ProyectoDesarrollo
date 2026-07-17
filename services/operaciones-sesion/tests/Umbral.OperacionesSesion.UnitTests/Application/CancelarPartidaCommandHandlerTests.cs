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

public class CancelarPartidaCommandHandlerTests
{
    private static readonly DateTime T0 = new(2026, 7, 12, 12, 0, 0, DateTimeKind.Utc);

    private static SesionPartida SesionEnLobby(Guid partidaId)
    {
        var lista = new[] { new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia) }.ToList();
        var snapshot = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, lista);
        return SesionPartida.Publicar(partidaId, snapshot);
    }

    private static (CancelarPartidaCommandHandler handler, FakeSesionPartidaRepository repo, FakeOperacionesSesionUnitOfWork uow, FakeSesionEventsPublisher events) Build()
    {
        var repo = new FakeSesionPartidaRepository();
        var uow = new FakeOperacionesSesionUnitOfWork();
        var events = new FakeSesionEventsPublisher();
        var time = new FakeTimeProvider(T0);
        return (new CancelarPartidaCommandHandler(repo, uow, events, time), repo, uow, events);
    }

    [Fact]
    public async Task Cancelar_desde_lobby_guarda_y_publica_evento_con_motivo_operador()
    {
        var partidaId = Guid.NewGuid();
        var (handler, repo, uow, events) = Build();
        repo.Add(SesionEnLobby(partidaId));

        var response = await handler.Handle(new CancelarPartidaCommand(partidaId), CancellationToken.None);

        Assert.Equal(partidaId, response.PartidaId);
        Assert.Equal("Cancelada", response.Estado);
        Assert.Equal(1, uow.SaveCount);
        Assert.Single(events.PartidasCanceladas);
        Assert.Equal("CanceladaPorOperador", events.PartidasCanceladas[0].Motivo);
        Assert.Equal(partidaId, events.PartidasCanceladas[0].PartidaId);
    }

    [Fact]
    public async Task Cancelar_sesion_inexistente_lanza_404()
    {
        var (handler, _, _, _) = Build();
        await Assert.ThrowsAsync<SesionNoEncontradaException>(
            () => handler.Handle(new CancelarPartidaCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Cancelar_estado_terminal_propaga_PartidaNoCancelableException()
    {
        var partidaId = Guid.NewGuid();
        var (handler, repo, _, _) = Build();
        var sesion = SesionEnLobby(partidaId);
        sesion.Cancelar(T0); // now Cancelada
        repo.Add(sesion);

        await Assert.ThrowsAsync<PartidaNoCancelableException>(
            () => handler.Handle(new CancelarPartidaCommand(partidaId), CancellationToken.None));
    }
}
