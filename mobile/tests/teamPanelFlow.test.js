import test from "node:test";
import assert from "node:assert/strict";
import { fetchMyTeamStatus } from "../src/features/teams/teamPanelFlow.js";

const API_BASE = "https://api.test";
const TOKEN = "test-token";
const USER_ID = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const OTHER_ID = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

test("fetchMyTeamStatus calls GET /identity/teams/mine with Bearer token", async () => {
  let requestedUrl;
  let requestedHeaders;

  await fetchMyTeamStatus({
    apiBaseUrl: API_BASE,
    token: TOKEN,
    currentUserId: USER_ID,
    fetchImpl: async (url, options) => {
      requestedUrl = url;
      requestedHeaders = options.headers;
      return { ok: false, status: 404 };
    },
  });

  assert.equal(requestedUrl, `${API_BASE}/identity/teams/mine`);
  assert.equal(requestedHeaders["Authorization"], `Bearer ${TOKEN}`);
});

test("fetchMyTeamStatus returns sinEquipo on 404", async () => {
  const result = await fetchMyTeamStatus({
    apiBaseUrl: API_BASE,
    token: TOKEN,
    currentUserId: USER_ID,
    fetchImpl: async () => ({ ok: false, status: 404 }),
  });

  assert.equal(result.ok, true);
  assert.equal(result.status, "sinEquipo");
});

test("fetchMyTeamStatus returns lider when current user is esLider true", async () => {
  const result = await fetchMyTeamStatus({
    apiBaseUrl: API_BASE,
    token: TOKEN,
    currentUserId: USER_ID,
    fetchImpl: async () => ({
      ok: true,
      status: 200,
      json: async () => ({
        equipoId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
        nombreEquipo: "Los Halcones",
        estado: "Activo",
        participantes: [
          { usuarioId: USER_ID, nombre: "Ana", esLider: true },
          { usuarioId: OTHER_ID, nombre: "Beto", esLider: false },
        ],
      }),
    }),
  });

  assert.equal(result.ok, true);
  assert.equal(result.status, "lider");
  assert.equal(result.nombreEquipo, "Los Halcones");
  assert.equal(result.participantes.length, 2);
});

test("fetchMyTeamStatus returns miembro when current user is esLider false", async () => {
  const result = await fetchMyTeamStatus({
    apiBaseUrl: API_BASE,
    token: TOKEN,
    currentUserId: OTHER_ID,
    fetchImpl: async () => ({
      ok: true,
      status: 200,
      json: async () => ({
        equipoId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
        nombreEquipo: "Los Halcones",
        estado: "Activo",
        participantes: [
          { usuarioId: USER_ID, nombre: "Ana", esLider: true },
          { usuarioId: OTHER_ID, nombre: "Beto", esLider: false },
        ],
      }),
    }),
  });

  assert.equal(result.ok, true);
  assert.equal(result.status, "miembro");
});

test("fetchMyTeamStatus returns network error when fetch throws", async () => {
  const result = await fetchMyTeamStatus({
    apiBaseUrl: API_BASE,
    token: TOKEN,
    currentUserId: USER_ID,
    fetchImpl: async () => {
      throw new Error("network down");
    },
  });

  assert.equal(result.ok, false);
  assert.equal(result.type, "network");
});

test("fetchMyTeamStatus returns unauthorized on 401", async () => {
  const result = await fetchMyTeamStatus({
    apiBaseUrl: API_BASE,
    token: TOKEN,
    currentUserId: USER_ID,
    fetchImpl: async () => ({ ok: false, status: 401 }),
  });

  assert.equal(result.ok, false);
  assert.equal(result.type, "unauthorized");
});

test("fetchMyTeamStatus returns error on unexpected status", async () => {
  const result = await fetchMyTeamStatus({
    apiBaseUrl: API_BASE,
    token: TOKEN,
    currentUserId: USER_ID,
    fetchImpl: async () => ({ ok: false, status: 500 }),
  });

  assert.equal(result.ok, false);
  assert.equal(result.type, "error");
});
