using MediatR;

namespace Umbral.OperacionesSesion.Application.Commands;

public sealed record BarrerTimeoutsCommand() : IRequest<int>;
