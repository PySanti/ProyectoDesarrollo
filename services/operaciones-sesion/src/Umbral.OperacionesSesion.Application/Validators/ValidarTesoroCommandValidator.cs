using FluentValidation;
using Umbral.OperacionesSesion.Application.Commands;
namespace Umbral.OperacionesSesion.Application.Validators;
public sealed class ValidarTesoroCommandValidator : AbstractValidator<ValidarTesoroCommand>
{
    public ValidarTesoroCommandValidator()
    {
        RuleFor(c => c.PartidaId).NotEmpty();
        RuleFor(c => c.ImagenBase64).NotEmpty();
        RuleFor(c => c.ImagenBase64)
            .Must(SerBase64Valido)
            .When(c => !string.IsNullOrEmpty(c.ImagenBase64))
            .WithMessage("ImagenBase64 debe ser una cadena Base64 válida.");
        // ParticipanteId NO se valida: proviene del claim sub, no del body.
    }

    private static bool SerBase64Valido(string valor)
    {
        Span<byte> buffer = new byte[((valor.Length * 3) + 3) / 4];
        return Convert.TryFromBase64String(valor, buffer, out _);
    }
}
