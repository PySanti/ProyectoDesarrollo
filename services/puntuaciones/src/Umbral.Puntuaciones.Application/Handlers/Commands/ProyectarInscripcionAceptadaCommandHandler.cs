using MediatR;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Application.Handlers.Commands;

public sealed class ProyectarInscripcionAceptadaCommandHandler : IRequestHandler<ProyectarInscripcionAceptadaCommand>
{
    private readonly IProyeccionesRepository _repo;
    private readonly IPuntuacionesUnitOfWork _uow;

    public ProyectarInscripcionAceptadaCommandHandler(IProyeccionesRepository repo, IPuntuacionesUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task Handle(ProyectarInscripcionAceptadaCommand request, CancellationToken cancellationToken)
    {
        if (await _repo.EventoYaProcesadoAsync(request.EventId, cancellationToken))
        {
            return;
        }

        // Identidad dual: en Equipo el competidor es el equipo; en Individual, el participante.
        // El evento trae participanteId xor equipoId segun modalidad.
        var esEquipo = request.Modalidad == "Equipo";
        var competidorId = esEquipo ? request.EquipoId : request.ParticipanteId;
        if (competidorId is null)
        {
            // Payload incoherente con su modalidad: no hay competidor que proyectar.
            return;
        }

        var tipo = esEquipo ? TipoCompetidor.Equipo : TipoCompetidor.Participante;
        if (await _repo.GetParticipacionAsync(request.PartidaId, competidorId.Value, cancellationToken) is null)
        {
            _repo.AddParticipacion(ParticipacionProyectada.Nueva(request.PartidaId, competidorId.Value, tipo));
        }

        _repo.RegistrarEventoProcesado(EventoProcesado.Registrar(
            request.EventId, "InscripcionAceptada", request.OccurredAt, DateTime.UtcNow));
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
