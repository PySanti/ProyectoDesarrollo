namespace Umbral.OperacionesSesion.Application.Interfaces;

public sealed record ConvocatoriaCreadaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid ConvocatoriaId, Guid EquipoId, Guid UsuarioId);

public sealed record ConvocatoriaRespondidaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid ConvocatoriaId, Guid UsuarioId, string EstadoConvocatoria);

public sealed record InscripcionEquipoCreadaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid InscripcionId, Guid EquipoId, DateTime Instante);

public sealed record InscripcionEquipoCanceladaEvent(
    Guid PartidaId, Guid InscripcionId, Guid EquipoId, DateTime Instante);
