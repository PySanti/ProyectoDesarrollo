export async function transferTeamLeadership(apiBaseUrl, token, nuevoLiderUserId, fetchImpl = fetch) {
  let response;
  try {
    response = await fetchImpl(`${apiBaseUrl}/api/teams/leadership`, {
      method: "PATCH",
      headers: {
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ nuevoLiderUserId }),
    });
  } catch {
    return {
      ok: false,
      type: "network",
      message: "No se pudo conectar con el servidor. Verifica tu conexion e intenta de nuevo.",
    };
  }

  if (response.status === 400) {
    return { ok: false, type: "validation", message: "Selecciona un nuevo lider valido." };
  }

  if (response.status === 404) {
    return { ok: false, type: "notFound", message: "No perteneces a ningun equipo activo." };
  }

  if (response.status === 409) {
    return {
      ok: false,
      type: "conflict",
      message: "No se pudo transferir el liderazgo. Verifica que seas lider y que el nuevo lider pertenezca al equipo.",
    };
  }

  if (response.status === 401) {
    return { ok: false, type: "unauthorized", message: "Sesion expirada o no autorizada." };
  }

  if (response.status === 403) {
    return { ok: false, type: "forbidden", message: "Debes tener rol Participante para transferir liderazgo." };
  }

  if (!response.ok) {
    return { ok: false, type: "error", message: "No se pudo transferir el liderazgo." };
  }

  const data = await response.json();
  return { ok: true, data };
}
