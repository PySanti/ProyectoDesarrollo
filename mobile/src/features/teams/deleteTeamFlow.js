import { deleteMyTeam } from "./deleteTeamApi.js";

export async function submitDeleteTeam({ apiBaseUrl, token, fetchImpl = fetch }) {
  try {
    return await deleteMyTeam(apiBaseUrl, token, fetchImpl);
  } catch {
    return {
      ok: false,
      type: "error",
      message: "Ocurrió un error inesperado al eliminar el equipo. Intenta nuevamente.",
    };
  }
}
