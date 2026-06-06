using MediatR;

namespace Umbral.BdtGameService.Application.Games.DecodeExpectedQr;

public sealed record DecodificarQrEsperadoBdtCommand(
    string FileName,
    string ContentType,
    long Length,
    byte[] ImageContent) : IRequest<DecodificarQrEsperadoBdtResponse>;
