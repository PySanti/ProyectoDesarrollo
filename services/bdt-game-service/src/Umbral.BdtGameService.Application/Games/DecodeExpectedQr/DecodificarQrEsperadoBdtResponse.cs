namespace Umbral.BdtGameService.Application.Games.DecodeExpectedQr;

public sealed record DecodificarQrEsperadoBdtResponse(
    string EstadoProcesamiento,
    string? QrDecodificado,
    string Mensaje);
