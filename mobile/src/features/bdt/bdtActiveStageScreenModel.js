import { loadActiveBdtStage } from "./bdtActiveStageFlow.js";

export async function loadActiveBdtStageFromScreen({
  apiBaseUrl,
  token,
  partidaId,
  loadFn = loadActiveBdtStage,
  setLoading,
  setErrorMessage,
  setUnavailableMessage,
  setStageData,
}) {
  setLoading(true);
  setErrorMessage(null);
  setUnavailableMessage(null);

  let result;
  try {
    result = await loadFn({ apiBaseUrl, token, partidaId });
  } catch {
    setLoading(false);
    setErrorMessage("Ocurrio un error inesperado. Intenta nuevamente.");
    return;
  }

  setLoading(false);

  if (!result.ok) {
    setStageData(null);
    if (result.type === "unavailable") {
      setUnavailableMessage(result.message);
      return;
    }

    setErrorMessage(result.message);
    return;
  }

  setStageData(result.data);
}
