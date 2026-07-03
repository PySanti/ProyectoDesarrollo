using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;

namespace Umbral.OperacionesSesion.Application.Commands;

public sealed record PublicarPartidaCommand(Guid PartidaId, string? BearerToken) : IRequest<LobbyDto>;
