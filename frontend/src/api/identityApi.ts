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
  const response = await fetchImpl(`${resolveBaseUrl()}/api/identity/users`, {
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
