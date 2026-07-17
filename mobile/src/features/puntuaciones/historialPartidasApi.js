// HU-27: historial de partidas jugadas del participante autenticado (Bloque 7e).
// Patrón de partidasPublicadasApi.js: fetch autenticado + mapeo de error por status.
import { parseJwtPayload } from "../../auth/tokenClaims.js";

export async function getHistorialPartidas(apiBaseUrl, token, fetchImpl = fetch) {
  let participanteId;
  try {
    participanteId = parseJwtPayload(token).sub;
  } catch {
    return { ok: false, type: "error", message: "No se pudo identificar al participante." };
  }

  let response;
  try {
    response = await fetchImpl(`${apiBaseUrl}/puntuaciones/participantes/${participanteId}/historial-partidas`, {
      method: "GET",
      headers: { Authorization: `Bearer ${token}` },
    });
  } catch {
    return {
      ok: false,
      type: "network",
      message: "No se pudo conectar con el servidor. Verifica tu conexión e intenta de nuevo.",
    };
  }

  if (response.status === 401) {
    return { ok: false, type: "unauthorized", message: "Sesión expirada o no autorizada." };
  }
  if (!response.ok) {
    return { ok: false, type: "error", message: "No se pudo cargar el historial de partidas." };
  }

  const body = await response.json().catch(() => null);
  return {
    ok: true,
    data: {
      participanteId: body?.participanteId ?? participanteId,
      partidas: body?.partidas ?? [],
    },
  };
}
