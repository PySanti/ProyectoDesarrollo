using MediatR;
using Umbral.Puntuaciones.Application.DTOs;

namespace Umbral.Puntuaciones.Application.Queries;

public sealed record ObtenerMarcadorQuery(Guid PartidaId, Guid JuegoId, Guid CompetidorId) : IRequest<MarcadorResponse>;
