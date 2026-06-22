import { TriviaAnswerResponse, TriviaQuestionResultResponse, TriviaScoreResponse } from "../../../api/triviaApi";

/**
 * ── PLANTILLA DE INTEGRACIÓN — Partida de Trivia "en vivo" (maqueta G2) ───────────────────────────
 *
 * La pantalla `TriviaLivePlayScreen` depende **solo** de la interfaz `LiveTriviaSource`. Hoy existe una
 * implementación mock (`mockLiveTriviaSource.ts`) que permite ver y probar el registro de juego sin
 * backend. Cuando el backend de ejecución sincronizada exista, un agente futuro crea una
 * `BackendLiveTriviaSource` que cumpla esta MISMA interfaz y la inyecta en el container; la pantalla no
 * cambia.
 *
 * Estado del contrato por pieza (qué ya existe vs. qué falta en backend):
 *
 *   • `onQuestion` (pregunta activa empujada en tiempo real)  → **FALTA**. Es el contrato pendiente:
 *       SignalR del runtime de Trivia en **Operaciones de Sesion** (a través del gateway) debe empujar la
 *       pregunta activa + su ventana de tiempo al participante cuando el operador la abre, y empujar
 *       `null`/evento de cierre al terminar. Ver `contracts/events/` (eventos de Trivia). El timer es
 *       **autoritativo del servidor**: el cliente sincroniza su cuenta regresiva a la deadline del server.
 *
 *   • `submit(preguntaId, opcionIndex)`  → **YA EXISTE**: `answerTriviaQuestion()` en `api/triviaApi.ts`
 *       (HU-26). Devuelve `TriviaAnswerResponse`. `opcionIndex === null` = se agotó el tiempo sin responder.
 *
 *   • `questionResult(preguntaId)`  → **YA EXISTE**: `getTriviaQuestionResult()` (HU-28). Devuelve la
 *       opción correcta para revelar tras responder. `TriviaQuestionResultResponse`.
 *
 *   • `finalScore()`  → puntaje propio **YA EXISTE**: `getTriviaScore()` (HU-29) → `TriviaScoreResponse`.
 *       El **ranking** se alimenta del endpoint/evento de ranking de Trivia (`PuntajeAcumulado`,
 *       `RankingTriviaActualizado`). NOTA: esto es **Trivia** (puntaje); NO confundir con el ranking de
 *       BDT, que ordena por puntaje acumulado de etapas ganadas (desempate por tiempo acumulado).
 *
 *   • `advance()`  → en la implementación real es probablemente un **no-op** (el servidor empuja la
 *       siguiente pregunta por su cuenta, dirigido por el operador). En el mock avanza el guion para
 *       permitir una demo auto-administrada (botón "Siguiente"). Mantener para no atar la pantalla a la
 *       cadencia del mock.
 *
 * Regla intacta: esto es **visual + maqueta**, no cambia contratos ni reglas. Cuando se implemente el
 * backend real, NO se inventan campos: se mapean los reales a estas formas.
 */

/** Pregunta activa empujada por el servidor (contrato en tiempo real pendiente). */
export interface LiveQuestion {
  preguntaId: string;
  /** Posición 1-based en la partida. */
  index: number;
  /** Total de preguntas de la partida. */
  total: number;
  texto: string;
  /** Textos de opción; el índice del arreglo es el `opcionIndex` (0-based) que espera `submit`. */
  opciones: string[];
  /** Ventana de respuesta en segundos (autoritativa del servidor). */
  limiteSegundos: number;
}

/** Una fila del ranking de Trivia (ordenado por puntaje acumulado). */
export interface LiveRankingEntry {
  posicion: number;
  participante: string;
  puntaje: number;
  /** Marca la fila del propio participante (para resaltar "tú"). */
  esTu: boolean;
}

/** Cierre de la partida: puntaje propio (endpoint real) + posición + ranking. */
export interface LiveFinalScore {
  score: TriviaScoreResponse;
  posicion: number;
  ranking: LiveRankingEntry[];
}

/**
 * Fuente de una partida de Trivia en vivo. La pantalla de juego consume solo esta interfaz; la
 * implementación real la cumple con SignalR (push de pregunta/cierre) + REST (responder, resultado,
 * puntaje, ranking). Ver la cabecera de este archivo para el mapeo pieza por pieza.
 */
export interface LiveTriviaSource {
  /** Abre/conecta la fuente. Real: abre la conexión SignalR y se suscribe a la partida. */
  start(): void;
  /** Libera recursos. Real: cierra la conexión SignalR. */
  stop(): void;
  /**
   * Suscribe a la pregunta activa. El callback recibe cada nueva pregunta empujada, y `null` cuando la
   * partida termina. Devuelve una función para desuscribir.
   */
  onQuestion(cb: (q: LiveQuestion | null) => void): () => void;
  /**
   * Envía la opción elegida (`null` = se agotó el tiempo). Real: `answerTriviaQuestion()` (HU-26).
   */
  submit(preguntaId: string, opcionIndex: number | null): Promise<TriviaAnswerResponse>;
  /**
   * Revela el resultado de una pregunta (opción correcta, mi opción). Real: `getTriviaQuestionResult()`
   * (HU-28).
   */
  questionResult(preguntaId: string): Promise<TriviaQuestionResultResponse>;
  /**
   * Pide la siguiente pregunta (demo auto-administrada). Real: probablemente no-op (el servidor empuja
   * por su cuenta dirigido por el operador).
   */
  advance(): void;
  /** Puntaje propio + ranking final al terminar. Real: `getTriviaScore()` (HU-29) + endpoint de ranking. */
  finalScore(): Promise<LiveFinalScore>;
}
