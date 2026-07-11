namespace Umbral.OperacionesSesion.Application.Interfaces;

public sealed record ConvocatoriaCreadaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid ConvocatoriaId, Guid EquipoId, Guid UsuarioId);

public sealed record ConvocatoriaRespondidaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid ConvocatoriaId, Guid UsuarioId, string EstadoConvocatoria);

public sealed record InscripcionEquipoCreadaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid InscripcionId, Guid EquipoId, DateTime Instante);

public sealed record InscripcionEquipoCanceladaEvent(
    Guid PartidaId, Guid InscripcionId, Guid EquipoId, DateTime Instante);

// HU-19: ciclo de aprobación de inscripciones por el operador. Los tres comparten forma;
// alimentan solo el historial de Puntuaciones (cola ligada a operaciones-sesion.#).
public sealed record InscripcionSolicitadaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid InscripcionId, string Modalidad,
    Guid? ParticipanteId, Guid? EquipoId, DateTime Instante);

public sealed record InscripcionAceptadaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid InscripcionId, string Modalidad,
    Guid? ParticipanteId, Guid? EquipoId, DateTime Instante);

public sealed record InscripcionRechazadaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid InscripcionId, string Modalidad,
    Guid? ParticipanteId, Guid? EquipoId, DateTime Instante);
