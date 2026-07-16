using MediatR;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Queries;
using Umbral.IdentityService.Domain.Abstractions.Persistence;

namespace Umbral.IdentityService.Application.Handlers.Queries;

public sealed class ObtenerMiEquipoQueryHandler : IRequestHandler<ObtenerMiEquipoQuery, EquipoMineResponse?>
{
    private readonly IEquipoRepository _equipos;
    private readonly IUsuarioRepository _usuarios;

    public ObtenerMiEquipoQueryHandler(IEquipoRepository equipos, IUsuarioRepository usuarios)
    {
        _equipos = equipos;
        _usuarios = usuarios;
    }

    public async Task<EquipoMineResponse?> Handle(ObtenerMiEquipoQuery request, CancellationToken cancellationToken)
    {
        var equipo = await _equipos.GetActiveByMemberUserIdAsync(request.ActorUserId, cancellationToken);
        if (equipo is null) return null;

        var usuarios = await _usuarios.GetAllAsync(cancellationToken);
        // Los miembros de equipo (ParticipanteEquipo.UsuarioId) guardan el sub de Keycloak,
        // no el UsuarioId local: hay que resolver el nombre por KeycloakId parseado (igual
        // que ListarEquiposQueryHandler).
        var nombres = new Dictionary<Guid, string>();
        foreach (var u in usuarios)
        {
            if (Guid.TryParse(u.KeycloakId, out var keycloakId))
                nombres[keycloakId] = u.Nombre;
        }

        return new EquipoMineResponse(
            equipo.EquipoId,
            equipo.NombreEquipo,
            equipo.Estado.ToString(),
            equipo.Participantes
                .Select(p => new MiembroEquipoResponse(
                    p.UsuarioId,
                    nombres.TryGetValue(p.UsuarioId, out var nombre) ? nombre : "",
                    p.EsLider))
                .ToList());
    }
}
