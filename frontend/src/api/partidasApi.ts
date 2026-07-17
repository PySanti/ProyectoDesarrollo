// Cliente del servicio Partidas (configuración) a través del gateway.
// Contrato: contracts/http/partidas-config.md — este archivo lo espeja, no lo redefine.

export type Modalidad = "Individual" | "Equipo";
export type ModoInicioPartida = "Manual" | "Automatico" | "ManualYAutomatico";

export interface CreatePartidaRequest {
  nombrePartida: string;
  modalidad: Modalidad;
  modoInicioPartida: ModoInicioPartida;
  tiempoInicio: string | null;
  minimosParticipacion: number;
  maximosParticipacion: number;
}

export interface CreatePartidaResponse {
  partidaId: string;
}

export interface OpcionPayload {
  texto: string;
  esCorrecta: boolean;
}

export interface PreguntaPayload {
  texto: string;
  opciones: OpcionPayload[];
  puntaje: number;
  tiempoLimiteSegundos: number;
}

export interface AddJuegoTriviaRequest {
  orden: number;
  preguntas: PreguntaPayload[];
}

export interface EtapaPayload {
  orden: number;
  codigoQREsperado: string;
  puntaje: number;
  tiempoLimiteSegundos: number;
}

export interface AddJuegoBdtRequest {
  orden: number;
  areaBusqueda: string;
  etapas: EtapaPayload[];
}

export interface AddJuegoResponse {
  juegoId: string;
}

export interface PartidaSummary {
  partidaId: string;
  nombrePartida: string;
  modalidad: Modalidad;
  modoInicioPartida: ModoInicioPartida;
  tiempoInicio: string | null;
  minimosParticipacion: number;
  maximosParticipacion: number;
  estado: string | null;
  cantidadJuegos: number;
  // Instante de creación (UTC). El backend entrega la lista ordenada por este campo,
  // descendente: no reordenar en cliente.
  fechaCreacion: string;
}

export interface OpcionDetail {
  opcionId: string;
  texto: string;
  esCorrecta: boolean;
}

export interface PreguntaDetail {
  preguntaId: string;
  texto: string;
  puntajeAsignado: number;
  tiempoLimiteSegundos: number;
  opciones: OpcionDetail[];
}

export interface EtapaDetail {
  etapaBDTId: string;
  orden: number;
  codigoQREsperado: string;
  puntajeAsignado: number;
  tiempoLimiteSegundos: number;
}

export interface JuegoDetail {
  juegoId: string;
  orden: number;
  tipoJuego: "Trivia" | "BusquedaDelTesoro";
  estado: string;
  trivia: { preguntas: PreguntaDetail[] } | null;
  bdt: { areaBusqueda: string; etapas: EtapaDetail[] } | null;
}

export interface PartidaDetail {
  partidaId: string;
  nombrePartida: string;
  modalidad: Modalidad;
  modoInicioPartida: ModoInicioPartida;
  tiempoInicio: string | null;
  minimosParticipacion: number;
  maximosParticipacion: number;
  estado: string | null;
  juegos: JuegoDetail[];
}

export class PartidasApiError extends Error {
  constructor(
    message: string,
    public readonly statusCode: number
  ) {
    super(message);
    this.name = "PartidasApiError";
  }
}

function resolveBaseUrl(): string {
  const value = import.meta.env.VITE_GATEWAY_BASE_URL as string | undefined;
  if (!value) {
    throw new Error("Missing VITE_GATEWAY_BASE_URL environment variable.");
  }
  return value.replace(/\/$/, "");
}

function buildAuthHeaders(accessToken: string): HeadersInit {
  return {
    "Content-Type": "application/json",
    Authorization: `Bearer ${accessToken}`
  };
}

async function request<T>(
  path: string,
  init: RequestInit,
  fetchImpl: typeof fetch
): Promise<T> {
  const response = await fetchImpl(`${resolveBaseUrl()}${path}`, init);
  const body = (await response.json().catch(() => ({}))) as T & { message?: string };
  if (!response.ok) {
    const message = body.message ?? `Partidas API error. Status=${response.status}`;
    throw new PartidasApiError(message, response.status);
  }
  return body;
}

export async function createPartida(
  payload: CreatePartidaRequest,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<CreatePartidaResponse> {
  return request<CreatePartidaResponse>(
    "/partidas",
    { method: "POST", headers: buildAuthHeaders(accessToken), body: JSON.stringify(payload) },
    fetchImpl
  );
}

export async function addJuegoTrivia(
  partidaId: string,
  payload: AddJuegoTriviaRequest,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<AddJuegoResponse> {
  return request<AddJuegoResponse>(
    `/partidas/${partidaId}/juegos/trivia`,
    { method: "POST", headers: buildAuthHeaders(accessToken), body: JSON.stringify(payload) },
    fetchImpl
  );
}

export async function addJuegoBdt(
  partidaId: string,
  payload: AddJuegoBdtRequest,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<AddJuegoResponse> {
  return request<AddJuegoResponse>(
    `/partidas/${partidaId}/juegos/bdt`,
    { method: "POST", headers: buildAuthHeaders(accessToken), body: JSON.stringify(payload) },
    fetchImpl
  );
}

export async function getPartida(
  partidaId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<PartidaDetail> {
  return request<PartidaDetail>(
    `/partidas/${partidaId}`,
    { method: "GET", headers: buildAuthHeaders(accessToken) },
    fetchImpl
  );
}

export async function getPartidas(
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<PartidaSummary[]> {
  return request<PartidaSummary[]>(
    "/partidas",
    { method: "GET", headers: buildAuthHeaders(accessToken) },
    fetchImpl
  );
}
