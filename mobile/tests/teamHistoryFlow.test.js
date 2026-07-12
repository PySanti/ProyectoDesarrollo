import test from "node:test";
import assert from "node:assert/strict";
import { loadTeamHistory } from "../src/features/teams/teamHistoryFlow.js";

test("loadTeamHistory should return ok with items on 200 with a non-empty list", async () => {
  const fetchImpl = async () => ({
    status: 200,
    ok: true,
    json: async () => ({
      historial: [{ nombreEquipo: "Titanes", equipoId: "e", fechaRegistro: "2026-07-08T00:00:00Z" }],
    }),
  });

  const result = await loadTeamHistory({ apiBaseUrl: "http://x", token: "t", fetchImpl });

  assert.equal(result.ok, true);
  assert.equal(result.data.historial.length, 1);
  assert.equal(result.data.historial[0].nombreEquipo, "Titanes");
});

test("loadTeamHistory should return ok with zero items on 200 with an empty list (not an error)", async () => {
  const fetchImpl = async () => ({ status: 200, ok: true, json: async () => ({ historial: [] }) });

  const result = await loadTeamHistory({ apiBaseUrl: "http://x", token: "t", fetchImpl });

  assert.equal(result.ok, true);
  assert.equal(result.data.historial.length, 0);
});

test("loadTeamHistory should default to an empty list when the body has no historial field", async () => {
  const fetchImpl = async () => ({ status: 200, ok: true, json: async () => ({}) });

  const result = await loadTeamHistory({ apiBaseUrl: "http://x", token: "t", fetchImpl });

  assert.equal(result.ok, true);
  assert.deepEqual(result.data.historial, []);
});

test("loadTeamHistory should return unauthorized on 401", async () => {
  const fetchImpl = async () => ({ status: 401, ok: false, json: async () => ({}) });

  const result = await loadTeamHistory({ apiBaseUrl: "http://x", token: "t", fetchImpl });

  assert.equal(result.ok, false);
  assert.equal(result.type, "unauthorized");
  assert.match(result.message, /Sesión expirada/);
});

test("loadTeamHistory should return an error on other non-ok statuses", async () => {
  const fetchImpl = async () => ({ status: 500, ok: false, json: async () => ({}) });

  const result = await loadTeamHistory({ apiBaseUrl: "http://x", token: "t", fetchImpl });

  assert.equal(result.ok, false);
  assert.equal(result.type, "error");
  assert.match(result.message, /No se pudo cargar el historial/);
});

test("loadTeamHistory should return a network error when fetch throws", async () => {
  const fetchImpl = async () => {
    throw new Error("network down");
  };

  const result = await loadTeamHistory({ apiBaseUrl: "http://x", token: "t", fetchImpl });

  assert.equal(result.ok, false);
  assert.equal(result.type, "network");
  assert.match(result.message, /No se pudo conectar con el servidor/);
});
