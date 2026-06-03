import { joinIndividualBdtGame, listPublishedBdtGames } from "./bdtPublishedGamesApi.js";

const validFilters = new Set(["Todas", "Individual", "Equipo"]);

export function mapBdtModalityFilterToQuery(filter) {
  if (!validFilters.has(filter)) {
    return { ok: false, type: "validation", message: "Selecciona una modalidad valida." };
  }

  return { ok: true, modalidad: filter === "Todas" ? undefined : filter };
}

export async function loadPublishedBdtGames({ apiBaseUrl, token, filter = "Todas", fetchImpl }) {
  const mappedFilter = mapBdtModalityFilterToQuery(filter);
  if (!mappedFilter.ok) {
    return mappedFilter;
  }

  let result;
  try {
    result = await listPublishedBdtGames(apiBaseUrl, token, mappedFilter.modalidad, fetchImpl);
  } catch {
    return {
      ok: false,
      type: "error",
      message: "Ocurrio un error inesperado al cargar las partidas BDT.",
    };
  }

  return result;
}

export async function joinPublishedIndividualBdtGame({ apiBaseUrl, token, game, fetchImpl }) {
  if (!game || !game.partidaId) {
    return { ok: false, type: "validation", message: "Selecciona una BDT valida." };
  }

  if (game.modalidad !== "Individual") {
    return { ok: false, type: "validation", message: "Solo puedes unirte individualmente a partidas BDT individuales." };
  }

  try {
    return await joinIndividualBdtGame(apiBaseUrl, token, game.partidaId, fetchImpl);
  } catch {
    return {
      ok: false,
      type: "error",
      message: "Ocurrio un error inesperado al unirte a la BDT.",
    };
  }
}
