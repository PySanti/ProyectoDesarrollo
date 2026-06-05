import Keycloak from "keycloak-js";

export interface AuthUser {
  username: string;
  roles: string[];
  token: string;
}

export interface AuthProvider {
  init(): Promise<AuthUser>;
  logout(): Promise<void>;
}

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
  private initPromise: Promise<AuthUser> | null = null;

  constructor() {
    if (!keycloakUrl || !keycloakRealm || !keycloakClientId) {
      throw new Error("Missing Keycloak env vars. Check VITE_KEYCLOAK_URL, VITE_KEYCLOAK_REALM, VITE_KEYCLOAK_CLIENT_ID.");
    }

    this.keycloak = new Keycloak({
      url: keycloakUrl,
      realm: keycloakRealm,
      clientId: keycloakClientId
    });
  }

  async init(): Promise<AuthUser> {
    if (this.initPromise) {
      return this.initPromise;
    }

    this.initPromise = this.keycloak
      .init({
        onLoad: "login-required",
        checkLoginIframe: false,
        pkceMethod: "S256"
      })
      .then((authenticated) => {
        if (!authenticated || !this.keycloak.token) {
          throw new Error("Unable to authenticate with Keycloak.");
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

  async logout(): Promise<void> {
    await this.keycloak.logout({ redirectUri: window.location.origin });
  }
}

function extractRoles(parsed: Keycloak.KeycloakTokenParsed | undefined): string[] {
  const realmRoles = Array.isArray(parsed?.realm_access?.roles)
    ? parsed.realm_access.roles
    : [];

  const resourceAccess = parsed?.resource_access ?? {};
  const clientRoles = Object.values(resourceAccess).flatMap((client) =>
    Array.isArray(client?.roles) ? client.roles : []
  );

  return Array.from(new Set([...realmRoles, ...clientRoles].map(normalizeRole).filter(Boolean)));
}

function normalizeRole(role: string): string {
  return knownRoles.get(role.trim().toLowerCase()) ?? role.trim();
}

export const authProvider: AuthProvider = new KeycloakAuthProvider();
