export async function getActiveBdtStage(apiBaseUrl, token, partidaId, fetchImpl = fetch) {
  let response;
  try {
    response = await fetchImpl(`${apiBaseUrl}/api/bdt/games/${encodeURIComponent(partidaId)}/active-stage`, {
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

  if (response.status === 400) {
    return { ok: false, type: "invalidGame", message: "La partida BDT seleccionada no es valida." };
  }

  if (response.status === 401) {
    return { ok: false, type: "unauthorized", message: "Sesion expirada o no autorizada." };
  }

  if (response.status === 403) {
    return { ok: false, type: "forbidden", message: "No estas registrado como participante activo de esta BDT." };
  }

  if (response.status === 404) {
    return { ok: false, type: "notFound", message: "La partida BDT ya no esta disponible." };
  }

  if (response.status === 409) {
    return { ok: false, type: "unavailable", message: "La etapa activa no esta disponible en este momento." };
  }

  if (!response.ok) {
    return { ok: false, type: "error", message: "No se pudo cargar la etapa activa BDT." };
  }

  const data = await response.json();
  return { ok: true, data };
}
