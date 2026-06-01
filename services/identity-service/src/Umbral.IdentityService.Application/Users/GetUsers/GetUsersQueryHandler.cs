using MediatR;
using Umbral.IdentityService.Application.Abstractions.Persistence;
using Umbral.IdentityService.Application.Users.Common;

namespace Umbral.IdentityService.Application.Users.GetUsers;

public sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, IReadOnlyList<UserSummaryResponse>>
{
    private readonly IUsuarioRepository _usuarioRepository;

    public GetUsersQueryHandler(IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    public async Task<IReadOnlyList<UserSummaryResponse>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var usuarios = await _usuarioRepository.GetAllAsync(cancellationToken);

        return usuarios
            .Select(user => new UserSummaryResponse(
                user.UsuarioId,
                user.KeycloakId,
                user.Nombre,
                user.Correo,
                user.Rol.ToString(),
                user.Estado.ToString()))
            .ToList();
    }
}
