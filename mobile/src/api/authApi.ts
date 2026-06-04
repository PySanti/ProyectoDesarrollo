import AsyncStorage from '@react-native-async-storage/async-storage';

const KEYCLOAK_URL = 'http://localhost:8080';
const REALM = 'umbral';
const CLIENT_ID = 'umbral-api';

export interface TokenResponse {
  access_token: string;
  refresh_token: string;
  expires_in: number;
}

export async function login(
  username: string,
  password: string,
): Promise<TokenResponse> {
  const body = new URLSearchParams({
    client_id: CLIENT_ID,
    username,
    password,
    grant_type: 'password',
  });

  const response = await fetch(
    `${KEYCLOAK_URL}/realms/${REALM}/protocol/openid-connect/token`,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: body.toString(),
    },
  );

  if (!response.ok) {
    const error = await response.text();
    throw new Error(error);
  }

  const data: TokenResponse = await response.json();
  await AsyncStorage.setItem('auth_token', data.access_token);
  await AsyncStorage.setItem('refresh_token', data.refresh_token);
  return data;
}

export async function logout(): Promise<void> {
  await AsyncStorage.removeItem('auth_token');
  await AsyncStorage.removeItem('refresh_token');
}

export async function getStoredToken(): Promise<string | null> {
  return AsyncStorage.getItem('auth_token');
}

export async function isAuthenticated(): Promise<boolean> {
  const token = await getStoredToken();
  return token !== null;
}
