using Umbral.TriviaGame.Application.Dtos;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.Enums;

namespace Umbral.TriviaGame.Application.Mappers;

internal static class TriviaGameMapper
{
    public static TriviaGameListItemDto ToListItemDto(PartidaTrivia partida)
    {
        return new TriviaGameListItemDto(
            partida.Id.Value,
            partida.Nombre.Value,
            partida.Modalidad.ToString(),
            partida.Estado.ToString(),
            partida.TiempoInicio.Value,
            partida.MinimoParticipantes.Value,
            partida.MaximoJugadores?.Value,
            partida.MaximoEquipos?.Value);
    }

    public static TriviaGameDetailDto ToDto(PartidaTrivia partida)
    {
        return new TriviaGameDetailDto(
            partida.Id.Value,
            partida.Nombre.Value,
            partida.Estado.ToString(),
            partida.Modalidad.ToString(),
            partida.ModoInicio.ToString(),
            partida.FormularioAsociadoId.Value,
            partida.TiempoInicio.Value,
            partida.MinimoParticipantes.Value,
            partida.MaximoJugadores?.Value,
            partida.MaximoEquipos?.Value,
            partida.MinimoJugadoresPorEquipo?.Value,
            partida.MaximoJugadoresPorEquipo?.Value,
            partida.CreatedAtUtc,
            partida.StartedAtUtc);
    }

    public static Modalidad ParseModalidad(string value)
    {
        var trimmed = value.Trim().ToLowerInvariant();
        return trimmed switch
        {
            "individual" => Modalidad.Individual,
            "equipo" => Modalidad.Equipo,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value,
                $"Modalidad inválida: {value}. Valores permitidos: Individual, Equipo.")
        };
    }

    public static Modalidad ToModalidad(string value)
    {
        return value switch
        {
            "Individual" => Modalidad.Individual,
            "Equipo" => Modalidad.Equipo,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value,
                $"Modalidad inválida: {value}")
        };
    }

    public static ModoInicio ToModoInicio(string value)
    {
        return value switch
        {
            "Manual" => ModoInicio.Manual,
            "Automatico" => ModoInicio.Automatico,
            "ManualYAutomatico" => ModoInicio.ManualYAutomatico,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value,
                $"Modo de inicio inválido: {value}")
        };
    }
}
