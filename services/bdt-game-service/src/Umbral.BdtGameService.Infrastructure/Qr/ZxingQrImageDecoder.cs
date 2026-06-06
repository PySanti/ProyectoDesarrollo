using SkiaSharp;
using Umbral.BdtGameService.Application.Abstractions.Qr;
using ZXing;
using ZXing.SkiaSharp;

namespace Umbral.BdtGameService.Infrastructure.Qr;

public sealed class ZxingQrImageDecoder : IQrImageDecoder
{
    private const int MaxDecodeDimension = 1600;
    private const int MinDecodeDimension = 900;

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
                    TryInverted = true,
                    PureBarcode = false,
                    PossibleFormats = new[] { BarcodeFormat.QR_CODE }
                }
            };

            var decoded = DecodeWithFallbacks(reader, bitmap, cancellationToken);
            return Task.FromResult(decoded);
        }
        catch
        {
            return Task.FromResult<string?>(null);
        }
    }

    private static string? DecodeWithFallbacks(BarcodeReader reader, SKBitmap bitmap, CancellationToken cancellationToken)
    {
        foreach (var candidate in CreateDecodeCandidates(bitmap))
        {
            cancellationToken.ThrowIfCancellationRequested();
            using (candidate)
            {
                var result = reader.Decode(candidate.Bitmap);
                if (!string.IsNullOrWhiteSpace(result?.Text))
                {
                    return result.Text.Trim();
                }
            }
        }

        return null;
    }

    private static IEnumerable<BitmapCandidate> CreateDecodeCandidates(SKBitmap source)
    {
        yield return new BitmapCandidate(source, dispose: false);

        var normalized = ResizeForDecode(source);
        if (!ReferenceEquals(normalized, source))
        {
            yield return new BitmapCandidate(normalized, dispose: true);
        }

        using var grayscale = ToGrayscale(normalized);
        yield return new BitmapCandidate(grayscale.Copy(), dispose: true);

        using var contrast = IncreaseContrast(grayscale);
        yield return new BitmapCandidate(contrast.Copy(), dispose: true);

        foreach (var degrees in new[] { 90, 180, 270 })
        {
            yield return new BitmapCandidate(Rotate(normalized, degrees), dispose: true);
            yield return new BitmapCandidate(Rotate(contrast, degrees), dispose: true);
        }

        if (!ReferenceEquals(normalized, source))
        {
            normalized.Dispose();
        }
    }

    private static SKBitmap ResizeForDecode(SKBitmap source)
    {
        var longestSide = Math.Max(source.Width, source.Height);
        var shortestSide = Math.Min(source.Width, source.Height);

        if (longestSide <= MaxDecodeDimension && shortestSide >= MinDecodeDimension / 2)
        {
            return source;
        }

        var targetLongestSide = longestSide > MaxDecodeDimension ? MaxDecodeDimension : MinDecodeDimension;
        var scale = targetLongestSide / (float)longestSide;
        var targetWidth = Math.Max(1, (int)Math.Round(source.Width * scale));
        var targetHeight = Math.Max(1, (int)Math.Round(source.Height * scale));

        return source.Resize(
            new SKImageInfo(targetWidth, targetHeight),
            new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear)) ?? source.Copy();
    }

    private static SKBitmap ToGrayscale(SKBitmap source)
    {
        var target = new SKBitmap(source.Width, source.Height, source.ColorType, source.AlphaType);
        using var canvas = new SKCanvas(target);
        using var paint = new SKPaint
        {
            ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
            {
                0.299f, 0.587f, 0.114f, 0, 0,
                0.299f, 0.587f, 0.114f, 0, 0,
                0.299f, 0.587f, 0.114f, 0, 0,
                0, 0, 0, 1, 0
            })
        };
        canvas.DrawBitmap(source, 0, 0, paint);
        return target;
    }

    private static SKBitmap IncreaseContrast(SKBitmap source)
    {
        var target = new SKBitmap(source.Width, source.Height, source.ColorType, source.AlphaType);
        using var canvas = new SKCanvas(target);
        using var paint = new SKPaint
        {
            ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
            {
                1.35f, 0, 0, 0, -35,
                0, 1.35f, 0, 0, -35,
                0, 0, 1.35f, 0, -35,
                0, 0, 0, 1, 0
            })
        };
        canvas.DrawBitmap(source, 0, 0, paint);
        return target;
    }

    private static SKBitmap Rotate(SKBitmap source, int degrees)
    {
        var swapDimensions = degrees is 90 or 270;
        var target = new SKBitmap(
            swapDimensions ? source.Height : source.Width,
            swapDimensions ? source.Width : source.Height,
            source.ColorType,
            source.AlphaType);

        using var canvas = new SKCanvas(target);
        canvas.Clear(SKColors.White);
        canvas.Translate(target.Width / 2f, target.Height / 2f);
        canvas.RotateDegrees(degrees);
        canvas.Translate(-source.Width / 2f, -source.Height / 2f);
        canvas.DrawBitmap(source, 0, 0);
        return target;
    }

    private static bool IsSupportedImage(string contentType)
    {
        return contentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class BitmapCandidate : IDisposable
    {
        private readonly bool _dispose;

        public BitmapCandidate(SKBitmap bitmap, bool dispose)
        {
            Bitmap = bitmap;
            _dispose = dispose;
        }

        public SKBitmap Bitmap { get; }

        public void Dispose()
        {
            if (_dispose)
            {
                Bitmap.Dispose();
            }
        }
    }
}
