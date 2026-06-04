using QRCoder;
using Umbral.BdtGameService.Infrastructure.Qr;
using Umbral.BdtGameService.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;

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
}
