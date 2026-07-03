using FluentValidation;
using Umbral.OperacionesSesion.Application.Commands;
namespace Umbral.OperacionesSesion.Application.Validators;
public sealed class AvanzarPreguntaCommandValidator : AbstractValidator<AvanzarPreguntaCommand>
{
    public AvanzarPreguntaCommandValidator() => RuleFor(x => x.PartidaId).NotEmpty();
}
