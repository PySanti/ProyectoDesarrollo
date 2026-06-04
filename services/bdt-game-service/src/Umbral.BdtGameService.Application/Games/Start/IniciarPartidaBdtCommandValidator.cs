using FluentValidation;

namespace Umbral.BdtGameService.Application.Games.Start;

public sealed class IniciarPartidaBdtCommandValidator : AbstractValidator<IniciarPartidaBdtCommand>
{
    public IniciarPartidaBdtCommandValidator()
    {
        RuleFor(command => command.PartidaId).NotEmpty();
        RuleFor(command => command.OperadorUserId).NotEmpty();
    }
}
