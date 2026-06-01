export async function joinTeamByCode(apiBaseUrl, token, payload, fetchImpl = fetch) {
  let response;
  try {
    response = await fetchImpl(`${apiBaseUrl}/api/teams/join-by-code`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(payload),
    });
  } catch {
    return {
      ok: false,
      type: "network",
      message: "No se pudo conectar con el servidor. Verifica tu conexion e intenta de nuevo.",
    };
  }

  if (response.status === 404) {
    return { ok: false, type: "notFound", message: "El codigo ingresado no corresponde a un equipo activo." };
  }

  if (response.status === 409) {
    return { ok: false, type: "conflict", message: "No puedes unirte a este equipo." };
  }

  if (response.status === 401) {
    return { ok: false, type: "unauthorized", message: "Sesion expirada o no autorizada." };
  }

  if (response.status === 403) {
    return { ok: false, type: "forbidden", message: "Debes tener rol Participante para unirte a un equipo." };
  }

  if (!response.ok) {
    return { ok: false, type: "error", message: "No se pudo unir al equipo." };
  }

  const data = await response.json();
  return { ok: true, data };
}
