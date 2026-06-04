export function mapTriviaModalityFilterToQuery(filter) {
  if (filter === "Todas") {
    return { ok: true, modalidad: undefined };
  }

  if (filter === "Individual" || filter === "Equipo") {
    return { ok: true, modalidad: filter };
  }

  return {
    ok: false,
    type: "validation",
    message: "Filtro de modalidad de Trivia invalido.",
  };
}

export function buildPublishedTriviaGamesUrl(apiBaseUrl, filter = "Todas") {
  const parsed = mapTriviaModalityFilterToQuery(filter);

  if (!parsed.ok) {
    return parsed;
  }

  const baseUrl = apiBaseUrl.replace(/\/$/, "");
  const query = parsed.modalidad ? `?modalidad=${encodeURIComponent(parsed.modalidad)}` : "";

  return {
    ok: true,
    url: `${baseUrl}/api/trivia-games${query}`,
  };
}

export function summarizePublishedTriviaGame(game) {
  if (game?.modalidad === "Individual") {
    return `Jugadores: ${game.minimoParticipantes} - ${game.maximoJugadores ?? "-"}`;
  }

  return `Equipos: ${game?.minimoParticipantes ?? 0} - ${game?.maximoEquipos ?? "-"}`;
}
