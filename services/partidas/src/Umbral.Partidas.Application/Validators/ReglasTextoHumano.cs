using System.Linq;
using FluentValidation;

namespace Umbral.Partidas.Application.Validators;

public static class ReglasTextoHumano
{
    /// <summary>
    /// Texto legible para personas: obligatorio y con al menos una letra tras recortar espacios.
    /// Rechaza entradas como "   " o "****". Usar Cascade(CascadeMode.Stop) en el RuleFor para que
    /// un valor vacío muestre solo "obligatorio" y no ademas "debe tener letra". <paramref name="maximo"/>
    /// nulo omite el límite de longitud (p.ej. AreaBusqueda es texto libre sin cota).
    /// </summary>
    public static IRuleBuilderOptions<T, string> TextoHumano<T>(
        this IRuleBuilder<T, string> rule, int? maximo = null)
    {
        var built = rule
            .NotEmpty()
            .Must(TieneAlMenosUnaLetra).WithMessage("'{PropertyName}' debe contener al menos una letra.");

        return maximo is int m ? built.MaximumLength(m) : built;
    }

    private static bool TieneAlMenosUnaLetra(string? value)
        => !string.IsNullOrWhiteSpace(value) && value.Any(char.IsLetter);
}
