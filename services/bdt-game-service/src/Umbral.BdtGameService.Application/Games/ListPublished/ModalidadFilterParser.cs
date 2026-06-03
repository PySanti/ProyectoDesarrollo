using Umbral.BdtGameService.Domain.Enums;

namespace Umbral.BdtGameService.Application.Games.ListPublished;

public static class ModalidadFilterParser
{
    public static bool IsValid(string? value)
    {
        return string.IsNullOrWhiteSpace(value) || TryParse(value, out _);
    }

    public static bool TryParse(string? value, out Modalidad? modalidad)
    {
        modalidad = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (value == nameof(Modalidad.Individual))
        {
            modalidad = Modalidad.Individual;
            return true;
        }

        if (value == nameof(Modalidad.Equipo))
        {
            modalidad = Modalidad.Equipo;
            return true;
        }

        return false;
    }
}
