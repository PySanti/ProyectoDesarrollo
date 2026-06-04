namespace Umbral.BdtGameService.Application.Games.Start;

public sealed record EtapaActivaBdtResponse(
    Guid EtapaId,
    int Orden,
    int TiempoLimiteSegundos,
    DateTime IniciadaEnUtc,
    DateTime CierraEnUtc);
