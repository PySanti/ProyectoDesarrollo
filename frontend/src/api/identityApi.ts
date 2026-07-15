export interface CreateUserRequest {
  name: string;
  email: string;
  initialRole: "Administrador" | "Operador" | "Participante";
}

export interface CreateUserResponse {
  userId: string;
  keycloakId: string;
  name: string;
  email: string;
  role: "Administrador" | "Operador" | "Participante";
  status: string;
}

export interface IdentityUserSummary {
  userId: string;
  keycloakId: string;
  name: string;
  email: string;
  role: "Administrador" | "Operador" | "Participante";
  status: "Activo" | "Desactivado";
}

export type IdentityUserDetail = IdentityUserSummary;

export interface UpdateUserGeneralDataRequest {
  name: string;
  email: string;
}

export interface DeactivateUserResponse {
  userId: string;
  status: "Desactivado";
}

export class IdentityApiError extends Error {
  constructor(
    message: string,
    public readonly statusCode: number
  ) {
    super(message);
    this.name = "IdentityApiError";
  }
}

const baseUrl = import.meta.env.VITE_GATEWAY_BASE_URL as string | undefined;

function resolveBaseUrl(): string {
  if (!baseUrl) {
    throw new Error("Missing VITE_GATEWAY_BASE_URL environment variable.");
  }

  return baseUrl.replace(/\/$/, "");
}

export async function createIdentityUser(
  payload: CreateUserRequest,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<CreateUserResponse> {
  const response = await fetchImpl(`${resolveBaseUrl()}/identity/users`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${accessToken}`
    },
    body: JSON.stringify(payload)
  });

  const body = (await response.json().catch(() => ({}))) as
    | { message?: string }
    | CreateUserResponse;

  if (!response.ok) {
    const message =
      (body as { message?: string }).message ??
      `Identity API error. Status=${response.status}`;
    throw new IdentityApiError(message, response.status);
  }

  return body as CreateUserResponse;
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

export async function getIdentityUsers(
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<IdentityUserSummary[]> {
  const response = await fetchImpl(`${resolveBaseUrl()}/identity/users`, {
    method: "GET",
    headers: buildAuthHeaders(accessToken)
  });

  const body = await parseJsonBody<IdentityUserSummary[]>(response);
  throwIfNotOk(response, body as { message?: string });
  return body as IdentityUserSummary[];
}

export async function getIdentityUserById(
  userId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<IdentityUserDetail> {
  const response = await fetchImpl(`${resolveBaseUrl()}/identity/users/${userId}`, {
    method: "GET",
    headers: buildAuthHeaders(accessToken)
  });

  const body = await parseJsonBody<IdentityUserDetail>(response);
  throwIfNotOk(response, body as { message?: string });
  return body as IdentityUserDetail;
}

export async function updateIdentityUserGeneralData(
  userId: string,
  payload: UpdateUserGeneralDataRequest,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<IdentityUserDetail> {
  const response = await fetchImpl(`${resolveBaseUrl()}/identity/users/${userId}`, {
    method: "PATCH",
    headers: buildAuthHeaders(accessToken),
    body: JSON.stringify(payload)
  });

  const body = await parseJsonBody<IdentityUserDetail>(response);
  throwIfNotOk(response, body as { message?: string });
  return body as IdentityUserDetail;
}

export async function deactivateIdentityUser(
  userId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<DeactivateUserResponse> {
  const response = await fetchImpl(
    `${resolveBaseUrl()}/identity/users/${userId}/deactivation`,
    {
      method: "PATCH",
      headers: buildAuthHeaders(accessToken),
      body: JSON.stringify({})
    }
  );

  const body = await parseJsonBody<DeactivateUserResponse>(response);
  throwIfNotOk(response, body as { message?: string });
  return body as DeactivateUserResponse;
}

export type PermisoFuncional = "GestionarPartidas" | "GestionarEquipos" | "ParticiparEnPartidas";

export interface RolePermissions {
  rol: "Administrador" | "Operador" | "Participante";
  permisos: PermisoFuncional[];
  privilegiosGobernanza: boolean;
}

export interface GovernanceRolesResponse {
  roles: RolePermissions[];
}

export interface ChangeUserRoleResponse {
  usuarioId: string;
  rol: "Administrador" | "Operador" | "Participante";
}

export async function getGovernanceRoles(
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<GovernanceRolesResponse> {
  const response = await fetchImpl(`${resolveBaseUrl()}/identity/governance/roles`, {
    method: "GET",
    headers: buildAuthHeaders(accessToken)
  });

  const body = await parseJsonBody<GovernanceRolesResponse>(response);
  throwIfNotOk(response, body as { message?: string });
  return body as GovernanceRolesResponse;
}

export async function updateRolePermissions(
  rol: string,
  permisos: PermisoFuncional[],
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<RolePermissions> {
  const response = await fetchImpl(
    `${resolveBaseUrl()}/identity/governance/roles/${rol}/permisos`,
    {
      method: "PUT",
      headers: buildAuthHeaders(accessToken),
      body: JSON.stringify({ permisos })
    }
  );

  const body = await parseJsonBody<RolePermissions>(response);
  throwIfNotOk(response, body as { message?: string });
  return body as RolePermissions;
}

export async function changeUserRole(
  userId: string,
  rol: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<ChangeUserRoleResponse> {
  const response = await fetchImpl(`${resolveBaseUrl()}/identity/users/${userId}/role`, {
    method: "PATCH",
    headers: buildAuthHeaders(accessToken),
    body: JSON.stringify({ rol })
  });

  const body = await parseJsonBody<ChangeUserRoleResponse>(response);
  throwIfNotOk(response, body as { message?: string });
  return body as ChangeUserRoleResponse;
}

export interface EquipoMiembro {
  usuarioId: string;
  nombre: string;
  esLider: boolean;
}

export interface EquipoAdminItem {
  equipoId: string;
  nombreEquipo: string;
  estado: string;
  participantes: EquipoMiembro[];
}

export async function getEquipos(
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<EquipoAdminItem[]> {
  const response = await fetchImpl(`${resolveBaseUrl()}/identity/teams`, {
    method: "GET",
    headers: buildAuthHeaders(accessToken)
  });

  const body = await parseJsonBody<EquipoAdminItem[]>(response);
  throwIfNotOk(response, body as { message?: string });
  return body as EquipoAdminItem[];
}
