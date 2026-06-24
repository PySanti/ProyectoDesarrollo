import { loadEligibleParticipants, sendInvitation } from "./inviteMemberApi.js";

export async function fetchEligibleParticipants({ apiBaseUrl, token, fetchImpl }) {
  try {
    return await loadEligibleParticipants(apiBaseUrl, token, fetchImpl);
  } catch {
    return {
      ok: false,
      type: "error",
      message: "Ocurrio un error inesperado al cargar los participantes. Intenta nuevamente.",
    };
  }
}

export async function submitInviteMember({ apiBaseUrl, token, invitadoUserId, fetchImpl }) {
  const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
  const value = typeof invitadoUserId === "string" ? invitadoUserId.trim() : "";

  if (!guidPattern.test(value)) {
    return { ok: false, type: "validation", message: "Selecciona un participante valido." };
  }

  try {
    return await sendInvitation(apiBaseUrl, token, value, fetchImpl);
  } catch {
    return {
      ok: false,
      type: "error",
      message: "Ocurrio un error inesperado al enviar la invitacion. Intenta nuevamente.",
    };
  }
}
