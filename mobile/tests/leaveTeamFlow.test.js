import test from "node:test";
import assert from "node:assert/strict";
import { submitLeaveTeam } from "../src/features/teams/leaveTeamFlow.js";

test("submitLeaveTeam should call DELETE membership endpoint and return payload on success", async () => {
  const payload = {
    userId: "00000000-0000-0000-0000-000000000001",
    equipoId: "00000000-0000-0000-0000-000000000002",
    resultado: "SalioDelEquipo",
    equipoEstado: "Activo",
  };
  let calledUrl;
  let calledOptions;

  const result = await submitLeaveTeam({
    apiBaseUrl: "http://localhost:5001",
    token: "token",
    fetchImpl: async (url, options) => {
      calledUrl = url;
      calledOptions = options;
      return { ok: true, status: 200, json: async () => payload };
    },
  });

  assert.equal(calledUrl, "http://localhost:5001/identity/teams/membership");
  assert.equal(calledOptions.method, "DELETE");
  assert.equal(calledOptions.headers.Authorization, "Bearer token");
  assert.equal(result.ok, true);
  assert.deepEqual(result.data, payload);
});

test("submitLeaveTeam should return no-active-team message on 404", async () => {
  const result = await submitLeaveTeam({
    apiBaseUrl: "http://localhost:5001",
    token: "token",
    fetchImpl: async () => ({ ok: false, status: 404 }),
  });

  assert.equal(result.ok, false);
  assert.equal(result.type, "notFound");
  assert.match(result.message, /No perteneces a ningun equipo activo/);
});

test("submitLeaveTeam should return leadership-transfer message on 409", async () => {
  const result = await submitLeaveTeam({
    apiBaseUrl: "http://localhost:5001",
    token: "token",
    fetchImpl: async () => ({ ok: false, status: 409 }),
  });

  assert.equal(result.ok, false);
  assert.equal(result.type, "leaderMustTransfer");
  assert.match(result.message, /Debes transferir el liderazgo/);
});

test("submitLeaveTeam should return retryable network error when fetch throws", async () => {
  const result = await submitLeaveTeam({
    apiBaseUrl: "http://localhost:5001",
    token: "token",
    fetchImpl: async () => {
      throw new Error("network down");
    },
  });

  assert.equal(result.ok, false);
  assert.equal(result.type, "network");
  assert.match(result.message, /No se pudo conectar con el servidor/);
});
