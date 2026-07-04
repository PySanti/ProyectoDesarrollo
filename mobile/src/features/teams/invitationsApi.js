export async function loadInvitations(apiBaseUrl, token, fetchImpl = fetch) {
  let response;
  try {
    response = await fetchImpl(`${apiBaseUrl}/identity/teams/invitations`, {
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

  if (response.status === 401) {
    return { ok: false, type: "unauthorized", message: "Sesion expirada o no autorizada." };
  }

  if (response.status === 403) {
    return { ok: false, type: "forbidden", message: "Debes tener rol Participante para ver invitaciones." };
  }

  if (!response.ok) {
    return { ok: false, type: "error", message: "No se pudieron cargar las invitaciones." };
  }

  const data = await response.json();
  return { ok: true, data };
}

export async function acceptInvitation(apiBaseUrl, token, invitacionId, fetchImpl = fetch) {
  let response;
  try {
    response = await fetchImpl(`${apiBaseUrl}/identity/teams/invitations/${invitacionId}/acceptance`, {
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

  if (response.status === 404) {
    return { ok: false, type: "notFound", message: "La invitacion no existe o ya no esta pendiente." };
  }

  if (response.status === 409) {
    return {
      ok: false,
      type: "conflict",
      message: "No puedes aceptar esta invitacion. Es posible que ya pertenezcas a un equipo o el equipo este lleno.",
    };
  }

  if (response.status === 401) {
    return { ok: false, type: "unauthorized", message: "Sesion expirada o no autorizada." };
  }

  if (response.status === 403) {
    return { ok: false, type: "forbidden", message: "Debes tener rol Participante para aceptar invitaciones." };
  }

  if (!response.ok) {
    return { ok: false, type: "error", message: "No se pudo aceptar la invitacion." };
  }

  const data = await response.json();
  return { ok: true, data };
}

export async function rejectInvitation(apiBaseUrl, token, invitacionId, fetchImpl = fetch) {
  let response;
  try {
    response = await fetchImpl(`${apiBaseUrl}/identity/teams/invitations/${invitacionId}/rejection`, {
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

  if (response.status === 404) {
    return { ok: false, type: "notFound", message: "La invitacion no existe o ya no esta pendiente." };
  }

  if (response.status === 401) {
    return { ok: false, type: "unauthorized", message: "Sesion expirada o no autorizada." };
  }

  if (response.status === 403) {
    return { ok: false, type: "forbidden", message: "Debes tener rol Participante para rechazar invitaciones." };
  }

  if (!response.ok) {
    return { ok: false, type: "error", message: "No se pudo rechazar la invitacion." };
  }

  const data = await response.json();
  return { ok: true, data };
}
