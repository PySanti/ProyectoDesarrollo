using System;
using System.Threading;
using System.Threading.Tasks;
using Umbral.Partidas.Application.Commands;
using Umbral.Partidas.Application.Handlers.Commands;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.Domain.ValueObjects;
using Umbral.Partidas.UnitTests.Application.Fakes;

namespace Umbral.Partidas.UnitTests.Application;

public class ProyectarEstadoPartidaCommandHandlerTests
{
    private static readonly DateTime T0 = new(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc);

    private static Partida NewPartida()
        => Partida.Crear(NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10, T0);

    [Fact]
    public async Task Handle_proyecta_el_estado_de_runtime_y_guarda()
    {
        var partidas = new FakePartidaRepository();
        var uow = new FakePartidasUnitOfWork();
        var partida = NewPartida();
        partidas.Add(partida);

        var handler = new ProyectarEstadoPartidaCommandHandler(partidas, uow);
        await handler.Handle(
            new ProyectarEstadoPartidaCommand(partida.PartidaId.Valor, EstadoPartida.Cancelada), CancellationToken.None);

        Assert.Equal(EstadoPartida.Cancelada, partidas.Store[partida.PartidaId.Valor].Estado);
        Assert.Equal(1, uow.SaveCount);
    }

    // Best-effort (ADR-0012): un evento de una partida que este servicio no conoce no puede tumbar
    // el consumidor. No-op sin guardar en vez de lanzar.
    [Fact]
    public async Task Handle_partida_inexistente_no_lanza_ni_guarda()
    {
        var partidas = new FakePartidaRepository();
        var uow = new FakePartidasUnitOfWork();

        var handler = new ProyectarEstadoPartidaCommandHandler(partidas, uow);
        await handler.Handle(
            new ProyectarEstadoPartidaCommand(Guid.NewGuid(), EstadoPartida.Iniciada), CancellationToken.None);

        Assert.Equal(0, uow.SaveCount);
    }
}
