import * as AuthSession from "expo-auth-session";
import * as SecureStore from "expo-secure-store";
import * as WebBrowser from "expo-web-browser";
import { mobileEnv } from "../config/env";
import { AuthSessionState } from "./authTypes";
import { buildAuthUser, isJwtExpired } from "./tokenClaims.js";

WebBrowser.maybeCompleteAuthSession();

const storageKey = "umbral.auth.session";

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
  const authResult = await request.promptAsync(discovery);

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
      return null;
    }

    return parsed;
  } catch {
    return null;
  }
}

export async function logoutAsync(): Promise<void> {
  await SecureStore.deleteItemAsync(storageKey);
}
