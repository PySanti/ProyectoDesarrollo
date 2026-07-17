using MediatR;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Queries;
using Umbral.IdentityService.Domain.Abstractions.Persistence;

namespace Umbral.IdentityService.Application.Handlers.Queries;

public sealed class ResolverNombresQueryHandler
    : IRequestHandler<ResolverNombresQuery, NombresResponse>
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IEquipoRepository _equipos;

    public ResolverNombresQueryHandler(IUsuarioRepository usuarios, IEquipoRepository equipos)
    {
        _usuarios = usuarios;
        _equipos = equipos;
    }

    public async Task<NombresResponse> Handle(
        ResolverNombresQuery request, CancellationToken cancellationToken)
    {
        var participantes = new List<NombreParticipanteResponse>();
        if (request.ParticipanteIds.Count > 0)
        {
            var pedidos = request.ParticipanteIds.ToHashSet();
            var usuarios = await _usuarios.GetAllAsync(cancellationToken);
            // Los competidores viajan en el espacio del sub de Keycloak, no del UsuarioId
            // local: el join va por KeycloakId parseado (mismo patrón que
            // ListarEquiposQueryHandler). Un KeycloakId no parseable se ignora.
            foreach (var u in usuarios)
            {
                if (Guid.TryParse(u.KeycloakId, out var sub) && pedidos.Contains(sub))
                {
                    participantes.Add(new NombreParticipanteResponse(sub, u.Nombre));
                }
            }
        }

        var equipos = new List<NombreEquipoResponse>();
        if (request.EquipoIds.Count > 0)
        {
            var pedidos = request.EquipoIds.ToHashSet();
            var todos = await _equipos.GetAllAsync(cancellationToken);
            foreach (var e in todos)
            {
                if (pedidos.Contains(e.EquipoId))
                {
                    equipos.Add(new NombreEquipoResponse(e.EquipoId, e.NombreEquipo));
                }
            }
        }

        return new NombresResponse(participantes, equipos);
    }
}
