import { mapCommonError, networkError } from "./partidasPublicadasApi.js";

export async function getMiSesion(apiBaseUrl, token, fetchImpl = fetch) {
  let response;
  try {
    response = await fetchImpl(`${apiBaseUrl}/operaciones-sesion/mi-sesion`, {
      method: "GET",
      headers: { Authorization: `Bearer ${token}` },
    });
  } catch {
    return networkError();
  }
  if (response.status === 204) {
    return { ok: true, sesion: null };
  }
  const body = await response.json().catch(() => null);
  if (!response.ok) {
    return mapCommonError(response.status, body);
  }
  return { ok: true, sesion: body };
}
