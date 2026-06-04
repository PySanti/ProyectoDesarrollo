using MediatR;
using Umbral.TriviaGame.Application.Dtos;

namespace Umbral.TriviaGame.Application.Commands;

public sealed record StartTriviaGameCommand(Guid PartidaId) : IRequest<TriviaGameDetailDto>;
