using MediatR;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Application.Handlers.Commands;

public sealed class ProyectarEtapaBdtGanadaCommandHandler : IRequestHandler<ProyectarEtapaBdtGanadaCommand>
{
    private readonly IProyeccionesRepository _repo;
    private readonly IPuntuacionesUnitOfWork _uow;

    public ProyectarEtapaBdtGanadaCommandHandler(IProyeccionesRepository repo, IPuntuacionesUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task Handle(ProyectarEtapaBdtGanadaCommand request, CancellationToken cancellationToken)
    {
        if (await _repo.EventoYaProcesadoAsync(request.EventId, cancellationToken))
        {
            return;
        }

        // Identidad dual slice-E: en Equipo se acredita al equipo; en Individual, al participante.
        var competidorId = request.EquipoId ?? request.ParticipanteId;
        var tipo = request.EquipoId is null ? TipoCompetidor.Participante : TipoCompetidor.Equipo;

        var marcador = await _repo.GetMarcadorAsync(request.JuegoId, competidorId, cancellationToken);
        if (marcador is null)
        {
            marcador = Marcador.Nuevo(request.JuegoId, competidorId, request.PartidaId, tipo);
            _repo.AddMarcador(marcador);
        }
        marcador.Acreditar(request.Puntaje, request.TiempoResolucionMs);

        _repo.RegistrarEventoProcesado(EventoProcesado.Registrar(
            request.EventId, "EtapaBDTGanada", request.OccurredAt, DateTime.UtcNow));
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
