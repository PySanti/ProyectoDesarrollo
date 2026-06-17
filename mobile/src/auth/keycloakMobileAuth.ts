import * as AuthSession from "expo-auth-session";
import * as SecureStore from "expo-secure-store";
import * as WebBrowser from "expo-web-browser";
import { mobileEnv } from "../config/env";
import { AuthSessionState } from "./authTypes";
import { buildAuthUser, isJwtExpired } from "./tokenClaims.js";

WebBrowser.maybeCompleteAuthSession();

/**
 * Error de autenticación con el mensaje YA en español, listo para mostrarse al usuario.
 * Toda falla de login (cancelación, fallo del navegador, error de Keycloak/expo-auth-session,
 * token inválido) se normaliza a esta clase para que nunca se filtre un texto en inglés de la
 * librería o del servidor a la interfaz. `cancelled=true` marca la cancelación voluntaria
 * (el usuario cerró el navegador): la pantalla de login no muestra ningún error en ese caso.
 */
export class AuthError extends Error {
  readonly cancelled: boolean;

  constructor(message: string, options?: { cancelled?: boolean }) {
    super(message);
    this.name = "AuthError";
    this.cancelled = options?.cancelled ?? false;
  }
}

const storageKey = "umbral.auth.session";
// El refresh token se guarda aparte (no en AuthSessionState) solo para poder
// cerrar la sesion SSO de Keycloak por backchannel al hacer logout.
const refreshKey = "umbral.auth.refresh";

const discovery = {
  authorizationEndpoint: `${mobileEnv.keycloakUrl}/realms/${mobileEnv.keycloakRealm}/protocol/openid-connect/auth`,
  tokenEndpoint: `${mobileEnv.keycloakUrl}/realms/${mobileEnv.keycloakRealm}/protocol/openid-connect/token`,
  revocationEndpoint: `${mobileEnv.keycloakUrl}/realms/${mobileEnv.keycloakRealm}/protocol/openid-connect/revoke`,
  endSessionEndpoint: `${mobileEnv.keycloakUrl}/realms/${mobileEnv.keycloakRealm}/protocol/openid-connect/logout`,
};

export async function loginWithKeycloakAsync(): Promise<AuthSessionState> {
  const redirectUri =
    mobileEnv.authRedirectUri ||
    AuthSession.makeRedirectUri({
      scheme: mobileEnv.redirectScheme,
      path: "auth",
    });

  const request = new AuthSession.AuthRequest({
    clientId: mobileEnv.keycloakClientId,
    redirectUri,
    responseType: AuthSession.ResponseType.Code,
    usePKCE: true,
    scopes: ["openid", "profile", "email"],
    // Fuerza la página de login de Keycloak en español (param OIDC estándar `ui_locales`).
    // Junto con `defaultLocale=es` del realm, evita que el idioma del dispositivo/navegador
    // muestre "Sign in..." / "Invalid..." en inglés.
    extraParams: { ui_locales: "es" },
  });

  await request.makeAuthUrlAsync(discovery);
  // preferEphemeralSession evita que el navegador comparta/persista la cookie SSO
  // de Keycloak entre logins. Junto con el cierre de sesion por backchannel en
  // logoutAsync, garantiza que un nuevo "iniciar sesion" muestre el formulario
  // limpio (sin reusar la cuenta anterior y sin panel de reautenticacion).
  let authResult: AuthSession.AuthSessionResult;
  try {
    authResult = await request.promptAsync(discovery, { preferEphemeralSession: true });
  } catch {
    throw new AuthError("No se pudo abrir el inicio de sesión. Intenta de nuevo.");
  }

  // El usuario cerró el navegador sin autenticarse: no es un error que deba alarmarlo.
  if (authResult.type === "cancel" || authResult.type === "dismiss") {
    throw new AuthError("Inicio de sesión cancelado.", { cancelled: true });
  }

  if (authResult.type !== "success" || !authResult.params.code) {
    throw new AuthError("No se pudo completar el inicio de sesión. Intenta de nuevo.");
  }

  let tokenResult: AuthSession.TokenResponse;
  try {
    tokenResult = await AuthSession.exchangeCodeAsync(
      {
        code: authResult.params.code,
        clientId: mobileEnv.keycloakClientId,
        redirectUri,
        extraParams: {
          code_verifier: request.codeVerifier || "",
        },
      },
      discovery,
    );
  } catch {
    // Errores del endpoint de token / red de Keycloak (en inglés) se normalizan a español.
    throw new AuthError("No se pudo validar tu sesión con el servidor. Intenta de nuevo.");
  }

  if (!tokenResult.accessToken) {
    throw new AuthError("El servidor de autenticación no devolvió un token de acceso.");
  }

  let user: AuthSessionState["user"];
  try {
    user = buildAuthUser(tokenResult.accessToken);
  } catch {
    throw new AuthError("El token recibido no es válido. Intenta iniciar sesión de nuevo.");
  }

  const sessionState: AuthSessionState = {
    token: tokenResult.accessToken,
    user,
  };

  await SecureStore.setItemAsync(storageKey, JSON.stringify(sessionState));
  if (tokenResult.refreshToken) {
    await SecureStore.setItemAsync(refreshKey, tokenResult.refreshToken);
  } else {
    await SecureStore.deleteItemAsync(refreshKey);
  }
  return sessionState;
}

export async function restoreSessionAsync(): Promise<AuthSessionState | null> {
  const raw = await SecureStore.getItemAsync(storageKey);
  if (!raw) {
    return null;
  }

  try {
    const parsed = JSON.parse(raw) as AuthSessionState;
    if (!parsed?.token || !parsed?.user?.sub) {
      return null;
    }

    if (isJwtExpired(parsed.token)) {
      await SecureStore.deleteItemAsync(storageKey);
      await SecureStore.deleteItemAsync(refreshKey);
      return null;
    }

    return parsed;
  } catch {
    return null;
  }
}

export async function logoutAsync(): Promise<void> {
  try {
    const refreshToken = await SecureStore.getItemAsync(refreshKey);
    if (refreshToken) {
      // Cierre de sesion por backchannel: invalida la sesion SSO en Keycloak con
      // un POST (sin abrir navegador, sin parpadeo). Si no se hace, la cookie SSO
      // sobrevive y el siguiente login reusa la misma cuenta sin pedir credenciales.
      const body =
        `client_id=${encodeURIComponent(mobileEnv.keycloakClientId)}` +
        `&refresh_token=${encodeURIComponent(refreshToken)}`;
      await fetch(discovery.endSessionEndpoint, {
        method: "POST",
        headers: { "Content-Type": "application/x-www-form-urlencoded" },
        body,
      });
    }
  } catch {
    // El logout local debe completarse aunque falle el cierre remoto (p.ej. sin red).
  } finally {
    await SecureStore.deleteItemAsync(refreshKey);
    await SecureStore.deleteItemAsync(storageKey);
  }
}
