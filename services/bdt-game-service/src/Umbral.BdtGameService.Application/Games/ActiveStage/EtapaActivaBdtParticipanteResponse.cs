namespace Umbral.BdtGameService.Application.Games.ActiveStage;

public sealed record EtapaActivaBdtParticipanteResponse(
    Guid EtapaId,
    int Orden,
    string Estado,
    int TiempoLimiteSegundos,
    DateTime IniciadaEnUtc,
    DateTime CierraEnUtc);
