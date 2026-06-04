import assert from "node:assert/strict";
import test from "node:test";
import {
  TEAM_TRIVIA_LEADER_WARNING,
  buildTriviaResultSummary,
  buildTriviaScoreSummary,
} from "../src/features/trivia/triviaParticipantScreenModel.js";

test("HU-13 warning text remains consistent for team Trivia access", () => {
  assert.equal(TEAM_TRIVIA_LEADER_WARNING, "Debes ser líder de un equipo para entrar en este evento");
});

test("HU-29 score summary displays backend-calculated score without recalculation", () => {
  const summary = buildTriviaScoreSummary({
    puntajeAcumulado: 300,
    respuestasCorrectas: 2,
    totalRespuestas: 3,
    tiempoAcumuladoSegundos: 15,
  });

  assert.deepEqual(summary, {
    title: "300 puntos",
    correctAnswers: "2",
    totalAnswers: "3",
    accumulatedTime: "15s",
  });
});

test("HU-28 result summary shows correct answer and selected answer", () => {
  const summary = buildTriviaResultSummary({
    opcionCorrectaText: "Caracas",
    miOpcionText: "Caracas",
    esCorrecta: true,
    puntajeObtenido: 100,
  });

  assert.deepEqual(summary, {
    selectedAnswer: "Caracas",
    correctAnswer: "Caracas",
    status: "Correcta",
    score: 100,
  });
});
