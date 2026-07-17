using Umbral.IdentityService.Domain.ValueObjects;
using MediatR;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Application.Exceptions;

using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
namespace Umbral.IdentityService.Application.Handlers.Commands;

public sealed class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand, DeactivateUserResponse>
{
    private readonly IUsuarioRepository _usuarioRepository;

    public DeactivateUserCommandHandler(IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    public async Task<DeactivateUserResponse> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _usuarioRepository.GetByIdAsync(UsuarioLocalId.From(request.UserId), cancellationToken);
        if (user is null)
        {
            throw new UserNotFoundException(request.UserId);
        }

        user.Desactivar();
        await _usuarioRepository.UpdateAsync(user, cancellationToken);

        return new DeactivateUserResponse(user.UsuarioId.Valor, user.Estado.ToString());
    }
}
