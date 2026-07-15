export async function fetchTeamHistory(apiBaseUrl, token, fetchImpl = fetch) {
  let response;
  try {
    response = await fetchImpl(`${apiBaseUrl}/identity/teams/mine/history`, {
      method: "GET",
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

  if (response.status === 401) {
    return { ok: false, type: "unauthorized", message: "Sesión expirada o no autorizada." };
  }

  if (!response.ok) {
    return { ok: false, type: "error", message: "No se pudo cargar el historial de equipos." };
  }

  const data = await response.json();
  return { ok: true, data: { historial: data?.historial ?? [] } };
}
