namespace Umbral.Partidas.Application.DTOs;

public sealed record EtapaRequest(
    int Orden,
    string CodigoQREsperado,
    int Puntaje,
    int TiempoLimiteSegundos);
