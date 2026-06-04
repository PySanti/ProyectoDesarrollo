using MediatR;

namespace Umbral.BdtGameService.Application.Games.UploadTreasure;

public sealed record SubirTesoroQrCommand(
    Guid PartidaId,
    Guid EtapaId,
    Guid ParticipanteUserId,
    string FileName,
    string ContentType,
    long Length,
    byte[] ImageContent) : IRequest<SubirTesoroQrResponse>;
