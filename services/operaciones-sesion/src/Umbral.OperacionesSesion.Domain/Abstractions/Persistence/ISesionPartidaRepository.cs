using Umbral.OperacionesSesion.Domain.Entities;

namespace Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

public interface ISesionPartidaRepository
{
    void Add(SesionPartida sesion);
    Task<SesionPartida?> GetByPartidaIdAsync(Guid partidaId, CancellationToken cancellationToken);
    Task<bool> ExistsForPartidaAsync(Guid partidaId, CancellationToken cancellationToken);
    Task<bool> ParticipanteTieneParticipacionActivaAsync(
        Guid participanteId, Guid exceptPartidaId, CancellationToken cancellationToken);
    Task<SesionPartida?> GetByParticipanteActivoAsync(Guid participanteId, CancellationToken cancellationToken);
    Task<IReadOnlyList<SesionPartida>> GetSesionesConActividadVencidaAsync(DateTime now, CancellationToken cancellationToken);
    Task<IReadOnlyList<SesionPartida>> GetSesionesAutoInicioPendienteAsync(DateTime now, CancellationToken cancellationToken);
    Task<bool> EquipoTieneParticipacionActivaAsync(
        Guid equipoId, Guid exceptPartidaId, CancellationToken cancellationToken);
    Task<SesionPartida?> GetByConvocatoriaIdAsync(Guid convocatoriaId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Convocatoria>> GetConvocatoriasPendientesByUsuarioAsync(
        Guid usuarioId, CancellationToken cancellationToken);
}
