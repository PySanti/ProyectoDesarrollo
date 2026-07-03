using FluentValidation;
using Umbral.OperacionesSesion.Application.Commands;

namespace Umbral.OperacionesSesion.Application.Validators;

public sealed class ResponderConvocatoriaCommandValidator : AbstractValidator<ResponderConvocatoriaCommand>
{
    public ResponderConvocatoriaCommandValidator()
    {
        RuleFor(x => x.ConvocatoriaId).NotEmpty();
        RuleFor(x => x.UsuarioId).NotEmpty();
    }
}
