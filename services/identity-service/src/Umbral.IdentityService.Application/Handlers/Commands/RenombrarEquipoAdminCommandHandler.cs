using MediatR;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Application.Handlers.Queries;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;

namespace Umbral.IdentityService.Application.Handlers.Commands;

public sealed class RenombrarEquipoAdminCommandHandler : IRequestHandler<RenombrarEquipoAdminCommand, EquipoAdminResponse>
{
    private readonly IEquipoRepository _equipos;
    private readonly IHistorialNombreEquipoRepository _historial;
    private readonly TimeProvider _timeProvider;

    public RenombrarEquipoAdminCommandHandler(
        IEquipoRepository equipos,
        IHistorialNombreEquipoRepository historial,
        TimeProvider timeProvider)
    {
        _equipos = equipos;
        _historial = historial;
        _timeProvider = timeProvider;
    }

    public async Task<EquipoAdminResponse> Handle(RenombrarEquipoAdminCommand request, CancellationToken cancellationToken)
    {
        var equipo = await _equipos.GetByIdAsync(request.EquipoId, cancellationToken)
            ?? throw new EquipoNoEncontradoException(request.EquipoId);

        equipo.Renombrar(request.NombreEquipo);
        await _equipos.UpdateAsync(equipo, cancellationToken);

        var ahora = _timeProvider.GetUtcNow().UtcDateTime;
        var registros = equipo.Participantes
            .Select(p => HistorialNombreEquipo.Registrar(p.UsuarioId, equipo.EquipoId, equipo.NombreEquipo, ahora))
            .ToList();
        await _historial.AddRangeAsync(registros, cancellationToken);

        return GetEquiposAdminQueryHandler.MapToResponse(equipo);
    }
}
