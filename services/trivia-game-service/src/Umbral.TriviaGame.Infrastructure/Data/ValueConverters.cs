using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Infrastructure.Data;

internal static class ValueConverters
{
    public static readonly ValueConverter<TriviaFormId, Guid> TriviaFormIdConverter =
        new(v => v.Value, v => TriviaFormId.Create(v));

    public static readonly ValueConverter<QuestionId, Guid> QuestionIdConverter =
        new(v => v.Value, v => QuestionId.Create(v));

    public static readonly ValueConverter<FormTitle, string> FormTitleConverter =
        new(v => v.Value, v => FormTitle.Create(v));

    public static readonly ValueConverter<QuestionText, string> QuestionTextConverter =
        new(v => v.Value, v => QuestionText.Create(v));

    public static readonly ValueConverter<OptionText, string> OptionTextConverter =
        new(v => v.Value, v => OptionText.Create(v));

    public static readonly ValueConverter<AssignedScore, int> AssignedScoreConverter =
        new(v => v.Value, v => AssignedScore.Create(v));

    public static readonly ValueConverter<TimeLimit, int> TimeLimitConverter =
        new(v => v.Seconds, v => TimeLimit.Create(v));

    public static readonly ValueConverter<OperatorId, string> OperatorIdConverter =
        new(v => v.Value, v => OperatorId.Create(v));

    public static readonly ValueConverter<TiempoInicio, DateTimeOffset> TiempoInicioConverter =
        new(v => v.Value, v => TiempoInicio.Create(v));

    public static readonly ValueConverter<PartidaId, Guid> PartidaIdConverter =
        new(v => v.Value, v => PartidaId.Create(v));

    public static readonly ValueConverter<NombrePartida, string> NombrePartidaConverter =
        new(v => v.Value, v => NombrePartida.Create(v));

    public static readonly ValueConverter<CantidadMinima, int> CantidadMinimaConverter =
        new(v => v.Value, v => CantidadMinima.Create(v));

    public static readonly ValueConverter<CantidadMaximaJugadores, int> CantidadMaximaJugadoresConverter =
        new(v => v.Value, v => CantidadMaximaJugadores.Create(v));

    public static readonly ValueConverter<TriviaInscripcionId, Guid> TriviaInscripcionIdConverter =
        new(v => v.Value, v => TriviaInscripcionId.Create(v));

    public static readonly ValueConverter<CantidadMaximaEquipos, int> CantidadMaximaEquiposConverter =
        new(v => v.Value, v => CantidadMaximaEquipos.Create(v));

    public static readonly ValueConverter<JugadoresPorEquipoMin, int> JugadoresPorEquipoMinConverter =
        new(v => v.Value, v => JugadoresPorEquipoMin.Create(v));

    public static readonly ValueConverter<JugadoresPorEquipoMax, int> JugadoresPorEquipoMaxConverter =
        new(v => v.Value, v => JugadoresPorEquipoMax.Create(v));
}
