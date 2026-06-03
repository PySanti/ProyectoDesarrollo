import { joinPublishedIndividualBdtGame, loadPublishedBdtGames } from "./bdtPublishedGamesFlow.js";

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

export async function joinIndividualBdtFromScreen({
  apiBaseUrl,
  token,
  game,
  joinFn = joinPublishedIndividualBdtGame,
  setJoiningPartidaId,
  setJoinErrorMessage,
  setWaitingData,
}) {
  setJoiningPartidaId(game?.partidaId ?? null);
  setJoinErrorMessage(null);

  let result;
  try {
    result = await joinFn({ apiBaseUrl, token, game });
  } catch {
    setJoiningPartidaId(null);
    setJoinErrorMessage("Ocurrio un error inesperado. Intenta nuevamente.");
    return;
  }

  setJoiningPartidaId(null);

  if (!result.ok) {
    setJoinErrorMessage(result.message);
    return;
  }

  setWaitingData(result.data);
}
