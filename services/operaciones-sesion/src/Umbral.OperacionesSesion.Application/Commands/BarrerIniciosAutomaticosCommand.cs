using MediatR;

namespace Umbral.OperacionesSesion.Application.Commands;

public sealed record BarrerIniciosAutomaticosCommand() : IRequest<int>;
