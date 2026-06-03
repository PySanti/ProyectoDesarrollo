export async function listPublishedBdtGames(apiBaseUrl, token, modalidad, fetchImpl = fetch) {
  const query = modalidad ? `?modalidad=${encodeURIComponent(modalidad)}` : "";

  let response;
  try {
    response = await fetchImpl(`${apiBaseUrl}/api/bdt/games/published${query}`, {
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
    return { ok: false, type: "invalidFilter", message: "El filtro de modalidad no es valido." };
  }

  if (response.status === 401) {
    return { ok: false, type: "unauthorized", message: "Sesion expirada o no autorizada." };
  }

  if (response.status === 403) {
    return { ok: false, type: "forbidden", message: "Debes tener rol Participante para ver partidas BDT." };
  }

  if (!response.ok) {
    return { ok: false, type: "error", message: "No se pudieron cargar las partidas BDT publicadas." };
  }

  const data = await response.json();
  return { ok: true, data };
}

export async function joinIndividualBdtGame(apiBaseUrl, token, partidaId, fetchImpl = fetch) {
  let response;
  try {
    response = await fetchImpl(`${apiBaseUrl}/api/bdt/games/${encodeURIComponent(partidaId)}/individual-inscriptions`, {
      method: "POST",
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
    return { ok: false, type: "forbidden", message: "Debes tener rol Participante para unirte a una BDT." };
  }

  if (response.status === 404) {
    return { ok: false, type: "notFound", message: "La partida BDT ya no esta disponible." };
  }

  if (response.status === 409) {
    return {
      ok: false,
      type: "conflict",
      message: "No puedes unirte a esta BDT. Puede estar llena, no estar en lobby o ya estas inscrito.",
    };
  }

  if (!response.ok) {
    return { ok: false, type: "error", message: "No se pudo completar la inscripcion a la BDT." };
  }

  const data = await response.json();
  return { ok: true, data };
}
