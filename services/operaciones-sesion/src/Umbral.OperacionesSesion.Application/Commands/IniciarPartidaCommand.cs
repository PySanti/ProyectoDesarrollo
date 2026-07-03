using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;

namespace Umbral.OperacionesSesion.Application.Commands;

public sealed record IniciarPartidaCommand(Guid PartidaId) : IRequest<InicioPartidaResponse>;
