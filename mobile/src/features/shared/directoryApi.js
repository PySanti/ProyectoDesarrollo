import { mapCommonError, networkError } from "../partidas/partidasPublicadasApi.js";

export async function resolverNombres(apiBaseUrl, token, payload, fetchImpl = fetch) {
  let response;
  try {
    response = await fetchImpl(`${apiBaseUrl}/identity/directory/names`, {
      method: "POST",
      headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
      body: JSON.stringify(payload),
    });
  } catch {
    return networkError();
  }
  const body = await response.json().catch(() => null);
  if (!response.ok) {
    return mapCommonError(response.status, body);
  }
  return { ok: true, data: { participantes: body?.participantes ?? [], equipos: body?.equipos ?? [] } };
}
