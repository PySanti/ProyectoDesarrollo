import { listPublishedBdtGames } from "./bdtPublishedGamesApi.js";

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
