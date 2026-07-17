using MediatR;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Queries;
using Umbral.IdentityService.Domain.Abstractions.Persistence;

namespace Umbral.IdentityService.Application.Handlers.Queries;

public sealed class ListarEquiposQueryHandler
    : IRequestHandler<ListarEquiposQuery, IReadOnlyList<EquipoAdminItemResponse>>
{
    private readonly IEquipoRepository _equipos;
    private readonly IUsuarioRepository _usuarios;

    public ListarEquiposQueryHandler(IEquipoRepository equipos, IUsuarioRepository usuarios)
    {
        _equipos = equipos;
        _usuarios = usuarios;
    }

    public async Task<IReadOnlyList<EquipoAdminItemResponse>> Handle(
        ListarEquiposQuery request, CancellationToken cancellationToken)
    {
        var equipos = await _equipos.GetAllAsync(cancellationToken);
        var usuarios = await _usuarios.GetAllAsync(cancellationToken);
        // Los miembros de equipo (ParticipanteEquipo.SubjectId) guardan el sub de Keycloak,
        // no el UsuarioId local: hay que resolver el nombre por KeycloakId parseado.
        var nombres = new Dictionary<Guid, string>();
        foreach (var u in usuarios)
        {
            if (Guid.TryParse(u.KeycloakId, out var keycloakId))
                nombres[keycloakId] = u.Nombre;
        }

        return equipos
            .Select(e => new EquipoAdminItemResponse(
                e.EquipoId,
                e.NombreEquipo,
                e.Estado.ToString(),
                e.Participantes
                    .Select(p => new MiembroEquipoAdminResponse(
                        p.SubjectId,
                        nombres.TryGetValue(p.SubjectId, out var nombre) ? nombre : "",
                        p.EsLider))
                    .ToList()))
            .ToList();
    }
}
