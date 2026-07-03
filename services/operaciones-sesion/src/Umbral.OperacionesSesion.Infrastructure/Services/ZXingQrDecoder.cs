using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Umbral.OperacionesSesion.Domain.Abstractions;
using ZXing;

namespace Umbral.OperacionesSesion.Infrastructure.Services;

/// <summary>
/// Decodifica el contenido textual de un QR a partir de una imagen PNG/JPEG.
/// Usa ZXing.Net + SixLabors.ImageSharp (cross-platform, sin System.Drawing).
/// Devuelve null si la imagen es nula/vacía, corrupta, o no contiene un QR legible.
/// </summary>
public sealed class ZXingQrDecoder : IQrDecoder
{
    public string? Decodificar(byte[] imagen)
    {
        if (imagen is null || imagen.Length == 0) return null;
        try
        {
            using var image = Image.Load<Rgba32>(imagen);
            var width = image.Width;
            var height = image.Height;
            var rgbBytes = new byte[width * height * 3];
            var idx = 0;
            image.ProcessPixelRows(accessor =>
            {
                for (var y = 0; y < accessor.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (var x = 0; x < row.Length; x++)
                    {
                        rgbBytes[idx++] = row[x].R;
                        rgbBytes[idx++] = row[x].G;
                        rgbBytes[idx++] = row[x].B;
                    }
                }
            });
            var source = new RGBLuminanceSource(rgbBytes, width, height, RGBLuminanceSource.BitmapFormat.RGB24);
            var reader = new BarcodeReaderGeneric
            {
                AutoRotate = true,
                Options =
                {
                    TryHarder = true,
                    PossibleFormats = new[] { BarcodeFormat.QR_CODE }
                }
            };
            var result = reader.Decode(source);
            return result?.Text;
        }
        catch
        {
            return null; // imagen corrupta / formato no soportado → ilegible
        }
    }
}
