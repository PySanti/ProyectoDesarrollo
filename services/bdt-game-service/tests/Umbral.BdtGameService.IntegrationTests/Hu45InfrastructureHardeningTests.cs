using QRCoder;
using Umbral.BdtGameService.Infrastructure.Qr;
using Umbral.BdtGameService.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using SkiaSharp;

namespace Umbral.BdtGameService.IntegrationTests;

public sealed class Hu45InfrastructureHardeningTests
{
    [Fact]
    public async Task ZxingQrImageDecoder_Should_Decode_Real_Png_Qr_Image()
    {
        var expected = "QR-ETAPA-REAL";
        var content = CreateQrPng(expected);
        var decoder = new ZxingQrImageDecoder();

        var decoded = await decoder.DecodeAsync(content, "image/png", CancellationToken.None);

        Assert.Equal(expected, decoded);
    }

    [Fact]
    public async Task ZxingQrImageDecoder_Should_Decode_Qr_Inside_Large_Png_Canvas()
    {
        var expected = "QR-ETAPA-CANVAS";
        var content = CreateLargeCanvasQrImage(expected, SKEncodedImageFormat.Png, rotateDegrees: 0);
        var decoder = new ZxingQrImageDecoder();

        var decoded = await decoder.DecodeAsync(content, "image/png", CancellationToken.None);

        Assert.Equal(expected, decoded);
    }

    [Fact]
    public async Task ZxingQrImageDecoder_Should_Decode_Rotated_Qr_Inside_Jpeg_Canvas()
    {
        var expected = "QR-ETAPA-JPEG-ROTADO";
        var content = CreateLargeCanvasQrImage(expected, SKEncodedImageFormat.Jpeg, rotateDegrees: 90);
        var decoder = new ZxingQrImageDecoder();

        var decoded = await decoder.DecodeAsync(content, "image/jpeg", CancellationToken.None);

        Assert.Equal(expected, decoded);
    }

    [Fact]
    public async Task ZxingQrImageDecoder_Should_Return_Null_For_Unreadable_Image()
    {
        var decoder = new ZxingQrImageDecoder();

        var decoded = await decoder.DecodeAsync(new byte[] { 1, 2, 3, 4 }, "image/png", CancellationToken.None);

        Assert.Null(decoded);
    }

    [Fact]
    public async Task LocalTesoroQrImageStorage_Should_Write_Retrievable_Artifact()
    {
        var basePath = Path.Combine(Path.GetTempPath(), "umbral-bdt-test-storage", Guid.NewGuid().ToString("N"));
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BdtTreasureStorage:BasePath"] = basePath
            })
            .Build();
        var storage = new LocalTesoroQrImageStorage(configuration);
        var content = new byte[] { 10, 20, 30 };

        var reference = await storage.StoreAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "tesoro.png", "image/png", content, CancellationToken.None);

        var fullPath = Path.Combine(basePath, reference.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(fullPath));
        Assert.Equal(content, await File.ReadAllBytesAsync(fullPath));
    }

    private static byte[] CreateQrPng(string text)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(data);
        return qrCode.GetGraphic(20);
    }

    private static byte[] CreateLargeCanvasQrImage(string text, SKEncodedImageFormat format, int rotateDegrees)
    {
        using var qrBitmap = SKBitmap.Decode(CreateQrPng(text));
        using var surface = SKSurface.Create(new SKImageInfo(2200, 1600));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        var qrTarget = new SKRect(760, 420, 1240, 900);
        canvas.Save();
        canvas.Translate(qrTarget.MidX, qrTarget.MidY);
        canvas.RotateDegrees(rotateDegrees);
        canvas.Translate(-qrTarget.MidX, -qrTarget.MidY);
        canvas.DrawBitmap(qrBitmap, qrTarget);
        canvas.Restore();

        using var image = surface.Snapshot();
        using var encoded = image.Encode(format, quality: 90);
        return encoded.ToArray();
    }
}
