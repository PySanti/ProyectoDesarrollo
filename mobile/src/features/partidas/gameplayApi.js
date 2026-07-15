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

// HU-24/BR-T06: aviso de cierre de pregunta con la respuesta correcta (PreguntaCerrada.textoOpcionCorrecta).
// null cuando el backend no envía el texto (payload aditivo, opcional).
export function formatRespuestaCorrecta(texto) {
  return texto ? `La respuesta correcta era: ${texto}` : null;
}

// HU-24/BR-T06 (7d review fix): guard anti-leak. `preguntaCerrada` vive a nivel de partida y
// sobrevive al cambiar de JuegoTrivia; sin este filtro por juegoId, la respuesta correcta de la
// última pregunta del juego anterior aparecería sobre la primera pregunta del juego nuevo antes
// de que cierre (contracts/http/operaciones-sesion-api.md: "revelado únicamente al cerrar, nunca antes").
export function seleccionarRespuestaCorrecta(preguntaCerrada, juegoId) {
  if (!preguntaCerrada || preguntaCerrada.juegoId !== juegoId) return null;
  return preguntaCerrada.texto ?? null;
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
