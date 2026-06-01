using MediatR;
using Umbral.TeamService.Application.Abstractions.Events;
using Umbral.TeamService.Application.Abstractions.Persistence;
using Umbral.TeamService.Application.Abstractions.Services;
using Umbral.TeamService.Application.Exceptions;
using Umbral.TeamService.Domain.Entities;

namespace Umbral.TeamService.Application.Teams.CreateTeam;

public sealed class CrearEquipoCommandHandler : IRequestHandler<CrearEquipoCommand, CrearEquipoResponse>
{
    private const int MaxPersistenceCodeCollisionRetries = 3;

    private readonly IEquipoRepository _equipoRepository;
    private readonly ICodigoAccesoGenerator _codigoAccesoGenerator;
    private readonly ITeamEventsPublisher _teamEventsPublisher;

    public CrearEquipoCommandHandler(
        IEquipoRepository equipoRepository,
        ICodigoAccesoGenerator codigoAccesoGenerator,
        ITeamEventsPublisher teamEventsPublisher)
    {
        _equipoRepository = equipoRepository;
        _codigoAccesoGenerator = codigoAccesoGenerator;
        _teamEventsPublisher = teamEventsPublisher;
    }

    public async Task<CrearEquipoResponse> Handle(CrearEquipoCommand request, CancellationToken cancellationToken)
    {
        var alreadyInActiveTeam = await _equipoRepository.ExistsActiveTeamByUserIdAsync(request.ActorUserId, cancellationToken);
        if (alreadyInActiveTeam)
        {
            throw new AlreadyBelongsToActiveTeamException(request.ActorUserId);
        }

        Equipo? equipo = null;
        for (var attempt = 0; attempt < MaxPersistenceCodeCollisionRetries; attempt++)
        {
            var codigoAcceso = await _codigoAccesoGenerator.GenerateUniqueCodeAsync(cancellationToken);
            equipo = Equipo.CrearPorParticipante(request.NombreEquipo, codigoAcceso, request.ActorUserId);

            try
            {
                await _equipoRepository.AddAsync(equipo, cancellationToken);
                break;
            }
            catch (AccessCodeGenerationException) when (attempt < MaxPersistenceCodeCollisionRetries - 1)
            {
                continue;
            }
            catch (ConcurrentTeamCreationException)
            {
                throw new AlreadyBelongsToActiveTeamException(request.ActorUserId);
            }
        }

        if (equipo is null)
        {
            throw new PersistenceException("No fue posible crear el equipo.");
        }

        await _teamEventsPublisher.PublishEquipoCreadoAsync(
            new EquipoCreadoIntegrationEvent(
                equipo.EquipoId,
                request.ActorUserId,
                equipo.CodigoAcceso,
                DateTime.UtcNow),
            cancellationToken);

        var integrantes = equipo.Participantes
            .Select(x => new CrearEquipoIntegranteResponse(x.UsuarioId, x.EsLider))
            .ToArray();

        return new CrearEquipoResponse(
            equipo.EquipoId,
            equipo.NombreEquipo,
            equipo.CodigoAcceso,
            equipo.Estado.ToString(),
            request.ActorUserId,
            integrantes);
    }
}
