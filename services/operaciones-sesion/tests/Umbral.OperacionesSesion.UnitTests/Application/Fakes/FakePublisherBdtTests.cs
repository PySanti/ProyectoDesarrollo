using Umbral.OperacionesSesion.Application.Interfaces;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application.Fakes;

public class FakePublisherBdtTests
{
    [Fact]
    public async Task Fake_records_bdt_events()
    {
        var fake = new FakeSesionEventsPublisher();
        var pid = Guid.NewGuid(); var sid = Guid.NewGuid(); var jid = Guid.NewGuid(); var eid = Guid.NewGuid();
        await fake.PublicarTesoroQRValidadoAsync(
            new TesoroQRValidadoEvent(pid, sid, jid, eid, Guid.NewGuid(), "Valido", DateTime.UtcNow), default);
        await fake.PublicarEtapaBDTGanadaAsync(
            new EtapaBDTGanadaEvent(pid, sid, jid, eid, Guid.NewGuid(), 50, 1234), default);
        await fake.PublicarEtapaBDTCerradaAsync(
            new EtapaBDTCerradaEvent(pid, sid, jid, eid, "Ganador", DateTime.UtcNow, Guid.NewGuid()), default);
        await fake.PublicarEtapaBDTActivadaAsync(
            new EtapaBDTActivadaEvent(pid, sid, jid, eid, 1, 60, DateTime.UtcNow), default);
        Assert.Single(fake.TesorosValidados);
        Assert.Single(fake.EtapasGanadas);
        Assert.Single(fake.EtapasCerradas);
        Assert.Single(fake.EtapasActivadas);
    }
}
