const required = (value: string | undefined, key: string): string => {
  if (!value || !value.trim()) {
    throw new Error(`Missing environment variable: ${key}`);
  }

  return value.trim();
};

export const mobileEnv = {
  keycloakUrl: required(process.env.EXPO_PUBLIC_KEYCLOAK_URL, "EXPO_PUBLIC_KEYCLOAK_URL"),
  keycloakRealm: required(process.env.EXPO_PUBLIC_KEYCLOAK_REALM, "EXPO_PUBLIC_KEYCLOAK_REALM"),
  keycloakClientId: required(process.env.EXPO_PUBLIC_KEYCLOAK_CLIENT_ID, "EXPO_PUBLIC_KEYCLOAK_CLIENT_ID"),
  gatewayApiBaseUrl: required(process.env.EXPO_PUBLIC_GATEWAY_BASE_URL, "EXPO_PUBLIC_GATEWAY_BASE_URL"),
  redirectScheme: process.env.EXPO_PUBLIC_APP_SCHEME?.trim() || "umbral",
  authRedirectUri: process.env.EXPO_PUBLIC_AUTH_REDIRECT_URI?.trim(),
};
