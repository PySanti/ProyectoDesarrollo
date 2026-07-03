using FluentValidation;
using Umbral.OperacionesSesion.Application.Commands;

namespace Umbral.OperacionesSesion.Application.Validators;

public sealed class IniciarPartidaCommandValidator : AbstractValidator<IniciarPartidaCommand>
{
    public IniciarPartidaCommandValidator()
    {
        RuleFor(x => x.PartidaId).NotEmpty();
    }
}
