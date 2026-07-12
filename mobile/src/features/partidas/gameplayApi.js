import { mapCommonError, networkError } from "./partidasPublicadasApi.js";

async function get(apiBaseUrl, token, path, fetchImpl) {
  let response;
  try {
    response = await fetchImpl(`${apiBaseUrl}${path}`, {
      method: "GET",
      headers: { Authorization: `Bearer ${token}` },
    });
  } catch {
    return { response: null, body: null, error: networkError() };
  }
  const body = await response.json().catch(() => null);
  return { response, body, error: null };
}

export async function getPreguntaActual(apiBaseUrl, token, partidaId, fetchImpl = fetch) {
  const { response, body, error } = await get(
    apiBaseUrl, token, `/operaciones-sesion/partidas/${partidaId}/pregunta-actual`, fetchImpl,
  );
  if (error) return error;
  if (response.status === 409) {
    return { ok: false, type: "sin_pregunta", message: body?.message || "Sin pregunta activa." };
  }
  if (!response.ok) return mapCommonError(response.status, body);
  return { ok: true, pregunta: body };
}

export async function responderPregunta(apiBaseUrl, token, partidaId, opcionId, fetchImpl = fetch) {
  let response;
  try {
    response = await fetchImpl(
      `${apiBaseUrl}/operaciones-sesion/partidas/${partidaId}/pregunta-actual/respuesta`,
      {
        method: "POST",
        headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
        body: JSON.stringify({ opcionId }),
      },
    );
  } catch {
    return networkError();
  }
  const body = await response.json().catch(() => null);
  if (!response.ok) return mapCommonError(response.status, body);
  return { ok: true, data: body };
}

export async function getRankingJuego(apiBaseUrl, token, partidaId, juegoId, fetchImpl = fetch) {
  const { response, body, error } = await get(
    apiBaseUrl, token, `/puntuaciones/partidas/${partidaId}/juegos/${juegoId}/ranking`, fetchImpl,
  );
  if (error) return error;
  if (!response.ok) return mapCommonError(response.status, body);
  return { ok: true, ranking: body };
}

export async function getRankingConsolidado(apiBaseUrl, token, partidaId, fetchImpl = fetch) {
  const { response, body, error } = await get(
    apiBaseUrl, token, `/puntuaciones/partidas/${partidaId}/ranking-consolidado`, fetchImpl,
  );
  if (error) return error;
  if (!response.ok) return mapCommonError(response.status, body);
  return { ok: true, ranking: body };
}

export async function getEtapaActual(apiBaseUrl, token, partidaId, fetchImpl = fetch) {
  const { response, body, error } = await get(
    apiBaseUrl, token, `/operaciones-sesion/partidas/${partidaId}/etapa-actual`, fetchImpl,
  );
  if (error) return error;
  if (response.status === 409) {
    return { ok: false, type: "sin_etapa", message: body?.message || "Sin etapa activa." };
  }
  if (!response.ok) return mapCommonError(response.status, body);
  return { ok: true, etapa: body };
}

export async function validarTesoro(apiBaseUrl, token, partidaId, imagenBase64, fetchImpl = fetch) {
  let response;
  try {
    response = await fetchImpl(
      `${apiBaseUrl}/operaciones-sesion/partidas/${partidaId}/etapa-actual/tesoro`,
      {
        method: "POST",
        headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
        body: JSON.stringify({ imagenBase64 }),
      },
    );
  } catch {
    return networkError();
  }
  const body = await response.json().catch(() => null);
  if (!response.ok) return mapCommonError(response.status, body);
  return { ok: true, data: body };
}
