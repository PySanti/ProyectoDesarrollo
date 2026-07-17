// Cliente HTTP del servicio Operaciones de Sesion (runtime de la partida), via gateway.
export type EstadoSesion = "Lobby" | "Iniciada" | "Cancelada" | "Terminada";
export type Modalidad = "Individual" | "Equipo";

export interface LobbyEquipo {
  equipoId: string;
  convocados: number;
  aceptados: number;
}

export interface SolicitudIndividual {
  inscripcionId: string;
  participanteId: string;
  fechaInscripcion: string;
}

export interface SolicitudEquipo {
  inscripcionId: string;
  equipoId: string;
  miembros: number;
  fechaInscripcion: string;
}

export interface LobbyDto {
  partidaId: string;
  sesionPartidaId: string;
  estado: EstadoSesion;
  modalidad: Modalidad;
  minimosParticipacion: number;
  maximosParticipacion: number;
  inscritosActivos: number;
  participantes: string[];
  equipos: LobbyEquipo[];
  solicitudesPendientesIndividual: SolicitudIndividual[];
  solicitudesPendientesEquipo: SolicitudEquipo[];
}

export interface InicioPartidaResponse {
  partidaId: string;
  estado: EstadoSesion;
  juegoActivadoId?: string;
  juegoActivadoOrden?: number;
}

export interface CancelacionPartidaResponse {
  partidaId: string;
  estado: EstadoSesion;
}

export interface JuegoEstado {
  juegoId: string;
  orden: number;
  tipoJuego: "Trivia" | "BusquedaDelTesoro";
  estado: string;
}

export interface EstadoSesionDto {
  partidaId: string;
  sesionPartidaId: string;
  estado: EstadoSesion;
  modalidad: Modalidad;
  juegos: JuegoEstado[];
  juegoActualOrden?: number;
}

export class OperacionesApiError extends Error {
  constructor(
    message: string,
    public readonly statusCode: number
  ) {
    super(message);
    this.name = "OperacionesApiError";
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

async function request<T>(path: string, init: RequestInit, fetchImpl: typeof fetch): Promise<T> {
  const response = await fetchImpl(`${resolveBaseUrl()}${path}`, init);
  const body = (await response.json().catch(() => ({}))) as T & { message?: string };
  if (!response.ok) {
    const message = body.message ?? `Operaciones API error. Status=${response.status}`;
    throw new OperacionesApiError(message, response.status);
  }
  return body;
}

export async function publicarPartida(
  partidaId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<LobbyDto> {
  return request<LobbyDto>(
    `/operaciones-sesion/partidas/${partidaId}/publicacion`,
    { method: "POST", headers: buildAuthHeaders(accessToken) },
    fetchImpl
  );
}

export async function getLobby(
  partidaId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<LobbyDto> {
  return request<LobbyDto>(
    `/operaciones-sesion/partidas/${partidaId}/lobby`,
    { method: "GET", headers: buildAuthHeaders(accessToken) },
    fetchImpl
  );
}

export async function aceptarInscripcion(
  partidaId: string,
  inscripcionId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<LobbyDto> {
  return request<LobbyDto>(
    `/operaciones-sesion/partidas/${partidaId}/inscripciones/${inscripcionId}/aceptacion`,
    { method: "POST", headers: buildAuthHeaders(accessToken) },
    fetchImpl
  );
}

export async function rechazarInscripcion(
  partidaId: string,
  inscripcionId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<LobbyDto> {
  return request<LobbyDto>(
    `/operaciones-sesion/partidas/${partidaId}/inscripciones/${inscripcionId}/rechazo`,
    { method: "POST", headers: buildAuthHeaders(accessToken) },
    fetchImpl
  );
}

export async function cancelarPartida(
  partidaId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<CancelacionPartidaResponse> {
  return request<CancelacionPartidaResponse>(
    `/operaciones-sesion/partidas/${partidaId}/cancelacion`,
    { method: "POST", headers: buildAuthHeaders(accessToken) },
    fetchImpl
  );
}

export async function iniciarPartida(
  partidaId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<InicioPartidaResponse> {
  return request<InicioPartidaResponse>(
    `/operaciones-sesion/partidas/${partidaId}/inicio`,
    { method: "POST", headers: buildAuthHeaders(accessToken) },
    fetchImpl
  );
}

export async function getEstadoSesion(
  partidaId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<EstadoSesionDto> {
  return request<EstadoSesionDto>(
    `/operaciones-sesion/partidas/${partidaId}/estado`,
    { method: "GET", headers: buildAuthHeaders(accessToken) },
    fetchImpl
  );
}

export interface OpcionPregunta {
  opcionId: string;
  texto: string;
}

export interface PreguntaActualDto {
  partidaId: string;
  juegoId: string;
  preguntaId: string;
  orden: number;
  texto: string;
  tiempoLimiteSegundos: number;
  fechaActivacion: string;
  opciones: OpcionPregunta[];
}

export interface AvancePreguntaResponse {
  partidaId: string;
  preguntaCerradaOrden: number;
  preguntaActivadaOrden?: number | null;
  sinMasPreguntas: boolean;
}

export interface AvanceJuegoResponse {
  partidaId: string;
  estado: string;
  juegoFinalizadoOrden?: number | null;
  juegoActivadoOrden?: number | null;
  terminada: boolean;
}

export async function getPreguntaActual(
  partidaId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<PreguntaActualDto> {
  return request<PreguntaActualDto>(
    `/operaciones-sesion/partidas/${partidaId}/pregunta-actual`,
    { method: "GET", headers: buildAuthHeaders(accessToken) },
    fetchImpl
  );
}

export async function avanzarPregunta(
  partidaId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<AvancePreguntaResponse> {
  return request<AvancePreguntaResponse>(
    `/operaciones-sesion/partidas/${partidaId}/pregunta-actual/avance`,
    { method: "POST", headers: buildAuthHeaders(accessToken) },
    fetchImpl
  );
}

export async function finalizarJuegoActual(
  partidaId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<AvanceJuegoResponse> {
  return request<AvanceJuegoResponse>(
    `/operaciones-sesion/partidas/${partidaId}/juego-actual/finalizacion`,
    { method: "POST", headers: buildAuthHeaders(accessToken) },
    fetchImpl
  );
}

export interface EtapaActualDto {
  partidaId: string;
  juegoId: string;
  etapaId: string;
  orden: number;
  areaBusqueda: string;
  tiempoLimiteSegundos: number;
  fechaActivacion: string;
}

export interface AvanceEtapaResponse {
  partidaId: string;
  etapaCerradaOrden: number;
  etapaActivadaOrden?: number | null;
  sinMasEtapas: boolean;
}

export interface EnviarPistaRequest {
  texto: string;
  participanteDestinoId?: string;
  equipoDestinoId?: string;
}

export interface PistaEnviadaResponse {
  partidaId: string;
  juegoId: string;
  participanteDestinoId?: string | null;
  equipoDestinoId?: string | null;
  timestampUtc: string;
}

export async function getEtapaActual(
  partidaId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<EtapaActualDto> {
  return request<EtapaActualDto>(
    `/operaciones-sesion/partidas/${partidaId}/etapa-actual`,
    { method: "GET", headers: buildAuthHeaders(accessToken) },
    fetchImpl
  );
}

export async function avanzarEtapa(
  partidaId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<AvanceEtapaResponse> {
  return request<AvanceEtapaResponse>(
    `/operaciones-sesion/partidas/${partidaId}/etapa-actual/avance`,
    { method: "POST", headers: buildAuthHeaders(accessToken) },
    fetchImpl
  );
}

export type ResultadoTesoro = "Valido" | "Invalido" | "NoLegible" | "NoCorrespondeEtapaActiva";

export interface IntentoTesoroDto {
  participanteId: string;
  equipoId?: string;
  resultado: ResultadoTesoro;
  instante: string;
}

export interface EtapaEnviosDto {
  etapaId: string;
  orden: number;
  intentos: IntentoTesoroDto[];
}

export interface EnviosTesoroDto {
  partidaId: string;
  juegoId: string;
  etapas: EtapaEnviosDto[];
}

export async function getEnviosTesoro(
  partidaId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<EnviosTesoroDto> {
  return request<EnviosTesoroDto>(
    `/operaciones-sesion/partidas/${partidaId}/juego-actual/envios-tesoro`,
    { method: "GET", headers: buildAuthHeaders(accessToken) },
    fetchImpl
  );
}

export async function enviarPista(
  partidaId: string,
  body: EnviarPistaRequest,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<PistaEnviadaResponse> {
  return request<PistaEnviadaResponse>(
    `/operaciones-sesion/partidas/${partidaId}/pistas`,
    { method: "POST", headers: buildAuthHeaders(accessToken), body: JSON.stringify(body) },
    fetchImpl
  );
}
