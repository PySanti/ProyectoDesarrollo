export async function leaveTeamMembership(apiBaseUrl, token, fetchImpl = fetch) {
  let response;
  try {
    response = await fetchImpl(`${apiBaseUrl}/api/teams/membership`, {
      method: "DELETE",
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
    return { ok: false, type: "notFound", message: "No perteneces a ningun equipo activo." };
  }

  if (response.status === 409) {
    return {
      ok: false,
      type: "leaderMustTransfer",
      message: "Debes transferir el liderazgo antes de salir del equipo.",
    };
  }

  if (response.status === 401) {
    return { ok: false, type: "unauthorized", message: "Sesion expirada o no autorizada." };
  }

  if (response.status === 403) {
    return { ok: false, type: "forbidden", message: "Debes tener rol Participante para salir de un equipo." };
  }

  if (!response.ok) {
    return { ok: false, type: "error", message: "No se pudo salir del equipo." };
  }

  const data = await response.json();
  return { ok: true, data };
}
