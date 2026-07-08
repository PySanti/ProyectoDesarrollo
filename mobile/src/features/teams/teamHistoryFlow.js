import { fetchTeamHistory } from "./teamHistoryApi.js";

export async function loadTeamHistory({ apiBaseUrl, token, fetchImpl = fetch }) {
  try {
    return await fetchTeamHistory(apiBaseUrl, token, fetchImpl);
  } catch {
    return {
      ok: false,
      type: "error",
      message: "Ocurrió un error inesperado al cargar el historial. Intenta nuevamente.",
    };
  }
}
