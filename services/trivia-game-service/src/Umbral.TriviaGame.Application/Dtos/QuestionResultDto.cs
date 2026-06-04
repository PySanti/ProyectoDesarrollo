namespace Umbral.TriviaGame.Application.Dtos;

public sealed record QuestionResultDto(
    Guid PreguntaId,
    string TextoPregunta,
    int OpcionCorrectaIndex,
    string OpcionCorrectaText,
    int? MiOpcionIndex,
    string? MiOpcionText,
    bool? EsCorrecta,
    int PuntajeObtenido,
    double TiempoEmpleadoSegundos,
    string MotivoCierre
);
