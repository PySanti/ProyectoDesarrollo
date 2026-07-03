using System;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class EnviarPistaCommandHandlerTests
{
    private static readonly DateTime T0 = new(2026, 6, 28, 10, 0, 5, DateTimeKind.Utc);

    [Fact]
    public async Task Publica_pista_enviada_con_campos_correctos()
    {
        var (repo, _, fake, partidaId, jugador) = BdtBuilder.SesionIniciada(("QR-1", 60));
        var handler = new EnviarPistaCommandHandler(repo, fake, new FakeTimeProvider(T0));

        var resp = await handler.Handle(new EnviarPistaCommand(partidaId, jugador, "Mira el faro"), default);

        var evt = Assert.Single(fake.PistasEnviadas);
        Assert.Equal(partidaId, evt.PartidaId);
        Assert.Equal(jugador, evt.ParticipanteDestinoId);
        Assert.Equal("Mira el faro", evt.Texto);
        Assert.Equal(T0, evt.Instante);
        Assert.NotEqual(Guid.Empty, evt.JuegoId);
        Assert.Equal(partidaId, resp.PartidaId);
        Assert.Equal(jugador, resp.ParticipanteDestinoId);
        Assert.Equal(evt.JuegoId, resp.JuegoId);
        Assert.Equal(T0, resp.TimestampUtc);
    }

    [Fact]
    public async Task Destino_no_inscrito_propaga_sin_publicar()
    {
        var (repo, _, fake, partidaId, _) = BdtBuilder.SesionIniciada(("QR-1", 60));
        var handler = new EnviarPistaCommandHandler(repo, fake, new FakeTimeProvider(T0));

        await Assert.ThrowsAsync<ParticipanteNoInscritoException>(
            () => handler.Handle(new EnviarPistaCommand(partidaId, Guid.NewGuid(), "x"), default));

        Assert.Empty(fake.PistasEnviadas);
    }

    [Fact]
    public async Task Sesion_inexistente_lanza()
    {
        var repo = new FakeSesionPartidaRepository();
        var fake = new FakeSesionEventsPublisher();
        var handler = new EnviarPistaCommandHandler(repo, fake, new FakeTimeProvider(T0));

        await Assert.ThrowsAsync<SesionNoEncontradaException>(
            () => handler.Handle(new EnviarPistaCommand(Guid.NewGuid(), Guid.NewGuid(), "x"), default));
    }
}
