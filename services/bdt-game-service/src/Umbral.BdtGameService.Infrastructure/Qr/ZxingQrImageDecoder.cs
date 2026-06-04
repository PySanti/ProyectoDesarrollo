using SkiaSharp;
using Umbral.BdtGameService.Application.Abstractions.Qr;
using ZXing;
using ZXing.SkiaSharp;

namespace Umbral.BdtGameService.Infrastructure.Qr;

public sealed class ZxingQrImageDecoder : IQrImageDecoder
{
    public Task<string?> DecodeAsync(byte[] imageContent, string contentType, CancellationToken cancellationToken)
    {
        if (!IsSupportedImage(contentType))
        {
            return Task.FromResult<string?>(null);
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var bitmap = SKBitmap.Decode(imageContent);
            if (bitmap is null)
            {
                return Task.FromResult<string?>(null);
            }

            var reader = new BarcodeReader
            {
                AutoRotate = true,
                Options = new ZXing.Common.DecodingOptions
                {
                    TryHarder = true,
                    PossibleFormats = new[] { BarcodeFormat.QR_CODE }
                }
            };

            var result = reader.Decode(bitmap);
            var decoded = string.IsNullOrWhiteSpace(result?.Text) ? null : result.Text.Trim();
            return Task.FromResult(decoded);
        }
        catch
        {
            return Task.FromResult<string?>(null);
        }
    }

    private static bool IsSupportedImage(string contentType)
    {
        return contentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase);
    }
}
