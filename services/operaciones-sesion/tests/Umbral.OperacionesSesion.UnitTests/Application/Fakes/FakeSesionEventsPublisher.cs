using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Interfaces;

namespace Umbral.OperacionesSesion.UnitTests.Application.Fakes;

public sealed class FakeSesionEventsPublisher : ISesionEventsPublisher
{
    public PartidaPublicadaEnLobbyEvent? LastEvent { get; private set; }
    public int PublishCount { get; private set; }
    public List<PartidaIniciadaEvent> PartidasIniciadas { get; } = new();
    public List<JuegoActivadoEvent> JuegosActivados { get; } = new();
    public List<PartidaCanceladaEvent> PartidasCanceladas { get; } = new();
    public List<PartidaFinalizadaEvent> PartidasFinalizadas { get; } = new();

    public Task PublicarPartidaPublicadaEnLobbyAsync(PartidaPublicadaEnLobbyEvent evento, CancellationToken cancellationToken)
    {
        LastEvent = evento;
        PublishCount++;
        return Task.CompletedTask;
    }

    public Task PublicarPartidaIniciadaAsync(PartidaIniciadaEvent evento, CancellationToken cancellationToken)
    {
        PartidasIniciadas.Add(evento);
        return Task.CompletedTask;
    }

    public Task PublicarJuegoActivadoAsync(JuegoActivadoEvent evento, CancellationToken cancellationToken)
    {
        JuegosActivados.Add(evento);
        return Task.CompletedTask;
    }

    public Task PublicarPartidaCanceladaAsync(PartidaCanceladaEvent evento, CancellationToken cancellationToken)
    {
        PartidasCanceladas.Add(evento);
        return Task.CompletedTask;
    }

    public Task PublicarPartidaFinalizadaAsync(PartidaFinalizadaEvent evento, CancellationToken cancellationToken)
    {
        PartidasFinalizadas.Add(evento);
        return Task.CompletedTask;
    }

    public List<RespuestaTriviaValidadaEvent> RespuestasValidadas { get; } = new();
    public List<PuntajeTriviaIncrementadoEvent> PuntajesIncrementados { get; } = new();
    public List<PreguntaTriviaActivadaEvent> PreguntasActivadas { get; } = new();
    public List<PreguntaTriviaCerradaEvent> PreguntasCerradas { get; } = new();

    public Task PublicarRespuestaTriviaValidadaAsync(RespuestaTriviaValidadaEvent evento, CancellationToken cancellationToken)
    { RespuestasValidadas.Add(evento); return Task.CompletedTask; }
    public Task PublicarPuntajeTriviaIncrementadoAsync(PuntajeTriviaIncrementadoEvent evento, CancellationToken cancellationToken)
    { PuntajesIncrementados.Add(evento); return Task.CompletedTask; }
    public Task PublicarPreguntaTriviaActivadaAsync(PreguntaTriviaActivadaEvent evento, CancellationToken cancellationToken)
    { PreguntasActivadas.Add(evento); return Task.CompletedTask; }
    public Task PublicarPreguntaTriviaCerradaAsync(PreguntaTriviaCerradaEvent evento, CancellationToken cancellationToken)
    { PreguntasCerradas.Add(evento); return Task.CompletedTask; }

    public List<TesoroQRValidadoEvent> TesorosValidados { get; } = new();
    public List<EtapaBDTGanadaEvent> EtapasGanadas { get; } = new();
    public List<EtapaBDTCerradaEvent> EtapasCerradas { get; } = new();
    public List<EtapaBDTActivadaEvent> EtapasActivadas { get; } = new();

    public Task PublicarTesoroQRValidadoAsync(TesoroQRValidadoEvent evento, CancellationToken cancellationToken)
    { TesorosValidados.Add(evento); return Task.CompletedTask; }
    public Task PublicarEtapaBDTGanadaAsync(EtapaBDTGanadaEvent evento, CancellationToken cancellationToken)
    { EtapasGanadas.Add(evento); return Task.CompletedTask; }
    public Task PublicarEtapaBDTCerradaAsync(EtapaBDTCerradaEvent evento, CancellationToken cancellationToken)
    { EtapasCerradas.Add(evento); return Task.CompletedTask; }
    public Task PublicarEtapaBDTActivadaAsync(EtapaBDTActivadaEvent evento, CancellationToken cancellationToken)
    { EtapasActivadas.Add(evento); return Task.CompletedTask; }

    public List<PistaEnviadaEvent> PistasEnviadas { get; } = new();

    public Task PublicarPistaEnviadaAsync(PistaEnviadaEvent evento, CancellationToken cancellationToken)
    { PistasEnviadas.Add(evento); return Task.CompletedTask; }

    public List<ConvocatoriaCreadaEvent> ConvocatoriasCreadas { get; } = new();
    public List<ConvocatoriaRespondidaEvent> ConvocatoriasRespondidas { get; } = new();

    public Task PublicarConvocatoriaCreadaAsync(ConvocatoriaCreadaEvent evento, CancellationToken cancellationToken)
    { ConvocatoriasCreadas.Add(evento); return Task.CompletedTask; }
    public Task PublicarConvocatoriaRespondidaAsync(ConvocatoriaRespondidaEvent evento, CancellationToken cancellationToken)
    { ConvocatoriasRespondidas.Add(evento); return Task.CompletedTask; }
}
