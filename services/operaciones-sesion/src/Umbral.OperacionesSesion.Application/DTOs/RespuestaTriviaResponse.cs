namespace Umbral.OperacionesSesion.Application.DTOs;
public sealed record RespuestaTriviaResponse(Guid PartidaId, Guid PreguntaId, bool EsCorrecta, bool CerroPregunta, int? Puntaje);
