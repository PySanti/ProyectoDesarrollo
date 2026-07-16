import { transferTeamLeadership } from "./transferLeadershipApi.js";

export function getEligibleLeaderMembers(members = [], currentLeaderUserId) {
  return members.filter((member) => {
    return member.usuarioId && member.usuarioId !== currentLeaderUserId && member.esLider !== true;
  });
}

// Only the current leader may transfer leadership: a `fetchMyTeamStatus` result of "miembro" (or
// anything other than "lider") must yield no eligible rows, even though the team roster is non-empty.
export function getParticipantesForTransfer(result) {
  return result?.ok && result.status === "lider" ? result.participantes : [];
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
