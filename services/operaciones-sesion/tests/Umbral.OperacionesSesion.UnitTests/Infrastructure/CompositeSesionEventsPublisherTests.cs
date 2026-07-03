using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Infrastructure.Services;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Infrastructure;

public class CompositeSesionEventsPublisherTests
{
    private static readonly DateTime T0 = new(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);
    private static PartidaFinalizadaEvent Evt() => new(Guid.NewGuid(), Guid.NewGuid(), T0);

    [Fact]
    public async Task Fan_out_invoca_a_todos()
    {
        var a = new RecordingPublisher();
        var b = new RecordingPublisher();
        var sut = new CompositeSesionEventsPublisher(new ISesionEventsPublisher[] { a, b }, NullLogger<CompositeSesionEventsPublisher>.Instance);

        await sut.PublicarPartidaFinalizadaAsync(Evt(), CancellationToken.None);

        Assert.Equal(1, a.Finalizadas);
        Assert.Equal(1, b.Finalizadas);
    }

    [Fact]
    public async Task Publicador_que_lanza_no_detiene_a_los_demas_ni_propaga()
    {
        var malo = new ThrowingPublisher();
        var bueno = new RecordingPublisher();
        var sut = new CompositeSesionEventsPublisher(new ISesionEventsPublisher[] { malo, bueno }, NullLogger<CompositeSesionEventsPublisher>.Instance);

        await sut.PublicarPartidaFinalizadaAsync(Evt(), CancellationToken.None); // no debe lanzar

        Assert.Equal(1, bueno.Finalizadas);
    }

    [Fact]
    public async Task OperationCanceledException_se_propaga()
    {
        var cancela = new CancelingPublisher();
        var bueno = new RecordingPublisher();
        var sut = new CompositeSesionEventsPublisher(new ISesionEventsPublisher[] { cancela, bueno }, NullLogger<CompositeSesionEventsPublisher>.Instance);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => sut.PublicarPartidaFinalizadaAsync(Evt(), CancellationToken.None));
    }

    [Fact]
    public async Task Pista_fan_out_invoca_a_todos()
    {
        var a = new RecordingPublisher();
        var b = new RecordingPublisher();
        var sut = new CompositeSesionEventsPublisher(new ISesionEventsPublisher[] { a, b }, NullLogger<CompositeSesionEventsPublisher>.Instance);

        await sut.PublicarPistaEnviadaAsync(
            new PistaEnviadaEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "pista", T0),
            CancellationToken.None);

        Assert.Equal(1, a.Pistas);
        Assert.Equal(1, b.Pistas);
    }

    [Fact]
    public async Task Convocatoria_fan_out_invoca_a_todos()
    {
        var a = new RecordingPublisher();
        var b = new RecordingPublisher();
        var sut = new CompositeSesionEventsPublisher(new ISesionEventsPublisher[] { a, b }, NullLogger<CompositeSesionEventsPublisher>.Instance);

        await sut.PublicarConvocatoriaCreadaAsync(
            new ConvocatoriaCreadaEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);
        await sut.PublicarConvocatoriaRespondidaAsync(
            new ConvocatoriaRespondidaEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Rechazada"),
            CancellationToken.None);

        Assert.Equal(1, a.ConvocatoriasCreadas);
        Assert.Equal(1, b.ConvocatoriasCreadas);
        Assert.Equal(1, a.ConvocatoriasRespondidas);
        Assert.Equal(1, b.ConvocatoriasRespondidas);
    }

    private class RecordingPublisher : NoOpBase
    {
        public int Finalizadas;
        public override Task PublicarPartidaFinalizadaAsync(PartidaFinalizadaEvent e, CancellationToken ct)
        { Finalizadas++; return Task.CompletedTask; }

        public int Pistas;
        public override Task PublicarPistaEnviadaAsync(PistaEnviadaEvent e, CancellationToken ct)
        { Pistas++; return Task.CompletedTask; }

        public int ConvocatoriasCreadas;
        public int ConvocatoriasRespondidas;
        public override Task PublicarConvocatoriaCreadaAsync(ConvocatoriaCreadaEvent e, CancellationToken ct)
        { ConvocatoriasCreadas++; return Task.CompletedTask; }
        public override Task PublicarConvocatoriaRespondidaAsync(ConvocatoriaRespondidaEvent e, CancellationToken ct)
        { ConvocatoriasRespondidas++; return Task.CompletedTask; }
    }

    private sealed class ThrowingPublisher : NoOpBase
    {
        public override Task PublicarPartidaFinalizadaAsync(PartidaFinalizadaEvent e, CancellationToken ct)
            => throw new InvalidOperationException("boom");
    }

    private sealed class CancelingPublisher : NoOpBase
    {
        public override Task PublicarPartidaFinalizadaAsync(PartidaFinalizadaEvent e, CancellationToken ct)
            => throw new OperationCanceledException();
    }

    // Base que implementa los 13 métodos como no-op; los tests sólo overridean PartidaFinalizada.
    private abstract class NoOpBase : ISesionEventsPublisher
    {
        public virtual Task PublicarPartidaPublicadaEnLobbyAsync(PartidaPublicadaEnLobbyEvent e, CancellationToken ct) => Task.CompletedTask;
        public virtual Task PublicarPartidaIniciadaAsync(PartidaIniciadaEvent e, CancellationToken ct) => Task.CompletedTask;
        public virtual Task PublicarJuegoActivadoAsync(JuegoActivadoEvent e, CancellationToken ct) => Task.CompletedTask;
        public virtual Task PublicarPartidaCanceladaAsync(PartidaCanceladaEvent e, CancellationToken ct) => Task.CompletedTask;
        public virtual Task PublicarPartidaFinalizadaAsync(PartidaFinalizadaEvent e, CancellationToken ct) => Task.CompletedTask;
        public virtual Task PublicarRespuestaTriviaValidadaAsync(RespuestaTriviaValidadaEvent e, CancellationToken ct) => Task.CompletedTask;
        public virtual Task PublicarPuntajeTriviaIncrementadoAsync(PuntajeTriviaIncrementadoEvent e, CancellationToken ct) => Task.CompletedTask;
        public virtual Task PublicarPreguntaTriviaActivadaAsync(PreguntaTriviaActivadaEvent e, CancellationToken ct) => Task.CompletedTask;
        public virtual Task PublicarPreguntaTriviaCerradaAsync(PreguntaTriviaCerradaEvent e, CancellationToken ct) => Task.CompletedTask;
        public virtual Task PublicarTesoroQRValidadoAsync(TesoroQRValidadoEvent e, CancellationToken ct) => Task.CompletedTask;
        public virtual Task PublicarEtapaBDTGanadaAsync(EtapaBDTGanadaEvent e, CancellationToken ct) => Task.CompletedTask;
        public virtual Task PublicarEtapaBDTCerradaAsync(EtapaBDTCerradaEvent e, CancellationToken ct) => Task.CompletedTask;
        public virtual Task PublicarEtapaBDTActivadaAsync(EtapaBDTActivadaEvent e, CancellationToken ct) => Task.CompletedTask;
        public virtual Task PublicarPistaEnviadaAsync(PistaEnviadaEvent e, CancellationToken ct) => Task.CompletedTask;
        public virtual Task PublicarConvocatoriaCreadaAsync(ConvocatoriaCreadaEvent e, CancellationToken ct) => Task.CompletedTask;
        public virtual Task PublicarConvocatoriaRespondidaAsync(ConvocatoriaRespondidaEvent e, CancellationToken ct) => Task.CompletedTask;
    }
}
