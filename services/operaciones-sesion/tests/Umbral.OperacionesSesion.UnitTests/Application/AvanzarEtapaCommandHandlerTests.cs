using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class AvanzarEtapaCommandHandlerTests
{
    [Fact]
    public async Task Advance_to_next_emits_cerrada_and_activada()
    {
        var (repo, uow, fake, partidaId, _) = BdtBuilder.SesionIniciada(("QR-1", 60), ("QR-2", 60));
        var handler = new AvanzarEtapaCommandHandler(repo, uow, fake, new FakeTimeProvider(new DateTime(2026, 6, 28, 10, 0, 5)));
        var resp = await handler.Handle(new AvanzarEtapaCommand(partidaId), default);
        Assert.False(resp.SinMasEtapas);
        Assert.Equal(2, resp.EtapaActivadaOrden);
        Assert.Single(fake.EtapasCerradas);
        Assert.Single(fake.EtapasActivadas);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task Advance_on_last_stage_has_no_next()
    {
        var (repo, uow, fake, partidaId, _) = BdtBuilder.SesionIniciada(("QR-1", 60));
        var handler = new AvanzarEtapaCommandHandler(repo, uow, fake, new FakeTimeProvider(new DateTime(2026, 6, 28, 10, 0, 5)));
        var resp = await handler.Handle(new AvanzarEtapaCommand(partidaId), default);
        Assert.True(resp.SinMasEtapas);
        Assert.Single(fake.EtapasCerradas);
        Assert.Empty(fake.EtapasActivadas);
    }
}
