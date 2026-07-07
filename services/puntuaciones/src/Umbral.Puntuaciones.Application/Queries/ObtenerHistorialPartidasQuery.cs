using MediatR;
using Umbral.Puntuaciones.Application.DTOs;

namespace Umbral.Puntuaciones.Application.Queries;

public sealed record ObtenerHistorialPartidasQuery(Guid ParticipanteId) : IRequest<HistorialPartidasResponse>;
