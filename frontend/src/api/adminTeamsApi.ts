import { IdentityApiError } from "./identityApi";

export { IdentityApiError };

export interface AdminTeamMember {
  usuarioId: string;
  esLider: boolean;
}

export interface AdminTeam {
  equipoId: string;
  nombreEquipo: string;
  estado: "Activo" | "Desactivado" | "Eliminado";
  liderUserId?: string;
  integrantes: AdminTeamMember[];
}

export interface CreateAdminTeamRequest {
  nombreEquipo: string;
  /**
   * Local `Usuario.UsuarioId` of the initial leader — the same id the admin
   * user directory (`identityApi` user list) exposes. Sent as-is, no
   * translation performed here; callers (e.g. the governance page) must pass
   * the directory's `userId`.
   */
  liderUserId: string;
}

export interface RenameAdminTeamRequest {
  nombreEquipo: string;
}

export interface ReassignAdminTeamLeaderRequest {
  nuevoLiderUserId: string;
}

export interface SetAdminTeamEstadoRequest {
  estado: "Activo" | "Desactivado";
}

const baseUrl = import.meta.env.VITE_IDENTITY_API_BASE_URL as string | undefined;

function resolveBaseUrl(): string {
  if (!baseUrl) {
    throw new Error("Missing VITE_IDENTITY_API_BASE_URL environment variable.");
  }

  return baseUrl.replace(/\/$/, "");
}

function buildAuthHeaders(accessToken: string): HeadersInit {
  return {
    "Content-Type": "application/json",
    Authorization: `Bearer ${accessToken}`
  };
}

async function parseJsonBody<T>(response: Response): Promise<T | { message?: string }> {
  return (await response.json().catch(() => ({}))) as T | { message?: string };
}

function throwIfNotOk(response: Response, body: { message?: string }): void {
  if (!response.ok) {
    const message = body.message ?? `Identity API error. Status=${response.status}`;
    throw new IdentityApiError(message, response.status);
  }
}

export async function listAdminTeams(
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<AdminTeam[]> {
  const response = await fetchImpl(`${resolveBaseUrl()}/identity/admin/teams`, {
    method: "GET",
    headers: buildAuthHeaders(accessToken)
  });

  const body = await parseJsonBody<AdminTeam[]>(response);
  throwIfNotOk(response, body as { message?: string });
  return body as AdminTeam[];
}

export async function getAdminTeam(
  id: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<AdminTeam> {
  const response = await fetchImpl(`${resolveBaseUrl()}/identity/admin/teams/${id}`, {
    method: "GET",
    headers: buildAuthHeaders(accessToken)
  });

  const body = await parseJsonBody<AdminTeam>(response);
  throwIfNotOk(response, body as { message?: string });
  return body as AdminTeam;
}

export async function createAdminTeam(
  payload: CreateAdminTeamRequest,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<AdminTeam> {
  const response = await fetchImpl(`${resolveBaseUrl()}/identity/admin/teams`, {
    method: "POST",
    headers: buildAuthHeaders(accessToken),
    body: JSON.stringify(payload)
  });

  const body = await parseJsonBody<AdminTeam>(response);
  throwIfNotOk(response, body as { message?: string });
  return body as AdminTeam;
}

export async function renameAdminTeam(
  id: string,
  payload: RenameAdminTeamRequest,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<AdminTeam> {
  const response = await fetchImpl(`${resolveBaseUrl()}/identity/admin/teams/${id}/name`, {
    method: "PATCH",
    headers: buildAuthHeaders(accessToken),
    body: JSON.stringify(payload)
  });

  const body = await parseJsonBody<AdminTeam>(response);
  throwIfNotOk(response, body as { message?: string });
  return body as AdminTeam;
}

export async function reassignAdminTeamLeader(
  id: string,
  payload: ReassignAdminTeamLeaderRequest,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<AdminTeam> {
  const response = await fetchImpl(`${resolveBaseUrl()}/identity/admin/teams/${id}/leadership`, {
    method: "PATCH",
    headers: buildAuthHeaders(accessToken),
    body: JSON.stringify(payload)
  });

  const body = await parseJsonBody<AdminTeam>(response);
  throwIfNotOk(response, body as { message?: string });
  return body as AdminTeam;
}

export async function setAdminTeamEstado(
  id: string,
  payload: SetAdminTeamEstadoRequest,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<AdminTeam> {
  const response = await fetchImpl(`${resolveBaseUrl()}/identity/admin/teams/${id}/estado`, {
    method: "PATCH",
    headers: buildAuthHeaders(accessToken),
    body: JSON.stringify(payload)
  });

  const body = await parseJsonBody<AdminTeam>(response);
  throwIfNotOk(response, body as { message?: string });
  return body as AdminTeam;
}

export async function deleteAdminTeam(
  id: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<void> {
  const response = await fetchImpl(`${resolveBaseUrl()}/identity/admin/teams/${id}`, {
    method: "DELETE",
    headers: buildAuthHeaders(accessToken)
  });

  if (!response.ok) {
    const body = await parseJsonBody<{ message?: string }>(response);
    throwIfNotOk(response, body as { message?: string });
  }
}
