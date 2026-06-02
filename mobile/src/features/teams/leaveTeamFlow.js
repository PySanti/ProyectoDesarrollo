import { leaveTeamMembership } from "./leaveTeamApi.js";

export async function submitLeaveTeam({ apiBaseUrl, token, fetchImpl = fetch }) {
  let result;
  try {
    result = await leaveTeamMembership(apiBaseUrl, token, fetchImpl);
  } catch {
    return {
      ok: false,
      type: "error",
      message: "Ocurrio un error inesperado al salir del equipo. Intenta nuevamente.",
    };
  }

  if (!result.ok && result.type === "leaderMustTransfer") {
    return {
      ok: false,
      type: "leaderMustTransfer",
      message: "Debes transferir el liderazgo antes de salir del equipo.",
    };
  }

  return result;
}
