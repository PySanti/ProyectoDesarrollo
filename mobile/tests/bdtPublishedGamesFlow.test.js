import test from "node:test";
import assert from "node:assert/strict";
import { loadPublishedBdtGames, mapBdtModalityFilterToQuery } from "../src/features/bdt/bdtPublishedGamesFlow.js";

test("mapBdtModalityFilterToQuery should omit modalidad for Todas", () => {
  const result = mapBdtModalityFilterToQuery("Todas");

  assert.equal(result.ok, true);
  assert.equal(result.modalidad, undefined);
});

test("mapBdtModalityFilterToQuery should map Individual and Equipo", () => {
  assert.equal(mapBdtModalityFilterToQuery("Individual").modalidad, "Individual");
  assert.equal(mapBdtModalityFilterToQuery("Equipo").modalidad, "Equipo");
});

test("mapBdtModalityFilterToQuery should reject invalid filter", () => {
  const result = mapBdtModalityFilterToQuery("Mixta");

  assert.equal(result.ok, false);
  assert.equal(result.type, "validation");
});

test("loadPublishedBdtGames should call endpoint without modalidad for Todas", async () => {
  let requestedUrl;
  const result = await loadPublishedBdtGames({
    apiBaseUrl: "http://localhost:5004",
    token: "token",
    filter: "Todas",
    fetchImpl: async (url) => {
      requestedUrl = url;
      return { ok: true, status: 200, json: async () => [] };
    },
  });

  assert.equal(requestedUrl, "http://localhost:5004/api/bdt/games/published");
  assert.equal(result.ok, true);
  assert.deepEqual(result.data, []);
});

test("loadPublishedBdtGames should call endpoint with modality filter", async () => {
  let requestedUrl;
  const result = await loadPublishedBdtGames({
    apiBaseUrl: "http://localhost:5004",
    token: "token",
    filter: "Equipo",
    fetchImpl: async (url) => {
      requestedUrl = url;
      return {
        ok: true,
        status: 200,
        json: async () => [
          {
            partidaId: "00000000-0000-0000-0000-000000000001",
            nombre: "Ruta nocturna",
            modalidad: "Equipo",
            estado: "Lobby",
            areaBusqueda: "Campus central",
            cantidadEtapas: 3,
          },
        ],
      };
    },
  });

  assert.equal(requestedUrl, "http://localhost:5004/api/bdt/games/published?modalidad=Equipo");
  assert.equal(result.ok, true);
  assert.equal(result.data[0].modalidad, "Equipo");
});

test("loadPublishedBdtGames should map invalid modality response", async () => {
  const result = await loadPublishedBdtGames({
    apiBaseUrl: "http://localhost:5004",
    token: "token",
    filter: "Individual",
    fetchImpl: async () => ({ ok: false, status: 400 }),
  });

  assert.equal(result.ok, false);
  assert.equal(result.type, "invalidFilter");
});
