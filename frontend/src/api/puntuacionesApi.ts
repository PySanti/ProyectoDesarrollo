// Cliente HTTP del servicio Puntuaciones (rankings, lectura), via gateway.
export interface RankingEntrada {
  posicion: number;
  competidorId: string;
  tipoCompetidor: "Participante" | "Equipo";
  puntos: number;
  tiempoAcumuladoMs: number;
  unidadesGanadas: number;
}

export interface RankingJuegoDto {
  juegoId: string;
  tipoJuego: string;
  generadoEn: string;
  entradas: RankingEntrada[];
}

export class PuntuacionesApiError extends Error {
  constructor(
    message: string,
    public readonly statusCode: number
  ) {
    super(message);
    this.name = "PuntuacionesApiError";
  }
}

function resolveBaseUrl(): string {
  const value = import.meta.env.VITE_GATEWAY_BASE_URL as string | undefined;
  if (!value) {
    throw new Error("Missing VITE_GATEWAY_BASE_URL environment variable.");
  }
  return value.replace(/\/$/, "");
}

export async function getRankingJuego(
  partidaId: string,
  juegoId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<RankingJuegoDto> {
  const response = await fetchImpl(
    `${resolveBaseUrl()}/puntuaciones/partidas/${partidaId}/juegos/${juegoId}/ranking`,
    { method: "GET", headers: { Authorization: `Bearer ${accessToken}` } }
  );
  const body = (await response.json().catch(() => ({}))) as RankingJuegoDto & { message?: string };
  if (!response.ok) {
    const message = body.message ?? `Puntuaciones API error. Status=${response.status}`;
    throw new PuntuacionesApiError(message, response.status);
  }
  return body;
}

export interface RankingConsolidadoEntradaDto {
  posicion: number;
  competidorId: string;
  tipoCompetidor: "Participante" | "Equipo";
  juegosGanados: number;
  puntosTotales: number;
  tiempoTotalMs: number;
}

export interface RankingConsolidadoDto {
  partidaId: string;
  generadoEn: string;
  entradas: RankingConsolidadoEntradaDto[];
}

export async function getRankingConsolidado(
  partidaId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<RankingConsolidadoDto> {
  const response = await fetchImpl(
    `${resolveBaseUrl()}/puntuaciones/partidas/${partidaId}/ranking-consolidado`,
    { method: "GET", headers: { Authorization: `Bearer ${accessToken}` } }
  );
  const body = (await response.json().catch(() => ({}))) as RankingConsolidadoDto & {
    message?: string;
  };
  if (!response.ok) {
    const message = body.message ?? `Puntuaciones API error. Status=${response.status}`;
    throw new PuntuacionesApiError(message, response.status);
  }
  return body;
}

export interface EventoHistorialDto {
  occurredAt: string;
  tipoEvento: string;
  juegoId: string | null;
  participanteId: string | null;
  equipoId: string | null;
  detalle: unknown;
  juegoOrden: number | null;
  tipoJuego: string | null;
}

export interface HistorialPartidaDto {
  partidaId: string;
  total: number;
  entradas: EventoHistorialDto[];
}

export interface HistorialQueryOpts {
  limit?: number;
  offset?: number;
  tipo?: string;
}

export async function getHistorialPartida(
  partidaId: string,
  accessToken: string,
  opts: HistorialQueryOpts = {},
  fetchImpl: typeof fetch = fetch
): Promise<HistorialPartidaDto> {
  const params = new URLSearchParams();
  if (opts.limit != null) params.set("limit", String(opts.limit));
  if (opts.offset != null) params.set("offset", String(opts.offset));
  if (opts.tipo) params.set("tipo", opts.tipo);
  const query = params.toString();
  const response = await fetchImpl(
    `${resolveBaseUrl()}/puntuaciones/partidas/${partidaId}/historial${query ? `?${query}` : ""}`,
    { method: "GET", headers: { Authorization: `Bearer ${accessToken}` } }
  );
  const body = (await response.json().catch(() => ({}))) as HistorialPartidaDto & {
    message?: string;
  };
  if (!response.ok) {
    const message = body.message ?? `Puntuaciones API error. Status=${response.status}`;
    throw new PuntuacionesApiError(message, response.status);
  }
  return body;
}

export interface RendimientoPartidaDto {
  partidaId: string;
  fechaFin: string;
  posicion: number;
  gano: boolean;
}

export interface RendimientoEquipoDto {
  equipoId: string;
  partidas: RendimientoPartidaDto[];
}

export async function getRendimientoEquipo(
  equipoId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<RendimientoEquipoDto> {
  const response = await fetchImpl(
    `${resolveBaseUrl()}/puntuaciones/equipos/${equipoId}/rendimiento`,
    { method: "GET", headers: { Authorization: `Bearer ${accessToken}` } }
  );
  const body = (await response.json().catch(() => ({}))) as RendimientoEquipoDto & {
    message?: string;
  };
  if (!response.ok) {
    const message = body.message ?? `Puntuaciones API error. Status=${response.status}`;
    throw new PuntuacionesApiError(message, response.status);
  }
  return body;
}
