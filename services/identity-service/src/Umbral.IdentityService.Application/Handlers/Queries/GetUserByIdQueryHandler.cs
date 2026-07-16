using Umbral.IdentityService.Domain.ValueObjects;
using MediatR;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Application.Queries;
using Umbral.IdentityService.Application.DTOs;
namespace Umbral.IdentityService.Application.Handlers.Queries;

public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDetailResponse>
{
    private readonly IUsuarioRepository _usuarioRepository;

    public GetUserByIdQueryHandler(IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    public async Task<UserDetailResponse> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _usuarioRepository.GetByIdAsync(UsuarioLocalId.From(request.UserId), cancellationToken);
        if (user is null)
        {
            throw new UserNotFoundException(request.UserId);
        }

        return new UserDetailResponse(
            user.UsuarioId.Valor,
            user.KeycloakId,
            user.Nombre,
            user.Correo,
            user.Rol.ToString(),
            user.Estado.ToString());
    }
}
