export type BdtModalidad = "Individual" | "Equipo";
export type BdtModoInicio = "Manual" | "Automatico" | "ManualYAutomatico";

export interface CreateBdtStageRequest {
  orden: number;
  codigoQrEsperado: string;
  tiempoLimiteSegundos: number;
}

export interface CreateBdtGameRequest {
  nombre: string;
  areaBusqueda: string;
  modalidad: BdtModalidad;
  minimoParticipantes: number;
  maximoParticipantes: number | null;
  maximoEquipos: number | null;
  minimoJugadoresPorEquipo: number | null;
  modoInicio: BdtModoInicio;
  etapas: CreateBdtStageRequest[];
}

export interface CreateBdtGameResponse {
  partidaId: string;
  nombre: string;
  modalidad: BdtModalidad;
  estado: "Lobby";
  areaBusqueda: string;
  modoInicio: BdtModoInicio;
  cantidadEtapas: number;
}

export interface PublishedBdtGameItem {
  partidaId: string;
  nombre: string;
  modalidad: BdtModalidad;
  estado: "Lobby";
  areaBusqueda: string;
  cantidadEtapas: number;
}

export interface ActiveBdtStageResponse {
  etapaId: string;
  orden: number;
  tiempoLimiteSegundos: number;
  iniciadaEnUtc: string;
  cierraEnUtc: string;
}

export interface StartBdtGameResponse {
  partidaId: string;
  nombre: string;
  estado: "Iniciada";
  modalidad: BdtModalidad;
  etapaActiva: ActiveBdtStageResponse;
  mensaje: string;
}

export interface DecodeExpectedQrResponse {
  estadoProcesamiento: "Decodificado" | "NoLegible";
  qrDecodificado: string | null;
  mensaje: string;
}

export class BdtApiError extends Error {
  constructor(
    message: string,
    public readonly statusCode: number
  ) {
    super(message);
    this.name = "BdtApiError";
  }
}

const baseUrl = import.meta.env.VITE_BDT_API_BASE_URL as string | undefined;

function resolveBaseUrl(): string {
  if (!baseUrl) {
    throw new Error("Missing VITE_BDT_API_BASE_URL environment variable.");
  }

  return baseUrl.replace(/\/$/, "");
}

export async function createBdtGame(
  payload: CreateBdtGameRequest,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<CreateBdtGameResponse> {
  const response = await fetchImpl(`${resolveBaseUrl()}/api/bdt/games`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${accessToken}`
    },
    body: JSON.stringify(payload)
  });

  const body = (await response.json().catch(() => ({}))) as
    | { message?: string }
    | CreateBdtGameResponse;

  if (!response.ok) {
    const message =
      (body as { message?: string }).message ??
      `BDT API error. Status=${response.status}`;
    throw new BdtApiError(message, response.status);
  }

  return body as CreateBdtGameResponse;
}

export async function decodeBdtExpectedQrImage(
  image: File,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<DecodeExpectedQrResponse> {
  const formData = new FormData();
  formData.append("image", image);

  const response = await fetchImpl(`${resolveBaseUrl()}/api/bdt/stages/expected-qr/decode`, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${accessToken}`
    },
    body: formData
  });

  const body = (await response.json().catch(() => ({}))) as
    | { message?: string }
    | DecodeExpectedQrResponse;

  if (!response.ok) {
    const message =
      (body as { message?: string }).message ??
      `BDT API error. Status=${response.status}`;
    throw new BdtApiError(message, response.status);
  }

  return body as DecodeExpectedQrResponse;
}

export async function getOperatorPublishedBdtGames(
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<PublishedBdtGameItem[]> {
  const response = await fetchImpl(`${resolveBaseUrl()}/api/bdt/operator/games/published`, {
    method: "GET",
    headers: {
      Authorization: `Bearer ${accessToken}`
    }
  });

  const body = (await response.json().catch(() => ({}))) as
    | { message?: string }
    | PublishedBdtGameItem[];

  if (!response.ok) {
    const message =
      (body as { message?: string }).message ??
      `BDT API error. Status=${response.status}`;
    throw new BdtApiError(message, response.status);
  }

  return body as PublishedBdtGameItem[];
}

export async function startBdtGame(
  partidaId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<StartBdtGameResponse> {
  const response = await fetchImpl(`${resolveBaseUrl()}/api/bdt/games/${partidaId}/start`, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${accessToken}`
    }
  });

  const body = (await response.json().catch(() => ({}))) as
    | { message?: string }
    | StartBdtGameResponse;

  if (!response.ok) {
    const message =
      (body as { message?: string }).message ??
      `BDT API error. Status=${response.status}`;
    throw new BdtApiError(message, response.status);
  }

  return body as StartBdtGameResponse;
}
