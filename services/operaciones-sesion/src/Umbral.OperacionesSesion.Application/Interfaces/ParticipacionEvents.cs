namespace Umbral.OperacionesSesion.Application.Interfaces;

public sealed record ConvocatoriaCreadaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid ConvocatoriaId, Guid EquipoId, Guid UsuarioId);

public sealed record ConvocatoriaRespondidaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid ConvocatoriaId, Guid UsuarioId, string EstadoConvocatoria);
