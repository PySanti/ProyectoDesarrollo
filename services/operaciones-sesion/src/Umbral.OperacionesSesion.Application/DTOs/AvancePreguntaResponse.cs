namespace Umbral.OperacionesSesion.Application.DTOs;
public sealed record AvancePreguntaResponse(Guid PartidaId, int PreguntaCerradaOrden, int? PreguntaActivadaOrden, bool SinMasPreguntas);
