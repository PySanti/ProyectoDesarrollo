namespace Umbral.Partidas.Application.DTOs;

public sealed record OpcionRequest(string Texto, bool EsCorrecta);

public sealed record PreguntaRequest(
    string Texto,
    IReadOnlyList<OpcionRequest> Opciones,
    int Puntaje,
    int TiempoLimiteSegundos);
