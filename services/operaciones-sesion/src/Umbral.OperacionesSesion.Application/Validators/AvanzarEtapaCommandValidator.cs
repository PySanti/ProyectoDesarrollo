using FluentValidation;
using Umbral.OperacionesSesion.Application.Commands;
namespace Umbral.OperacionesSesion.Application.Validators;
public sealed class AvanzarEtapaCommandValidator : AbstractValidator<AvanzarEtapaCommand>
{
    public AvanzarEtapaCommandValidator() => RuleFor(c => c.PartidaId).NotEmpty();
}
