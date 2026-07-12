// Refresh RNF-24 contra el token endpoint de Keycloak (cliente↔Keycloak directo,
// sin gateway/backend). Módulo puro con deps inyectadas: testeable bajo node:test.
export async function refrescarTokenFlow({ tokenEndpoint, clientId, refreshToken, fetchImpl, buildUser }) {
  if (!refreshToken) {
    return { ok: false };
  }
  try {
    const body =
      `grant_type=refresh_token` +
      `&client_id=${encodeURIComponent(clientId)}` +
      `&refresh_token=${encodeURIComponent(refreshToken)}`;
    const response = await fetchImpl(tokenEndpoint, {
      method: "POST",
      headers: { "Content-Type": "application/x-www-form-urlencoded" },
      body,
    });
    if (!response.ok) {
      return { ok: false };
    }
    const data = await response.json();
    if (!data?.access_token) {
      return { ok: false };
    }
    const user = buildUser(data.access_token);
    return { ok: true, token: data.access_token, user, refreshToken: data.refresh_token ?? null };
  } catch {
    return { ok: false };
  }
}
