// HU-49: rendimiento histórico del equipo activo del participante autenticado (Bloque 7e).
// Encadena identity/teams/mine (equipoId del equipo activo, patrón partidaLobbyFlow.js:35)
// con puntuaciones/equipos/{equipoId}/rendimiento.
export async function getRendimientoMiEquipo(apiBaseUrl, token, fetchImpl = fetch) {
  let miEquipo;
  try {
    miEquipo = await fetchImpl(`${apiBaseUrl}/identity/teams/mine`, {
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

  if (miEquipo.status === 401) {
    return { ok: false, type: "unauthorized", message: "Sesión expirada o no autorizada." };
  }
  if (miEquipo.status === 404) {
    return { ok: false, type: "sinEquipo", message: "No perteneces a un equipo activo." };
  }
  if (!miEquipo.ok) {
    return { ok: false, type: "error", message: "No se pudo obtener tu equipo." };
  }

  const equipo = await miEquipo.json().catch(() => null);
  const equipoId = equipo?.equipoId;
  if (!equipoId) {
    return { ok: false, type: "sinEquipo", message: "No perteneces a un equipo activo." };
  }

  let response;
  try {
    response = await fetchImpl(`${apiBaseUrl}/puntuaciones/equipos/${equipoId}/rendimiento`, {
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
    return { ok: false, type: "error", message: "No se pudo cargar el rendimiento del equipo." };
  }

  const body = await response.json().catch(() => null);
  return {
    ok: true,
    data: {
      equipoId: body?.equipoId ?? equipoId,
      partidas: body?.partidas ?? [],
    },
  };
}
