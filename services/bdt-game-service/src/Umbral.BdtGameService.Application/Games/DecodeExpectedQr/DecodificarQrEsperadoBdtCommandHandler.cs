using MediatR;
using Umbral.BdtGameService.Application.Abstractions.Qr;

namespace Umbral.BdtGameService.Application.Games.DecodeExpectedQr;

public sealed class DecodificarQrEsperadoBdtCommandHandler : IRequestHandler<DecodificarQrEsperadoBdtCommand, DecodificarQrEsperadoBdtResponse>
{
    private readonly IQrImageDecoder _qrImageDecoder;

    public DecodificarQrEsperadoBdtCommandHandler(IQrImageDecoder qrImageDecoder)
    {
        _qrImageDecoder = qrImageDecoder;
    }

    public async Task<DecodificarQrEsperadoBdtResponse> Handle(DecodificarQrEsperadoBdtCommand request, CancellationToken cancellationToken)
    {
        var qrDecodificado = await _qrImageDecoder.DecodeAsync(request.ImageContent, request.ContentType, cancellationToken);

        if (string.IsNullOrWhiteSpace(qrDecodificado))
        {
            return new DecodificarQrEsperadoBdtResponse(
                "NoLegible",
                null,
                "No se pudo leer un QR en la imagen.");
        }

        return new DecodificarQrEsperadoBdtResponse(
            "Decodificado",
            qrDecodificado.Trim(),
            "QR decodificado correctamente.");
    }
}
