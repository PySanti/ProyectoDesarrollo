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
  teamApiBaseUrl: required(process.env.EXPO_PUBLIC_TEAM_API_BASE_URL, "EXPO_PUBLIC_TEAM_API_BASE_URL"),
  bdtApiBaseUrl: required(process.env.EXPO_PUBLIC_BDT_API_BASE_URL, "EXPO_PUBLIC_BDT_API_BASE_URL"),
  triviaApiBaseUrl: required(process.env.EXPO_PUBLIC_TRIVIA_API_BASE_URL, "EXPO_PUBLIC_TRIVIA_API_BASE_URL"),
  redirectScheme: process.env.EXPO_PUBLIC_APP_SCHEME?.trim() || "umbral",
  authRedirectUri: process.env.EXPO_PUBLIC_AUTH_REDIRECT_URI?.trim(),
};
