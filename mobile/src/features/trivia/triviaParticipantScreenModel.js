export const TEAM_TRIVIA_LEADER_WARNING = "Debes ser líder de un equipo para entrar en este evento";

export function buildTriviaScoreSummary(score) {
  return {
    title: `${score?.puntajeAcumulado ?? 0} puntos`,
    correctAnswers: `${score?.respuestasCorrectas ?? 0}`,
    totalAnswers: `${score?.totalRespuestas ?? 0}`,
    accumulatedTime: `${score?.tiempoAcumuladoSegundos ?? 0}s`,
  };
}

export function buildTriviaResultSummary(result) {
  return {
    selectedAnswer: result?.miOpcionText ?? "Sin respuesta",
    correctAnswer: result?.opcionCorrectaText ?? "No disponible",
    status: result?.esCorrecta ? "Correcta" : "Incorrecta",
    score: result?.puntajeObtenido ?? 0,
  };
}
