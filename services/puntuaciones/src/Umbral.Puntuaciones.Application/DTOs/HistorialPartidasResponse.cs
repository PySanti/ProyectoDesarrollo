using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Application.DTOs;

public sealed record JuegoJugadoDto(Guid JuegoId, int Orden, TipoJuego TipoJuego, int Puntos);

public sealed record PartidaJugadaDto(
    Guid PartidaId, Modalidad? Modalidad, DateTime? FechaFin, Guid? EquipoId,
    int PuntosTotales, int Posicion, bool Gano, IReadOnlyList<JuegoJugadoDto> Juegos);

public sealed record HistorialPartidasResponse(Guid ParticipanteId, IReadOnlyList<PartidaJugadaDto> Partidas);
