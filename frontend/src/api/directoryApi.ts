// Directorio de nombres (Identity). Solo el fetch: la caché y el fallback viven en
// features/shared/useNombres.ts.
import { IdentityApiError } from "./identityApi";

export { IdentityApiError };

export interface ResolverNombresPayload {
  participanteIds: string[];
  equipoIds: string[];
}

export interface NombresResponse {
  participantes: { participanteId: string; nombre: string }[];
  equipos: { equipoId: string; nombreEquipo: string }[];
}

const baseUrl = import.meta.env.VITE_GATEWAY_BASE_URL as string | undefined;

function resolveBaseUrl(): string {
  if (!baseUrl) {
    throw new Error("Missing VITE_GATEWAY_BASE_URL environment variable.");
  }

  return baseUrl.replace(/\/$/, "");
}

export async function resolverNombres(
  payload: ResolverNombresPayload,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<NombresResponse> {
  const response = await fetchImpl(`${resolveBaseUrl()}/identity/directory/names`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${accessToken}`
    },
    body: JSON.stringify(payload)
  });

  const body = (await response.json().catch(() => ({}))) as NombresResponse & { message?: string };
  if (!response.ok) {
    throw new IdentityApiError(body.message ?? `Identity API error. Status=${response.status}`, response.status);
  }

  return { participantes: body.participantes ?? [], equipos: body.equipos ?? [] };
}
