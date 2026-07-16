export async function loadMyTeam(apiBaseUrl, token, fetchImpl = fetch) {
  let response;
  try {
    response = await fetchImpl(`${apiBaseUrl}/identity/teams/mine`, {
      method: "GET",
      headers: {
        Authorization: `Bearer ${token}`,
      },
    });
  } catch {
    return {
      ok: false,
      type: "network",
      message: "No se pudo conectar con el servidor. Verifica tu conexion e intenta de nuevo.",
    };
  }

  if (response.status === 404) {
    return { ok: true, data: null };
  }

  if (response.status === 401) {
    return { ok: false, type: "unauthorized", message: "Sesion expirada o no autorizada." };
  }

  if (response.status === 403) {
    return { ok: false, type: "forbidden", message: "Debes tener rol Participante para ver tu equipo." };
  }

  if (!response.ok) {
    return { ok: false, type: "error", message: "No se pudo cargar la informacion de tu equipo." };
  }

  const data = await response.json();
  return { ok: true, data };
}
