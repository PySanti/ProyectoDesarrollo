using MediatR;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class CancelarInscripcionCommandHandler : IRequestHandler<CancelarInscripcionCommand, Unit>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;

    public CancelarInscripcionCommandHandler(ISesionPartidaRepository sesiones, IOperacionesSesionUnitOfWork unitOfWork)
    {
        _sesiones = sesiones;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(CancelarInscripcionCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        sesion.CancelarInscripcion(request.ParticipanteId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
