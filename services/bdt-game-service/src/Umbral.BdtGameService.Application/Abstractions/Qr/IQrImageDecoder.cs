namespace Umbral.BdtGameService.Application.Abstractions.Qr;

public interface IQrImageDecoder
{
    Task<string?> DecodeAsync(byte[] imageContent, string contentType, CancellationToken cancellationToken);
}
