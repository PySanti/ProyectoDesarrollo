import { loadTeamHistory } from "./teamHistoryFlow.js";

export async function loadTeamHistoryForScreen({
  apiBaseUrl,
  token,
  loadFn = loadTeamHistory,
  setLoading,
  setErrorMessage,
  setHistorial,
}) {
  setLoading(true);
  setErrorMessage(null);

  let result;
  try {
    result = await loadFn({ apiBaseUrl, token });
  } catch {
    setLoading(false);
    setErrorMessage("Ocurrió un error inesperado al cargar el historial. Intenta nuevamente.");
    return;
  }

  setLoading(false);

  if (!result.ok) {
    setErrorMessage(result.message);
    return;
  }

  setHistorial(result.data.historial);
}
