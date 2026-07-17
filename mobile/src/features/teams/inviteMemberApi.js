export async function loadEligibleParticipants(apiBaseUrl, token, fetchImpl = fetch) {
  let response;
  try {
    response = await fetchImpl(`${apiBaseUrl}/identity/teams/eligible-participants`, {
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
    return {
      ok: false,
      type: "forbidden",
      message: "Debes ser lider del equipo para ver participantes elegibles.",
    };
  }

  if (!response.ok) {
    return { ok: false, type: "error", message: "No se pudieron cargar los participantes elegibles." };
  }

  const data = await response.json();
  return { ok: true, data };
}

export async function sendInvitation(apiBaseUrl, token, invitadoUserId, fetchImpl = fetch) {
  let response;
  try {
    response = await fetchImpl(`${apiBaseUrl}/identity/teams/invitations`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify({ invitadoUserId }),
    });
  } catch {
    return {
      ok: false,
      type: "network",
      message: "No se pudo conectar con el servidor. Verifica tu conexion e intenta de nuevo.",
    };
  }

  if (response.status === 404) {
    return { ok: false, type: "notFound", message: "No se encontro el participante o el equipo." };
  }

  if (response.status === 409) {
    // Varios casos comparten 409; el backend los distingue con `code` (nombre del tipo de
    // excepcion). Sin `code` se cae al mensaje generico.
    let code;
    try {
      code = (await response.json())?.code;
    } catch {
      code = undefined;
    }

    const messageByCode = {
      InvitacionPendienteYaExisteException: "Ya hay una invitacion activa para este participante.",
      EquipoLlenoException: "Tu equipo ya esta lleno (maximo 5 miembros).",
      UsuarioYaEnEquipoException: "Ese participante ya pertenece a un equipo.",
    };

    return {
      ok: false,
      type: "conflict",
      message:
        messageByCode[code] ??
        "No se pudo enviar la invitacion. El equipo puede estar lleno o el participante ya pertenece a un equipo.",
    };
  }

  if (response.status === 401) {
    return { ok: false, type: "unauthorized", message: "Sesion expirada o no autorizada." };
  }

  if (response.status === 403) {
    return { ok: false, type: "forbidden", message: "Debes ser lider del equipo para enviar invitaciones." };
  }

  if (!response.ok) {
    return { ok: false, type: "error", message: "No se pudo enviar la invitacion." };
  }

  const data = await response.json();
  return { ok: true, data };
}
