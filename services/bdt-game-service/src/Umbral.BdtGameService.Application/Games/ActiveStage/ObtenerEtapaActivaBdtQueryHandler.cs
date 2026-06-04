using MediatR;
using Umbral.BdtGameService.Application.Abstractions.Persistence;

namespace Umbral.BdtGameService.Application.Games.ActiveStage;

public sealed class ObtenerEtapaActivaBdtQueryHandler : IRequestHandler<ObtenerEtapaActivaBdtQuery, ObtenerEtapaActivaBdtResponse>
{
    private readonly IPartidaBdtRepository _repository;

    public ObtenerEtapaActivaBdtQueryHandler(IPartidaBdtRepository repository)
    {
        _repository = repository;
    }

    public async Task<ObtenerEtapaActivaBdtResponse> Handle(ObtenerEtapaActivaBdtQuery request, CancellationToken cancellationToken)
    {
        var partida = await _repository.GetByIdWithExploradoresAsync(request.PartidaId, cancellationToken);
        if (partida is null)
        {
            throw new KeyNotFoundException("Partida BDT no encontrada.");
        }

        var (explorador, etapaActiva) = partida.ObtenerEtapaActivaParaParticipante(request.ParticipanteUserId);

        return new ObtenerEtapaActivaBdtResponse(
            partida.PartidaId,
            partida.Nombre,
            partida.Estado.ToString(),
            partida.Modalidad.ToString(),
            explorador.ExploradorId,
            new EtapaActivaBdtParticipanteResponse(
                etapaActiva.EtapaId,
                etapaActiva.Orden,
                etapaActiva.Estado.ToString(),
                etapaActiva.TiempoLimiteSegundos,
                etapaActiva.IniciadaEnUtc!.Value,
                etapaActiva.CierraEnUtc!.Value),
            PuedeSubirTesoro: true,
            RequiereGeolocalizacion: true,
            "Etapa activa disponible.");
    }
}
