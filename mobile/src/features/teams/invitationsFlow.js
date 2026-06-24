import { loadInvitations, acceptInvitation, rejectInvitation } from "./invitationsApi.js";

export async function fetchInvitations({ apiBaseUrl, token, fetchImpl }) {
  try {
    return await loadInvitations(apiBaseUrl, token, fetchImpl);
  } catch {
    return {
      ok: false,
      type: "error",
      message: "Ocurrio un error inesperado al cargar las invitaciones. Intenta nuevamente.",
    };
  }
}

export async function submitAcceptInvitation({ apiBaseUrl, token, invitacionId, fetchImpl }) {
  if (!invitacionId) {
    return { ok: false, type: "validation", message: "ID de invitacion invalido." };
  }

  try {
    return await acceptInvitation(apiBaseUrl, token, invitacionId, fetchImpl);
  } catch {
    return {
      ok: false,
      type: "error",
      message: "Ocurrio un error inesperado al aceptar la invitacion. Intenta nuevamente.",
    };
  }
}

export async function submitRejectInvitation({ apiBaseUrl, token, invitacionId, fetchImpl }) {
  if (!invitacionId) {
    return { ok: false, type: "validation", message: "ID de invitacion invalido." };
  }

  try {
    return await rejectInvitation(apiBaseUrl, token, invitacionId, fetchImpl);
  } catch {
    return {
      ok: false,
      type: "error",
      message: "Ocurrio un error inesperado al rechazar la invitacion. Intenta nuevamente.",
    };
  }
}
