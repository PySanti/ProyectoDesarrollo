export async function deleteMyTeam(apiBaseUrl, token, fetchImpl = fetch) {
  let response;
  try {
    response = await fetchImpl(`${apiBaseUrl}/identity/teams/mine`, {
      method: "DELETE",
      headers: {
        Authorization: `Bearer ${token}`,
      },
    });
  } catch {
    return {
      ok: false,
      type: "network",
      message: "No se pudo conectar con el servidor. Verifica tu conexión e intenta de nuevo.",
    };
  }

  if (response.status === 404) {
    return { ok: false, type: "notFound", message: "No perteneces a ningún equipo activo." };
  }

  if (response.status === 409) {
    return {
      ok: false,
      type: "activeParticipation",
      message: "Tu equipo participa en una partida activa y no puede eliminarse.",
    };
  }

  if (response.status === 401) {
    return { ok: false, type: "unauthorized", message: "Sesión expirada o no autorizada." };
  }

  if (response.status === 403) {
    return { ok: false, type: "forbidden", message: "Solo el líder puede eliminar el equipo." };
  }

  if (!response.ok) {
    return { ok: false, type: "error", message: "No se pudo eliminar el equipo." };
  }

  return { ok: true };
}
