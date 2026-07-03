using FluentValidation;
using Umbral.OperacionesSesion.Application.Commands;

namespace Umbral.OperacionesSesion.Application.Validators;

public sealed class PublicarPartidaCommandValidator : AbstractValidator<PublicarPartidaCommand>
{
    public PublicarPartidaCommandValidator()
    {
        RuleFor(x => x.PartidaId).NotEmpty();
    }
}
