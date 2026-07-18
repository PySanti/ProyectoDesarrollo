using MediatR;
using Umbral.Partidas.Application.Commands;
using Umbral.Partidas.Domain.Abstractions.Persistence;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Application.Handlers.Commands;

public sealed class ProyectarEstadoPartidaCommandHandler : IRequestHandler<ProyectarEstadoPartidaCommand>
{
    private readonly IPartidaRepository _partidas;
    private readonly IPartidasUnitOfWork _unitOfWork;

    public ProyectarEstadoPartidaCommandHandler(IPartidaRepository partidas, IPartidasUnitOfWork unitOfWork)
    {
        _partidas = partidas;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ProyectarEstadoPartidaCommand request, CancellationToken cancellationToken)
    {
        var partida = await _partidas.GetByIdAsync(PartidaId.From(request.PartidaId), cancellationToken);
        // Best-effort (ADR-0012): un evento de una partida desconocida no tumba el consumidor.
        if (partida is null)
            return;

        partida.ProyectarEstado(request.Estado);
        _partidas.Update(partida);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
