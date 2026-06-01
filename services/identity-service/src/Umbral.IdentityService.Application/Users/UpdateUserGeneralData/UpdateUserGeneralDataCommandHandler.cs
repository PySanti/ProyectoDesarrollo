using MediatR;
using Umbral.IdentityService.Application.Abstractions.Persistence;
using Umbral.IdentityService.Application.Exceptions;

namespace Umbral.IdentityService.Application.Users.UpdateUserGeneralData;

public sealed class UpdateUserGeneralDataCommandHandler : IRequestHandler<UpdateUserGeneralDataCommand, UpdateUserGeneralDataResponse>
{
    private readonly IUsuarioRepository _usuarioRepository;

    public UpdateUserGeneralDataCommandHandler(IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    public async Task<UpdateUserGeneralDataResponse> Handle(UpdateUserGeneralDataCommand request, CancellationToken cancellationToken)
    {
        var user = await _usuarioRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            throw new UserNotFoundException(request.UserId);
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await _usuarioRepository.ExistsByEmailAsync(normalizedEmail, request.UserId, cancellationToken))
        {
            throw new DuplicateEmailException(normalizedEmail);
        }

        user.EditarDatosGenerales(request.Name, normalizedEmail);
        await _usuarioRepository.UpdateAsync(user, cancellationToken);

        return new UpdateUserGeneralDataResponse(
            user.UsuarioId,
            user.Nombre,
            user.Correo,
            user.Rol.ToString(),
            user.Estado.ToString());
    }
}
