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

const baseUrl = import.meta.env.VITE_IDENTITY_API_BASE_URL as string | undefined;

function resolveBaseUrl(): string {
  if (!baseUrl) {
    throw new Error("Missing VITE_IDENTITY_API_BASE_URL environment variable.");
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
