namespace Umbral.OperacionesSesion.Application.DTOs;

public sealed record MiSesionDto(
    Guid PartidaId,
    Guid SesionPartidaId,
    string EstadoPartida,
    string Modalidad,
    InscripcionResumenDto Inscripcion,
    JuegoActivoResumenDto? JuegoActivo,
    PreguntaActualDto? PreguntaActual,
    EtapaActualDto? EtapaActual,
    bool? YaRespondioPreguntaActual,
    MiConvocatoriaDto? Convocatoria);

public sealed record InscripcionResumenDto(Guid InscripcionId, string Estado);

public sealed record JuegoActivoResumenDto(Guid JuegoId, int Orden, string TipoJuego, string EstadoJuego);

public sealed record MiConvocatoriaDto(Guid ConvocatoriaId, Guid EquipoId, string Estado);
