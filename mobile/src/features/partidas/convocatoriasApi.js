import { mapCommonError, networkError } from "./partidasPublicadasApi.js";

export async function getMisConvocatorias(apiBaseUrl, token, fetchImpl = fetch) {
  let response;
  try {
    response = await fetchImpl(`${apiBaseUrl}/operaciones-sesion/mis-convocatorias`, {
      method: "GET",
      headers: { Authorization: `Bearer ${token}` },
    });
  } catch {
    return networkError();
  }
  const body = await response.json().catch(() => null);
  if (!response.ok) {
    return mapCommonError(response.status, body);
  }
  return { ok: true, data: body ?? [] };
}

async function responder(apiBaseUrl, token, convocatoriaId, accion, fetchImpl) {
  let response;
  try {
    response = await fetchImpl(`${apiBaseUrl}/operaciones-sesion/convocatorias/${convocatoriaId}/${accion}`, {
      method: "POST",
      headers: { Authorization: `Bearer ${token}` },
    });
  } catch {
    return networkError();
  }
  const body = await response.json().catch(() => null);
  if (!response.ok) {
    return mapCommonError(response.status, body);
  }
  return { ok: true, data: body };
}

export function aceptarConvocatoria(apiBaseUrl, token, convocatoriaId, fetchImpl = fetch) {
  return responder(apiBaseUrl, token, convocatoriaId, "aceptacion", fetchImpl);
}

export function rechazarConvocatoria(apiBaseUrl, token, convocatoriaId, fetchImpl = fetch) {
  return responder(apiBaseUrl, token, convocatoriaId, "rechazo", fetchImpl);
}
