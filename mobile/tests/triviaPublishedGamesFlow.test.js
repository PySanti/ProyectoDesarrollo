import assert from "node:assert/strict";
import test from "node:test";
import {
  buildPublishedTriviaGamesUrl,
  mapTriviaModalityFilterToQuery,
  summarizePublishedTriviaGame,
} from "../src/features/trivia/triviaPublishedGamesModel.js";

test("HU-09 published Trivia URL omits modalidad when filter is Todas", () => {
  const result = buildPublishedTriviaGamesUrl("http://localhost:5003/", "Todas");

  assert.equal(result.ok, true);
  assert.equal(result.url, "http://localhost:5003/api/trivia-games");
});

test("HU-11 published Trivia URL includes modalidad filter", () => {
  const result = buildPublishedTriviaGamesUrl("http://localhost:5003", "Equipo");

  assert.equal(result.ok, true);
  assert.equal(result.url, "http://localhost:5003/api/trivia-games?modalidad=Equipo");
});

test("HU-11 modality filter accepts only documented values", () => {
  assert.deepEqual(mapTriviaModalityFilterToQuery("Individual"), { ok: true, modalidad: "Individual" });
  assert.deepEqual(mapTriviaModalityFilterToQuery("Equipo"), { ok: true, modalidad: "Equipo" });

  const invalid = mapTriviaModalityFilterToQuery("Mixta");
  assert.equal(invalid.ok, false);
  assert.equal(invalid.type, "validation");
});

test("HU-09 mobile summary separates individual and team capacity labels", () => {
  assert.equal(
    summarizePublishedTriviaGame({ modalidad: "Individual", minimoParticipantes: 1, maximoJugadores: 10 }),
    "Jugadores: 1 - 10",
  );
  assert.equal(
    summarizePublishedTriviaGame({ modalidad: "Equipo", minimoParticipantes: 2, maximoEquipos: 5 }),
    "Equipos: 2 - 5",
  );
});
