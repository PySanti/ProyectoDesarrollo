using System.Linq;
using FluentValidation;

namespace Umbral.IdentityService.Application.Validators;

public static class ReglasTextoHumano
{
    /// <summary>
    /// Texto legible para personas: obligatorio y con al menos una letra tras recortar espacios.
    /// Rechaza entradas como "   " o "****" que sistemas externos (p.ej. Keycloak) terminan
    /// rechazando de forma opaca si se las deja pasar. Usar Cascade(CascadeMode.Stop) en el
    /// RuleFor para que un valor vacío muestre solo "obligatorio" y no ademas "debe tener letra".
    /// </summary>
    public static IRuleBuilderOptions<T, string> TextoHumano<T>(
        this IRuleBuilder<T, string> rule, int maximo)
    {
        return rule
            .NotEmpty()
            .Must(TieneAlMenosUnaLetra).WithMessage("'{PropertyName}' debe contener al menos una letra.")
            .MaximumLength(maximo);
    }

    private static bool TieneAlMenosUnaLetra(string? value)
        => !string.IsNullOrWhiteSpace(value) && value.Any(char.IsLetter);
}
