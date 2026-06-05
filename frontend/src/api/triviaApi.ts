export type TriviaModalidad = "Individual" | "Equipo";
export type TriviaModoInicio = "Manual" | "Automatico" | "ManualYAutomatico";

export interface CreateTriviaGameRequest {
  nombre: string;
  modalidad: TriviaModalidad;
  modoInicio: TriviaModoInicio;
  formularioId: string;
  tiempoInicio: string;
  minimoParticipantes: number;
  maximoJugadores: number | null;
  maximoEquipos: number | null;
  minimoJugadoresPorEquipo: number | null;
  maximoJugadoresPorEquipo: number | null;
}

export interface TriviaGameDetail {
  id: string;
  nombre: string;
  estado: string;
  modalidad: TriviaModalidad;
  modoInicio: TriviaModoInicio;
  formularioId: string;
  tiempoInicio: string;
  minimoParticipantes: number;
  maximoJugadores: number | null;
  maximoEquipos: number | null;
  minimoJugadoresPorEquipo: number | null;
  maximoJugadoresPorEquipo: number | null;
  createdAtUtc: string;
  startedAtUtc: string | null;
}

export interface TriviaGameListItem {
  id: string;
  nombre: string;
  modalidad: TriviaModalidad;
  estado: string;
  tiempoInicio: string;
  minimoParticipantes: number;
  maximoJugadores: number | null;
  maximoEquipos: number | null;
}

export interface TriviaFormListItem {
  id: string;
  title: string;
  isComplete: boolean;
  questionsCount: number;
  createdAtUtc: string;
}

export interface CreateTriviaFormOptionRequest {
  text: string;
  isCorrect: boolean;
}

export interface CreateTriviaFormQuestionRequest {
  text: string;
  assignedScore: number;
  timeLimitSeconds: number;
  displayOrder: number;
  options: CreateTriviaFormOptionRequest[];
}

export interface CreateTriviaFormRequest {
  title: string;
  questions: CreateTriviaFormQuestionRequest[];
}

export interface TriviaFormDetail extends TriviaFormListItem {
  incompleteReasons: string[];
  updatedAtUtc: string;
  questions: Array<{
    id: string;
    text: string;
    assignedScore: number;
    timeLimitSeconds: number;
    displayOrder: number;
    options: Array<{ index: number; text: string; isCorrect: boolean }>;
  }>;
}

export interface TriviaLobbyParticipant {
  inscripcionId: string;
  usuarioId: string;
  fechaInscripcion: string;
}

export interface TriviaGameLobby {
  partidaId: string;
  nombre: string;
  estado: string;
  modalidad: TriviaModalidad;
  tiempoInicio: string;
  minimoParticipantes: number;
  maximoJugadores: number;
  participantesActual: number;
  participantes: TriviaLobbyParticipant[];
}

export interface TriviaTeamLobbyItem {
  equipoId: string;
  fechaInscripcion: string;
}

export interface TriviaRankingEntry {
  usuarioId: string;
  puntajeAcumulado: number;
  tiempoAcumuladoSegundos: number;
  respuestasCorrectas: number;
  totalRespuestas: number;
  posicion: number;
}

export class TriviaApiError extends Error {
  constructor(
    message: string,
    public readonly statusCode: number
  ) {
    super(message);
    this.name = "TriviaApiError";
  }
}

const baseUrl = import.meta.env.VITE_TRIVIA_API_BASE_URL as string | undefined;

function resolveBaseUrl(): string {
  if (!baseUrl) {
    throw new Error("Missing VITE_TRIVIA_API_BASE_URL environment variable.");
  }

  return baseUrl.replace(/\/$/, "");
}

export async function getTriviaForms(
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<TriviaFormListItem[]> {
  const response = await fetchImpl(`${resolveBaseUrl()}/api/trivia-forms`, {
    method: "GET",
    headers: {
      Authorization: `Bearer ${accessToken}`
    }
  });

  const body = (await response.json().catch(() => ({}))) as
    | { message?: string }
    | TriviaFormListItem[];

  if (!response.ok) {
    const message =
      (body as { message?: string }).message ??
      `Trivia API error. Status=${response.status}`;
    throw new TriviaApiError(message, response.status);
  }

  return body as TriviaFormListItem[];
}

export async function createTriviaGame(
  payload: CreateTriviaGameRequest,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<TriviaGameDetail> {
  const response = await fetchImpl(`${resolveBaseUrl()}/api/trivia-games`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${accessToken}`
    },
    body: JSON.stringify(payload)
  });

  const body = (await response.json().catch(() => ({}))) as
    | { message?: string }
    | TriviaGameDetail;

  if (!response.ok) {
    const message =
      (body as { message?: string }).message ??
      `Trivia API error. Status=${response.status}`;
    throw new TriviaApiError(message, response.status);
  }

  return body as TriviaGameDetail;
}

async function parseJsonResponse<T>(response: Response): Promise<T> {
  const body = (await response.json().catch(() => ({}))) as { message?: string } | T;

  if (!response.ok) {
    const message =
      (body as { message?: string }).message ??
      `Trivia API error. Status=${response.status}`;
    throw new TriviaApiError(message, response.status);
  }

  return body as T;
}

export async function createTriviaForm(
  payload: CreateTriviaFormRequest,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<TriviaFormDetail> {
  const response = await fetchImpl(`${resolveBaseUrl()}/api/trivia-forms`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${accessToken}`
    },
    body: JSON.stringify(payload)
  });

  return parseJsonResponse<TriviaFormDetail>(response);
}

export async function getTriviaParticipants(
  partidaId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<TriviaGameLobby> {
  const response = await fetchImpl(`${resolveBaseUrl()}/api/trivia-games/${partidaId}/participants`, {
    method: "GET",
    headers: { Authorization: `Bearer ${accessToken}` }
  });

  return parseJsonResponse<TriviaGameLobby>(response);
}

export async function getOperatorTriviaGamesForSupervision(
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<TriviaGameListItem[]> {
  const response = await fetchImpl(`${resolveBaseUrl()}/api/trivia-games/operator/supervision`, {
    method: "GET",
    headers: { Authorization: `Bearer ${accessToken}` }
  });

  return parseJsonResponse<TriviaGameListItem[]>(response);
}

export async function getTriviaTeams(
  partidaId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<TriviaTeamLobbyItem[]> {
  const response = await fetchImpl(`${resolveBaseUrl()}/api/trivia-games/${partidaId}/teams`, {
    method: "GET",
    headers: { Authorization: `Bearer ${accessToken}` }
  });

  return parseJsonResponse<TriviaTeamLobbyItem[]>(response);
}

export async function startTriviaGame(
  partidaId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<TriviaGameDetail> {
  const response = await fetchImpl(`${resolveBaseUrl()}/api/trivia-games/${partidaId}/start`, {
    method: "POST",
    headers: { Authorization: `Bearer ${accessToken}` }
  });

  return parseJsonResponse<TriviaGameDetail>(response);
}

export async function getTriviaRanking(
  partidaId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<TriviaRankingEntry[]> {
  const response = await fetchImpl(`${resolveBaseUrl()}/api/trivia-games/${partidaId}/ranking`, {
    method: "GET",
    headers: { Authorization: `Bearer ${accessToken}` }
  });

  return parseJsonResponse<TriviaRankingEntry[]>(response);
}
