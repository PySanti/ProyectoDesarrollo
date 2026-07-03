using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Umbral.OperacionesSesion.Infrastructure.Services;
using ZXing;
using ZXing.QrCode;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Infrastructure;

public class ZXingQrDecoderTests
{
    // Genera un PNG con un QR que codifica `texto`.
    private static byte[] QrPng(string texto)
    {
        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions { Width = 200, Height = 200, Margin = 1 }
        };
        var pixelData = writer.Write(texto);
        using var image = Image.LoadPixelData<Bgra32>(pixelData.Pixels, pixelData.Width, pixelData.Height);
        using var ms = new MemoryStream();
        image.Save(ms, new PngEncoder());
        return ms.ToArray();
    }

    [Fact]
    public void Decodifica_qr_valido()
    {
        var decoder = new ZXingQrDecoder();
        var png = QrPng("ETAPA-UMBRAL-1");
        Assert.Equal("ETAPA-UMBRAL-1", decoder.Decodificar(png));
    }

    [Fact]
    public void Imagen_sin_qr_devuelve_null()
    {
        var decoder = new ZXingQrDecoder();
        using var blank = new Image<Bgra32>(50, 50, new Bgra32(255, 255, 255, 255));
        using var ms = new MemoryStream();
        blank.Save(ms, new PngEncoder());
        Assert.Null(decoder.Decodificar(ms.ToArray()));
    }
}
