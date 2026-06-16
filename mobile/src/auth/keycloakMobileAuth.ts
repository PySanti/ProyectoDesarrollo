import * as AuthSession from "expo-auth-session";
import * as SecureStore from "expo-secure-store";
import * as WebBrowser from "expo-web-browser";
import { mobileEnv } from "../config/env";
import { AuthSessionState } from "./authTypes";
import { buildAuthUser, isJwtExpired } from "./tokenClaims.js";

WebBrowser.maybeCompleteAuthSession();

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
  });

  await request.makeAuthUrlAsync(discovery);
  // preferEphemeralSession evita que el navegador comparta/persista la cookie SSO
  // de Keycloak entre logins. Junto con el cierre de sesion por backchannel en
  // logoutAsync, garantiza que un nuevo "iniciar sesion" muestre el formulario
  // limpio (sin reusar la cuenta anterior y sin panel de reautenticacion).
  const authResult = await request.promptAsync(discovery, { preferEphemeralSession: true });

  if (authResult.type !== "success" || !authResult.params.code) {
    throw new Error("Authentication was cancelled or failed.");
  }

  const tokenResult = await AuthSession.exchangeCodeAsync(
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

  if (!tokenResult.accessToken) {
    throw new Error("Token endpoint did not return access token.");
  }

  const sessionState: AuthSessionState = {
    token: tokenResult.accessToken,
    user: buildAuthUser(tokenResult.accessToken),
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
