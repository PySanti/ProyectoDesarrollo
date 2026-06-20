import Keycloak from "keycloak-js";

export interface AuthUser {
  username: string;
  roles: string[];
  token: string;
}

export interface AuthProvider {
  /** Resolves to the authenticated user, or null when there is no session yet. */
  init(): Promise<AuthUser | null>;
  /** Redirects to Keycloak to start the login flow. */
  login(): Promise<void>;
  logout(): Promise<void>;
}

// Only these app roles are meaningful to UMBRAL. Keycloak technical roles
// (default-roles-*, offline_access, uma_authorization, manage-account, ...)
// are intentionally dropped so they never surface in the UI.
const knownRoles = new Map<string, string>([
  ["administrador", "Administrador"],
  ["operador", "Operador"],
  ["participante", "Participante"]
]);

const keycloakUrl = import.meta.env.VITE_KEYCLOAK_URL as string | undefined;
const keycloakRealm = import.meta.env.VITE_KEYCLOAK_REALM as string | undefined;
const keycloakClientId = import.meta.env.VITE_KEYCLOAK_CLIENT_ID as string | undefined;

class KeycloakAuthProvider implements AuthProvider {
  private readonly keycloak: Keycloak;
  private initPromise: Promise<AuthUser | null> | null = null;

  constructor() {
    if (!keycloakUrl || !keycloakRealm || !keycloakClientId) {
      throw new Error(
        "Missing Keycloak env vars. Check VITE_KEYCLOAK_URL, VITE_KEYCLOAK_REALM, VITE_KEYCLOAK_CLIENT_ID."
      );
    }

    this.keycloak = new Keycloak({
      url: keycloakUrl,
      realm: keycloakRealm,
      clientId: keycloakClientId
    });
  }

  async init(): Promise<AuthUser | null> {
    if (this.initPromise) {
      return this.initPromise;
    }

    // No forced redirect: init only processes an existing session or a login
    // callback present in the URL. When there is no session we resolve to null
    // and the app shows its own branded login screen instead of bouncing to
    // the generic Keycloak page.
    this.initPromise = this.keycloak
      .init({
        checkLoginIframe: false,
        pkceMethod: "S256"
      })
      .then((authenticated) => {
        if (!authenticated || !this.keycloak.token) {
          return null;
        }

        const parsed = this.keycloak.tokenParsed;
        const roles = extractRoles(parsed);

        const username =
          (parsed?.preferred_username as string | undefined) ??
          (parsed?.name as string | undefined) ??
          "unknown";

        return {
          username,
          roles,
          token: this.keycloak.token as string
        };
      })
      .catch((error) => {
        this.initPromise = null;
        throw error;
      });

    return this.initPromise;
  }

  async login(): Promise<void> {
    // Trailing slash matters: Keycloak's registered redirect pattern is
    // `http://localhost:5173/*`, which does NOT match the bare origin
    // (`http://localhost:5173`). Sending origin + "/" keeps it inside the
    // wildcard so Keycloak doesn't reject with an error/"page not found".
    await this.keycloak.login({ redirectUri: `${window.location.origin}/` });
  }

  async logout(): Promise<void> {
    await this.keycloak.logout({ redirectUri: `${window.location.origin}/` });
  }
}

function extractRoles(parsed: Keycloak.KeycloakTokenParsed | undefined): string[] {
  const realmRoles = Array.isArray(parsed?.realm_access?.roles) ? parsed.realm_access.roles : [];

  const resourceAccess = parsed?.resource_access ?? {};
  const clientRoles = Object.values(resourceAccess).flatMap((client) =>
    Array.isArray(client?.roles) ? client.roles : []
  );

  // Keep only recognized UMBRAL app roles; drop Keycloak technical roles.
  return Array.from(
    new Set(
      [...realmRoles, ...clientRoles]
        .map((role) => knownRoles.get(role.trim().toLowerCase()))
        .filter((role): role is string => Boolean(role))
    )
  );
}

export const authProvider: AuthProvider = new KeycloakAuthProvider();
