using MediatR;
using Umbral.IdentityService.Application.Abstractions.Persistence;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Application.Users.Common;

namespace Umbral.IdentityService.Application.Users.GetUserById;

public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDetailResponse>
{
    private readonly IUsuarioRepository _usuarioRepository;

    public GetUserByIdQueryHandler(IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    public async Task<UserDetailResponse> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _usuarioRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            throw new UserNotFoundException(request.UserId);
        }

        return new UserDetailResponse(
            user.UsuarioId,
            user.KeycloakId,
            user.Nombre,
            user.Correo,
            user.Rol.ToString(),
            user.Estado.ToString());
    }
}
