import { TriviaAnswerResponse, TriviaQuestionResultResponse, TriviaScoreResponse } from "../../../api/triviaApi";
import { LiveFinalScore, LiveQuestion, LiveRankingEntry, LiveTriviaSource } from "./liveTriviaTypes";

/**
 * Implementación **mock** de `LiveTriviaSource` para la maqueta G2: un guion fijo de preguntas que deja
 * ver y probar el flujo en vivo (countdown, reacción, count-up, ranking) sin backend. NO toca red ni
 * contratos; sus respuestas imitan las formas reales (`TriviaAnswerResponse`, etc.) para que el mapeo a
 * la implementación real sea 1:1. Sustituible por una `BackendLiveTriviaSource` que cumpla la misma
 * interfaz (ver `liveTriviaTypes.ts`).
 */

interface ScriptedQuestion {
  preguntaId: string;
  texto: string;
  opciones: string[];
  correctaIndex: number;
  limiteSegundos: number;
  /** Puntos si se responde correctamente (el backend real los calcula con su propia regla/tiempo). */
  puntos: number;
}

const SCRIPT: ScriptedQuestion[] = [
  {
    preguntaId: "demo-q1",
    texto: "¿Cuál es la capital de Francia?",
    opciones: ["Madrid", "París", "Roma", "Berlín"],
    correctaIndex: 1,
    limiteSegundos: 15,
    puntos: 100,
  },
  {
    preguntaId: "demo-q2",
    texto: "¿Cuánto es 2 + 2?",
    opciones: ["3", "4", "5", "22"],
    correctaIndex: 1,
    limiteSegundos: 12,
    puntos: 100,
  },
  {
    preguntaId: "demo-q3",
    texto: "¿De qué color es el cielo despejado de día?",
    opciones: ["Verde", "Azul", "Rojo", "Gris"],
    correctaIndex: 1,
    limiteSegundos: 10,
    puntos: 100,
  },
];

const DEMO_PARTIDA_ID = "demo-partida";

export function createMockLiveTriviaSource(): LiveTriviaSource {
  let current = -1;
  let listener: ((q: LiveQuestion | null) => void) | null = null;
  /** opcionIndex elegido por pregunta (`null` = sin responder a tiempo). */
  const chosen: Record<string, number | null> = {};

  function toLive(index: number): LiveQuestion {
    const q = SCRIPT[index];
    return {
      preguntaId: q.preguntaId,
      index: index + 1,
      total: SCRIPT.length,
      texto: q.texto,
      opciones: q.opciones,
      limiteSegundos: q.limiteSegundos,
    };
  }

  function push() {
    if (!listener) return;
    listener(current >= 0 && current < SCRIPT.length ? toLive(current) : null);
  }

  return {
    start() {
      current = 0;
      push();
    },
    stop() {
      listener = null;
    },
    onQuestion(cb) {
      listener = cb;
      return () => {
        if (listener === cb) listener = null;
      };
    },
    async submit(preguntaId, opcionIndex) {
      chosen[preguntaId] = opcionIndex;
      const q = SCRIPT.find((s) => s.preguntaId === preguntaId)!;
      const esCorrecta = opcionIndex === q.correctaIndex;
      const response: TriviaAnswerResponse = {
        respuestaId: `demo-resp-${preguntaId}`,
        partidaId: DEMO_PARTIDA_ID,
        preguntaId,
        esCorrecta,
        puntajeObtenido: esCorrecta ? q.puntos : 0,
        tiempoEmpleadoSegundos: 0,
        fechaRespuesta: new Date().toISOString(),
      };
      return response;
    },
    async questionResult(preguntaId) {
      const q = SCRIPT.find((s) => s.preguntaId === preguntaId)!;
      const mine = chosen[preguntaId] ?? null;
      const esCorrecta = mine === q.correctaIndex;
      const result: TriviaQuestionResultResponse = {
        preguntaId,
        textoPregunta: q.texto,
        opcionCorrectaIndex: q.correctaIndex,
        opcionCorrectaText: q.opciones[q.correctaIndex],
        miOpcionIndex: mine,
        miOpcionText: mine === null ? null : q.opciones[mine],
        esCorrecta: mine === null ? null : esCorrecta,
        puntajeObtenido: esCorrecta ? q.puntos : 0,
        tiempoEmpleadoSegundos: 0,
        motivoCierre: mine === null ? "Tiempo agotado" : "Respuesta registrada",
      };
      return result;
    },
    advance() {
      current += 1;
      push();
    },
    async finalScore() {
      const correctas = SCRIPT.filter((q) => chosen[q.preguntaId] === q.correctaIndex);
      const puntaje = correctas.reduce((sum, q) => sum + q.puntos, 0);
      const score: TriviaScoreResponse = {
        partidaId: DEMO_PARTIDA_ID,
        puntajeAcumulado: puntaje,
        tiempoAcumuladoSegundos: 0,
        respuestasCorrectas: correctas.length,
        totalRespuestas: SCRIPT.length,
      };
      // Ranking de ejemplo: el participante intercalado con rivales ficticios, ordenado por puntaje.
      // Puntajes elegidos para que el puesto refleje tu desempeño: 3/3 (300) = 1.º, 2/3 (200) = 2.º,
      // 1/3 (100) = 3.º, 0/3 = 4.º.
      const rivales: Array<{ participante: string; puntaje: number }> = [
        { participante: "Valentina", puntaje: 250 },
        { participante: "Mateo", puntaje: 150 },
        { participante: "Sofía", puntaje: 50 },
      ];
      const filas = [...rivales, { participante: "Tú", puntaje }]
        .sort((a, b) => b.puntaje - a.puntaje)
        .map<LiveRankingEntry>((r, i) => ({
          posicion: i + 1,
          participante: r.participante,
          puntaje: r.puntaje,
          esTu: r.participante === "Tú",
        }));
      const posicion = filas.find((f) => f.esTu)?.posicion ?? filas.length;
      return { score, posicion, ranking: filas };
    },
  };
}
