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
  createdUtc: string;
  startedAtUtc: string | null;
}

export interface TriviaFormListItem {
  id: string;
  title: string;
  isComplete: boolean;
  questionsCount: number;
  createdAtUtc: string;
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
