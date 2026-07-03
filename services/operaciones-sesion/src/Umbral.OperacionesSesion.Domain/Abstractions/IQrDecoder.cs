namespace Umbral.OperacionesSesion.Domain.Abstractions;

/// <summary>Decodifica el contenido textual de un QR contenido en una imagen. Devuelve null si no es legible.</summary>
public interface IQrDecoder
{
    string? Decodificar(byte[] imagen);
}
