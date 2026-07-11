using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;

namespace Umbral.OperacionesSesion.Application.Commands;

public sealed record RechazarInscripcionCommand(Guid PartidaId, Guid InscripcionId) : IRequest<LobbyDto>;
