import { TriviaGameListItem } from "../features/trivia/types";
import { buildPublishedTriviaGamesUrl } from "../features/trivia/triviaPublishedGamesModel.js";

export type TriviaModalidadFilter = "Individual" | "Equipo";

export interface TriviaJoinResponse {
  inscripcionId: string;
  partidaId: string;
  fechaInscripcion: string;
}

export interface TriviaLobbyResponse {
  partidaId: string;
  nombre: string;
  estado: string;
  modalidad: "Individual" | "Equipo";
  tiempoInicio: string;
  minimoParticipantes: number;
  maximoJugadores: number;
  participantesActual: number;
  participantes: Array<{
    inscripcionId: string;
    usuarioId: string;
    fechaInscripcion: string;
  }>;
}

export interface TriviaAnswerResponse {
  respuestaId: string;
  partidaId: string;
  preguntaId: string;
  esCorrecta: boolean;
  puntajeObtenido: number;
  tiempoEmpleadoSegundos: number;
  fechaRespuesta: string;
}

export interface TriviaQuestionResultResponse {
  preguntaId: string;
  textoPregunta: string;
  opcionCorrectaIndex: number;
  opcionCorrectaText: string;
  miOpcionIndex: number | null;
  miOpcionText: string | null;
  esCorrecta: boolean | null;
  puntajeObtenido: number;
  tiempoEmpleadoSegundos: number;
  motivoCierre: string;
}

export interface TriviaScoreResponse {
  partidaId: string;
  puntajeAcumulado: number;
  tiempoAcumuladoSegundos: number;
  respuestasCorrectas: number;
  totalRespuestas: number;
}

export class TriviaMobileApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
  ) {
    super(message);
    this.name = "TriviaMobileApiError";
  }
}

type BuiltTriviaGamesUrl =
  | { ok: true; url: string }
  | { ok: false; message: string };

type GetPublishedTriviaGamesParams = {
  apiBaseUrl: string;
  token: string;
  modalidad?: TriviaModalidadFilter;
  fetchImpl?: typeof fetch;
};

export async function getPublishedTriviaGames({
  apiBaseUrl,
  token,
  modalidad,
  fetchImpl = fetch,
}: GetPublishedTriviaGamesParams): Promise<TriviaGameListItem[]> {
  const builtUrl = buildPublishedTriviaGamesUrl(apiBaseUrl, modalidad ?? "Todas") as BuiltTriviaGamesUrl;
  if (!builtUrl.ok) {
    throw new TriviaMobileApiError(builtUrl.message, 400);
  }

  const response = await fetchImpl(builtUrl.url, {
    method: "GET",
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });

  const body = (await response.json().catch(() => ({}))) as
    | { message?: string }
    | TriviaGameListItem[];

  if (!response.ok) {
    const message =
      (body as { message?: string }).message ??
      `No se pudieron cargar las partidas de Trivia. Status=${response.status}`;
    throw new TriviaMobileApiError(message, response.status);
  }

  return body as TriviaGameListItem[];
}

async function parseTriviaResponse<T>(response: Response, defaultMessage: string): Promise<T> {
  const body = (await response.json().catch(() => ({}))) as { message?: string } | T;

  if (!response.ok) {
    const message = (body as { message?: string }).message ?? `${defaultMessage}. Status=${response.status}`;
    throw new TriviaMobileApiError(message, response.status);
  }

  return body as T;
}

type TriviaRequestParams = {
  apiBaseUrl: string;
  token: string;
  partidaId: string;
  fetchImpl?: typeof fetch;
};

function base(apiBaseUrl: string): string {
  return apiBaseUrl.replace(/\/$/, "");
}

export async function joinIndividualTriviaGame({
  apiBaseUrl,
  token,
  partidaId,
  fetchImpl = fetch,
}: TriviaRequestParams): Promise<TriviaJoinResponse> {
  const response = await fetchImpl(`${base(apiBaseUrl)}/api/trivia-games/${partidaId}/join`, {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
  });

  return parseTriviaResponse<TriviaJoinResponse>(response, "No se pudo unir a la Trivia");
}

export async function getTriviaLobby({
  apiBaseUrl,
  token,
  partidaId,
  fetchImpl = fetch,
}: TriviaRequestParams): Promise<TriviaLobbyResponse> {
  const response = await fetchImpl(`${base(apiBaseUrl)}/api/trivia-games/${partidaId}/lobby`, {
    method: "GET",
    headers: { Authorization: `Bearer ${token}` },
  });

  return parseTriviaResponse<TriviaLobbyResponse>(response, "No se pudo cargar la espera de Trivia");
}

export async function answerTriviaQuestion({
  apiBaseUrl,
  token,
  partidaId,
  preguntaId,
  opcionIndex,
  fetchImpl = fetch,
}: TriviaRequestParams & { preguntaId: string; opcionIndex: number }): Promise<TriviaAnswerResponse> {
  const response = await fetchImpl(`${base(apiBaseUrl)}/api/trivia-games/${partidaId}/questions/${preguntaId}/answer`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify({ opcionIndex }),
  });

  return parseTriviaResponse<TriviaAnswerResponse>(response, "No se pudo registrar la respuesta");
}

export async function getTriviaQuestionResult({
  apiBaseUrl,
  token,
  partidaId,
  preguntaId,
  fetchImpl = fetch,
}: TriviaRequestParams & { preguntaId: string }): Promise<TriviaQuestionResultResponse> {
  const response = await fetchImpl(`${base(apiBaseUrl)}/api/trivia-games/${partidaId}/questions/${preguntaId}/result`, {
    method: "GET",
    headers: { Authorization: `Bearer ${token}` },
  });

  return parseTriviaResponse<TriviaQuestionResultResponse>(response, "No se pudo cargar el resultado de la pregunta");
}

export async function getTriviaScore({
  apiBaseUrl,
  token,
  partidaId,
  fetchImpl = fetch,
}: TriviaRequestParams): Promise<TriviaScoreResponse> {
  const response = await fetchImpl(`${base(apiBaseUrl)}/api/trivia-games/${partidaId}/score`, {
    method: "GET",
    headers: { Authorization: `Bearer ${token}` },
  });

  return parseTriviaResponse<TriviaScoreResponse>(response, "No se pudo cargar el puntaje de Trivia");
}
