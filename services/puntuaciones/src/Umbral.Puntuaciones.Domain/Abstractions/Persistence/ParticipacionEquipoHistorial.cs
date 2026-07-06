namespace Umbral.Puntuaciones.Domain.Abstractions.Persistence;

// Membresía de equipo resuelta del historial (HU-27): partida donde el participante
// autoró una acción de juego acreditada a un equipo.
public sealed record ParticipacionEquipoHistorial(Guid PartidaId, Guid EquipoId);
