using MediatR;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Application.Exceptions;

using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;

using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
namespace Umbral.IdentityService.Application.Handlers.Commands;

public sealed class CrearEquipoCommandHandler : IRequestHandler<CrearEquipoCommand, CrearEquipoResponse>
{
    private readonly IEquipoRepository _equipoRepository;
    private readonly IEquipoEventsPublisher _equipoEventsPublisher;

    public CrearEquipoCommandHandler(
        IEquipoRepository equipoRepository,
        IEquipoEventsPublisher equipoEventsPublisher)
    {
        _equipoRepository = equipoRepository;
        _equipoEventsPublisher = equipoEventsPublisher;
    }

    public async Task<CrearEquipoResponse> Handle(CrearEquipoCommand request, CancellationToken cancellationToken)
    {
        var alreadyInActiveTeam = await _equipoRepository.ExistsActiveTeamByUserIdAsync(request.ActorUserId, cancellationToken);
        if (alreadyInActiveTeam)
        {
            throw new AlreadyBelongsToActiveTeamException(request.ActorUserId);
        }

        Equipo equipo;
        try
        {
            equipo = Equipo.CrearPorParticipante(request.NombreEquipo, request.ActorUserId);
            await _equipoRepository.AddAsync(equipo, cancellationToken);
        }
        catch (ConcurrentTeamCreationException)
        {
            throw new AlreadyBelongsToActiveTeamException(request.ActorUserId);
        }

        await _equipoEventsPublisher.PublishEquipoCreadoAsync(
            new EquipoCreadoIntegrationEvent(
                equipo.EquipoId,
                request.ActorUserId,
                DateTime.UtcNow),
            cancellationToken);

        var integrantes = equipo.Participantes
            .Select(x => new CrearEquipoIntegranteResponse(x.UsuarioId, x.EsLider))
            .ToArray();

        return new CrearEquipoResponse(
            equipo.EquipoId,
            equipo.NombreEquipo,
            equipo.Estado.ToString(),
            request.ActorUserId,
            integrantes);
    }
}
