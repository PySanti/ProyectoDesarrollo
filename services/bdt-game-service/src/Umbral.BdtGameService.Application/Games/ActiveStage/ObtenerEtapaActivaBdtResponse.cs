namespace Umbral.BdtGameService.Application.Games.ActiveStage;

public sealed record ObtenerEtapaActivaBdtResponse(
    Guid PartidaId,
    string Nombre,
    string Estado,
    string Modalidad,
    Guid ExploradorId,
    EtapaActivaBdtParticipanteResponse EtapaActiva,
    bool PuedeSubirTesoro,
    bool RequiereGeolocalizacion,
    string Mensaje);
