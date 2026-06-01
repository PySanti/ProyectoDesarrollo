using MediatR;
using Umbral.IdentityService.Application.Abstractions.Persistence;
using Umbral.IdentityService.Application.Exceptions;

namespace Umbral.IdentityService.Application.Users.DeactivateUser;

public sealed class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand, DeactivateUserResponse>
{
    private readonly IUsuarioRepository _usuarioRepository;

    public DeactivateUserCommandHandler(IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    public async Task<DeactivateUserResponse> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _usuarioRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            throw new UserNotFoundException(request.UserId);
        }

        user.Desactivar();
        await _usuarioRepository.UpdateAsync(user, cancellationToken);

        return new DeactivateUserResponse(user.UsuarioId, user.Estado.ToString());
    }
}
