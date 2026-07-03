namespace Umbral.OperacionesSesion.Application.DTOs;
public sealed record PreguntaActualDto(
    Guid PartidaId, Guid JuegoId, Guid PreguntaId, int Orden, string Texto,
    int TiempoLimiteSegundos, DateTime FechaActivacion, IReadOnlyList<OpcionPublicaDto> Opciones);
public sealed record OpcionPublicaDto(Guid OpcionId, string Texto);
