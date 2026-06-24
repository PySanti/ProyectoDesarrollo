using MediatR;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Application.Queries;
using Umbral.IdentityService.Application.DTOs;
namespace Umbral.IdentityService.Application.Handlers.Queries;

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
