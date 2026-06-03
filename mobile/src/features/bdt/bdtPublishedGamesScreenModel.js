import { loadPublishedBdtGames } from "./bdtPublishedGamesFlow.js";

export async function loadPublishedBdtGamesFromScreen({
  apiBaseUrl,
  token,
  filter,
  loadFn = loadPublishedBdtGames,
  setLoading,
  setErrorMessage,
  setGames,
}) {
  setLoading(true);
  setErrorMessage(null);

  let result;
  try {
    result = await loadFn({ apiBaseUrl, token, filter });
  } catch {
    setLoading(false);
    setErrorMessage("Ocurrio un error inesperado. Intenta nuevamente.");
    return;
  }

  setLoading(false);

  if (!result.ok) {
    setGames([]);
    setErrorMessage(result.message);
    return;
  }

  setGames(Array.isArray(result.data) ? result.data : []);
}
