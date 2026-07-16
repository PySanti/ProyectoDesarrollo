import { transferTeamLeadership } from "./transferLeadershipApi.js";

export function getEligibleLeaderMembers(members = [], currentLeaderUserId) {
  return members.filter((member) => {
    return member.usuarioId && member.usuarioId !== currentLeaderUserId && member.esLider !== true;
  });
}

export async function submitTransferLeadership({ apiBaseUrl, token, nuevoLiderUserId, fetchImpl }) {
  try {
    return await transferTeamLeadership(apiBaseUrl, token, nuevoLiderUserId, fetchImpl);
  } catch {
    return {
      ok: false,
      type: "error",
      message: "Ocurrio un error inesperado al transferir el liderazgo.",
    };
  }
}
