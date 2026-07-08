using Microsoft.Extensions.Logging;
using System.Text.Json;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Infrastructure.Services.Messaging;

namespace Umbral.OperacionesSesion.Infrastructure.Services;

public sealed class RabbitMqSesionEventsPublisher : ISesionEventsPublisher
{
    private readonly IRabbitMqPublishChannel _canal;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RabbitMqSesionEventsPublisher> _logger;

    public RabbitMqSesionEventsPublisher(IRabbitMqPublishChannel canal, TimeProvider timeProvider,
        ILogger<RabbitMqSesionEventsPublisher> logger)
    {
        _canal = canal;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    // Best-effort estricto: fallo de broker se loguea y NUNCA llega al caller (ADR-0012).
    private Task Publicar(string eventType, object payload)
    {
        try
        {
            var envelope = EventEnvelope.Create(eventType, payload, _timeProvider.GetUtcNow().UtcDateTime);
            var body = JsonSerializer.SerializeToUtf8Bytes(envelope, EventEnvelope.SerializerOptions);
            _canal.Publish(SesionEventRouting.RoutingKeyFor(eventType), body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallo publicando {EventType} a RabbitMQ (best-effort, se continúa)", eventType);
        }
        return Task.CompletedTask;
    }

    public Task PublicarPartidaPublicadaEnLobbyAsync(PartidaPublicadaEnLobbyEvent evento, CancellationToken cancellationToken) => Publicar("PartidaPublicadaEnLobby", evento);
    public Task PublicarPartidaIniciadaAsync(PartidaIniciadaEvent evento, CancellationToken cancellationToken) => Publicar("PartidaIniciada", evento);
    public Task PublicarJuegoActivadoAsync(JuegoActivadoEvent evento, CancellationToken cancellationToken) => Publicar("JuegoActivado", evento);
    public Task PublicarPartidaCanceladaAsync(PartidaCanceladaEvent evento, CancellationToken cancellationToken) => Publicar("PartidaCancelada", evento);
    public Task PublicarPartidaFinalizadaAsync(PartidaFinalizadaEvent evento, CancellationToken cancellationToken) => Publicar("PartidaFinalizada", evento);
    public Task PublicarRespuestaTriviaValidadaAsync(RespuestaTriviaValidadaEvent evento, CancellationToken cancellationToken) => Publicar("RespuestaTriviaValidada", evento);
    public Task PublicarPuntajeTriviaIncrementadoAsync(PuntajeTriviaIncrementadoEvent evento, CancellationToken cancellationToken) => Publicar("PuntajeTriviaIncrementado", evento);
    public Task PublicarPreguntaTriviaActivadaAsync(PreguntaTriviaActivadaEvent evento, CancellationToken cancellationToken) => Publicar("PreguntaTriviaActivada", evento);
    public Task PublicarPreguntaTriviaCerradaAsync(PreguntaTriviaCerradaEvent evento, CancellationToken cancellationToken) => Publicar("PreguntaTriviaCerrada", evento);
    public Task PublicarTesoroQRValidadoAsync(TesoroQRValidadoEvent evento, CancellationToken cancellationToken) => Publicar("TesoroQRValidado", evento);
    public Task PublicarEtapaBDTGanadaAsync(EtapaBDTGanadaEvent evento, CancellationToken cancellationToken) => Publicar("EtapaBDTGanada", evento);
    public Task PublicarEtapaBDTCerradaAsync(EtapaBDTCerradaEvent evento, CancellationToken cancellationToken) => Publicar("EtapaBDTCerrada", evento);
    public Task PublicarEtapaBDTActivadaAsync(EtapaBDTActivadaEvent evento, CancellationToken cancellationToken) => Publicar("EtapaBDTActivada", evento);
    public Task PublicarPistaEnviadaAsync(PistaEnviadaEvent evento, CancellationToken cancellationToken) => Publicar("PistaEnviada", evento);
    public Task PublicarConvocatoriaCreadaAsync(ConvocatoriaCreadaEvent evento, CancellationToken cancellationToken) => Publicar("ConvocatoriaCreada", evento);
    public Task PublicarConvocatoriaRespondidaAsync(ConvocatoriaRespondidaEvent evento, CancellationToken cancellationToken) => Publicar("ConvocatoriaRespondida", evento);
    public Task PublicarUbicacionActualizadaAsync(UbicacionActualizadaEvent evento, CancellationToken cancellationToken) => Publicar("UbicacionActualizada", evento);
    public Task PublicarInscripcionEquipoCreadaAsync(InscripcionEquipoCreadaEvent evento, CancellationToken cancellationToken) => Publicar("InscripcionEquipoCreada", evento);
    public Task PublicarInscripcionEquipoCanceladaAsync(InscripcionEquipoCanceladaEvent evento, CancellationToken cancellationToken) => Publicar("InscripcionEquipoCancelada", evento);
}
