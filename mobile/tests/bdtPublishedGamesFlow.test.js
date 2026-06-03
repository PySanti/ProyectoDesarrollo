import test from "node:test";
import assert from "node:assert/strict";
import { joinPublishedIndividualBdtGame, loadPublishedBdtGames, mapBdtModalityFilterToQuery } from "../src/features/bdt/bdtPublishedGamesFlow.js";
import { joinIndividualBdtGame } from "../src/features/bdt/bdtPublishedGamesApi.js";

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

test("joinIndividualBdtGame should call documented endpoint with POST, auth header and no body", async () => {
  let requestedUrl;
  let requestedOptions;
  const result = await joinIndividualBdtGame(
    "http://localhost:5004",
    "token",
    "00000000-0000-0000-0000-000000000039",
    async (url, options) => {
      requestedUrl = url;
      requestedOptions = options;
      return {
        ok: true,
        status: 200,
        json: async () => ({
          partidaId: "00000000-0000-0000-0000-000000000039",
          nombre: "Ruta QR",
          modalidad: "Individual",
          estado: "Lobby",
          inscripcionId: "00000000-0000-0000-0000-000000000001",
          participanteUserId: "00000000-0000-0000-0000-000000000002",
          posicionEnLobby: 1,
          mensaje: "Te uniste a la BDT. Espera el inicio de la partida.",
        }),
      };
    },
  );

  assert.equal(requestedUrl, "http://localhost:5004/api/bdt/games/00000000-0000-0000-0000-000000000039/individual-inscriptions");
  assert.equal(requestedOptions.method, "POST");
  assert.equal(requestedOptions.headers.Authorization, "Bearer token");
  assert.equal(Object.hasOwn(requestedOptions, "body"), false);
  assert.equal(result.ok, true);
  assert.equal(result.data.posicionEnLobby, 1);
});

test("joinPublishedIndividualBdtGame should reject team modality before API call", async () => {
  let called = false;
  const result = await joinPublishedIndividualBdtGame({
    apiBaseUrl: "http://localhost:5004",
    token: "token",
    game: { partidaId: "1", modalidad: "Equipo" },
    fetchImpl: async () => {
      called = true;
      return { ok: true, status: 200, json: async () => ({}) };
    },
  });

  assert.equal(result.ok, false);
  assert.equal(result.type, "validation");
  assert.equal(called, false);
});

test("joinPublishedIndividualBdtGame should propagate waiting screen DTO", async () => {
  const result = await joinPublishedIndividualBdtGame({
    apiBaseUrl: "http://localhost:5004",
    token: "token",
    game: { partidaId: "00000000-0000-0000-0000-000000000039", modalidad: "Individual" },
    fetchImpl: async () => ({
      ok: true,
      status: 200,
      json: async () => ({ nombre: "Ruta QR", modalidad: "Individual", posicionEnLobby: 2 }),
    }),
  });

  assert.equal(result.ok, true);
  assert.equal(result.data.nombre, "Ruta QR");
  assert.equal(result.data.posicionEnLobby, 2);
});

test("joinIndividualBdtGame should map documented errors", async () => {
  const statuses = [401, 403, 404, 409, 500];
  const types = [];

  for (const status of statuses) {
    const result = await joinIndividualBdtGame("http://localhost:5004", "token", "game", async () => ({ ok: false, status }));
    types.push(result.type);
  }

  assert.deepEqual(types, ["unauthorized", "forbidden", "notFound", "conflict", "error"]);
});

test("joinIndividualBdtGame should map network failures", async () => {
  const result = await joinIndividualBdtGame("http://localhost:5004", "token", "game", async () => {
    throw new Error("offline");
  });

  assert.equal(result.ok, false);
  assert.equal(result.type, "network");
});
