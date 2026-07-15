using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.UnitTests.Application.Fakes;

public sealed class FakeSesionPartidaRepository : ISesionPartidaRepository
{
    private readonly Dictionary<Guid, SesionPartida> _store = new(); // keyed by PartidaId
    public IReadOnlyDictionary<Guid, SesionPartida> Store => _store;

    // Test hook: simulate that the participant is active in some OTHER partida.
    public bool ParticipacionActivaEnOtra { get; set; }

    public void Add(SesionPartida sesion) => _store[sesion.PartidaId] = sesion;

    public Task<SesionPartida?> GetByPartidaIdAsync(Guid partidaId, CancellationToken cancellationToken)
        => Task.FromResult(_store.TryGetValue(partidaId, out var s) ? s : null);

    public Task<bool> ExistsForPartidaAsync(Guid partidaId, CancellationToken cancellationToken)
        => Task.FromResult(_store.ContainsKey(partidaId));

    public Task<bool> ParticipanteTieneParticipacionActivaAsync(
        Guid participanteId, Guid exceptPartidaId, CancellationToken cancellationToken)
        => Task.FromResult(ParticipacionActivaEnOtra);

    public Task<SesionPartida?> GetByParticipanteActivoAsync(Guid participanteId, CancellationToken cancellationToken)
        // HU-19: refleja el repo real (Task 3): la inscripción propia cuenta Pendiente+Activa
        // (OcupaParticipacion); la convocatoria aceptada sigue exigiendo inscripción activa.
        => Task.FromResult(_store.Values.FirstOrDefault(s =>
            (s.Estado == EstadoSesion.Lobby || s.Estado == EstadoSesion.Iniciada)
            && s.Inscripciones.Any(i =>
                (i.OcupaParticipacion && i.ParticipanteId == participanteId)
                || (i.EsActiva && i.Convocatorias.Any(c => c.UsuarioId == participanteId && c.Estado == EstadoConvocatoria.Aceptada)))));

    public Task<IReadOnlyList<SesionPartida>> GetSesionesConActividadVencidaAsync(DateTime now, CancellationToken cancellationToken)
        => Task.FromResult((IReadOnlyList<SesionPartida>)_store.Values
            .Where(s => s.Estado == EstadoSesion.Iniciada)
            .ToList());

    public Task<IReadOnlyList<SesionPartida>> GetSesionesAutoInicioPendienteAsync(DateTime now, CancellationToken cancellationToken)
        => Task.FromResult((IReadOnlyList<SesionPartida>)_store.Values
            .Where(s => s.Estado == EstadoSesion.Lobby
                && (s.ModoInicioPartida == ModoInicioPartida.Automatico
                    || s.ModoInicioPartida == ModoInicioPartida.ManualYAutomatico)
                && s.TiempoInicio != null
                && s.TiempoInicio <= now)
            .ToList());

    // Test hook: simulate that the equipo is active in some OTHER partida.
    public bool EquipoActivaEnOtra { get; set; }

    public Task<bool> EquipoTieneParticipacionActivaAsync(Guid equipoId, Guid exceptPartidaId, CancellationToken cancellationToken)
        => Task.FromResult(EquipoActivaEnOtra);

    public Task<SesionPartida?> GetByConvocatoriaIdAsync(Guid convocatoriaId, CancellationToken cancellationToken)
        => Task.FromResult(_store.Values.FirstOrDefault(s =>
            s.Inscripciones.Any(i => i.Convocatorias.Any(c => c.Id.Valor == convocatoriaId))));

    public Task<IReadOnlyList<ConvocatoriaPendienteProyeccion>> GetConvocatoriasPendientesByUsuarioAsync(
        Guid usuarioId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<ConvocatoriaPendienteProyeccion>>(_store.Values
            .Where(s => s.Estado == EstadoSesion.Lobby)
            .SelectMany(s => s.Inscripciones
                .Where(i => i.EsActiva)
                .SelectMany(i => i.Convocatorias
                    .Where(c => c.UsuarioId == usuarioId && c.EstaPendiente)
                    .Select(c => new ConvocatoriaPendienteProyeccion(
                        c.Id.Valor, c.PartidaId, s.Nombre, c.EquipoId, c.FechaEnvio))))
            .OrderBy(x => x.FechaEnvio)
            .ToList());

    public Task<IReadOnlyList<SesionPartida>> GetSesionesEnLobbyAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<SesionPartida>>(
            _store.Values.Where(s => s.Estado == EstadoSesion.Lobby).ToList());
}
