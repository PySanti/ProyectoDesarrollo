import { transferTeamLeadership } from "./transferLeadershipApi.js";

const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export function validateNewLeaderUserId(nuevoLiderUserId) {
  const value = typeof nuevoLiderUserId === "string" ? nuevoLiderUserId.trim() : "";
  if (!guidPattern.test(value)) {
    return { ok: false, type: "validation", message: "Selecciona un nuevo lider valido." };
  }

  return { ok: true, nuevoLiderUserId: value };
}

export function getEligibleLeaderMembers(members = [], currentLeaderUserId) {
  return members.filter((member) => {
    const userId = member.userId ?? member.usuarioId;
    return userId && userId !== currentLeaderUserId && member.esLider !== true;
  });
}

export async function submitTransferLeadership({ apiBaseUrl, token, nuevoLiderUserId, fetchImpl }) {
  const validation = validateNewLeaderUserId(nuevoLiderUserId);
  if (!validation.ok) {
    return validation;
  }

  try {
    return await transferTeamLeadership(apiBaseUrl, token, validation.nuevoLiderUserId, fetchImpl);
  } catch {
    return {
      ok: false,
      type: "error",
      message: "Ocurrio un error inesperado al transferir el liderazgo.",
    };
  }
}
