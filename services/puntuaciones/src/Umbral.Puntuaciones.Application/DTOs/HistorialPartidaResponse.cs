using System.Text.Json;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Application.DTOs;

// JuegoOrden/TipoJuego son nullable: los eventos de partida (PartidaIniciada,
// PartidaFinalizada) no tienen juego. Van al final para no romper construcciones
// posicionales existentes. `Juego` no tiene nombre en el dominio: su identidad propia es el
// orden dentro de la partida y su tipo, y la etiqueta legible la compone el cliente.
public sealed record EntradaHistorialDto(
    DateTime OccurredAt, string TipoEvento, Guid? JuegoId, Guid? ParticipanteId, Guid? EquipoId,
    JsonElement Detalle, int? JuegoOrden, TipoJuego? TipoJuego);

public sealed record HistorialPartidaResponse(
    Guid PartidaId, int Total, IReadOnlyList<EntradaHistorialDto> Entradas);
