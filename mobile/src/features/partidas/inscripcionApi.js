import { mapCommonError, networkError } from "./partidasPublicadasApi.js";

async function send(apiBaseUrl, token, path, method, fetchImpl) {
  let response;
  try {
    response = await fetchImpl(`${apiBaseUrl}${path}`, {
      method,
      headers: { Authorization: `Bearer ${token}` },
    });
  } catch {
    return networkError();
  }
  if (response.status === 204) {
    return { ok: true };
  }
  const body = await response.json().catch(() => null);
  if (!response.ok) {
    return mapCommonError(response.status, body);
  }
  return { ok: true, data: body };
}

export function inscribirse(apiBaseUrl, token, partidaId, fetchImpl = fetch) {
  return send(apiBaseUrl, token, `/operaciones-sesion/partidas/${partidaId}/inscripciones`, "POST", fetchImpl);
}

export function cancelarInscripcion(apiBaseUrl, token, partidaId, fetchImpl = fetch) {
  return send(apiBaseUrl, token, `/operaciones-sesion/partidas/${partidaId}/inscripciones/mia`, "DELETE", fetchImpl);
}

export function preinscribirEquipo(apiBaseUrl, token, partidaId, fetchImpl = fetch) {
  return send(apiBaseUrl, token, `/operaciones-sesion/partidas/${partidaId}/inscripciones-equipo`, "POST", fetchImpl);
}

export function cancelarPreinscripcionEquipo(apiBaseUrl, token, partidaId, fetchImpl = fetch) {
  return send(apiBaseUrl, token, `/operaciones-sesion/partidas/${partidaId}/inscripciones-equipo/mia`, "DELETE", fetchImpl);
}
