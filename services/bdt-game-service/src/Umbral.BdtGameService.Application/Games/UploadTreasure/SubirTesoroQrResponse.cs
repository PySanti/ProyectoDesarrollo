namespace Umbral.BdtGameService.Application.Games.UploadTreasure;

public sealed record SubirTesoroQrResponse(
    Guid TesoroId,
    Guid PartidaId,
    Guid EtapaId,
    Guid ExploradorId,
    DateTime FechaEnvioUtc,
    string EstadoProcesamiento,
    string? QrDecodificado,
    string Mensaje);
