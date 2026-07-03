using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;

namespace Umbral.OperacionesSesion.Application.Commands;

public sealed record FinalizarJuegoActualCommand(Guid PartidaId) : IRequest<AvanceJuegoResponse>;
