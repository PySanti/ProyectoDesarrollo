using FluentValidation;

namespace Umbral.BdtGameService.Application.Games.Create;

public sealed class CrearPartidaBdtCommandValidator : AbstractValidator<CrearPartidaBdtCommand>
{
    public CrearPartidaBdtCommandValidator()
    {
        RuleFor(command => command.Nombre).NotEmpty().MaximumLength(150);
        RuleFor(command => command.AreaBusqueda).NotEmpty().MaximumLength(500);
        RuleFor(command => command.Modalidad).NotEmpty().Must(BeValidModalidad).WithMessage("Modalidad invalida.");
        RuleFor(command => command.MinimoParticipantes).GreaterThan(0);
        RuleFor(command => command.ModoInicio).NotEmpty().Must(BeValidModoInicio).WithMessage("ModoInicio invalido.");
        RuleFor(command => command.Etapas)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Must(etapas => etapas.Count > 0)
            .WithMessage("Debe definir al menos una etapa.")
            .Must(etapas => etapas.Select(etapa => etapa.Orden).Distinct().Count() == etapas.Count)
            .WithMessage("No se permiten etapas con orden duplicado.");
        RuleForEach(command => command.Etapas)
            .ChildRules(etapa =>
            {
                etapa.RuleFor(value => value.Orden).GreaterThan(0);
                etapa.RuleFor(value => value.CodigoQrEsperado).NotEmpty().MaximumLength(250);
                etapa.RuleFor(value => value.TiempoLimiteSegundos).GreaterThan(0);
            })
            .When(command => command.Etapas is not null);
    }

    private static bool BeValidModalidad(string value)
    {
        return string.Equals(value, "Individual", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "Equipo", StringComparison.OrdinalIgnoreCase);
    }

    private static bool BeValidModoInicio(string value)
    {
        return string.Equals(value, "Manual", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "Automatico", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "ManualYAutomatico", StringComparison.OrdinalIgnoreCase);
    }
}
