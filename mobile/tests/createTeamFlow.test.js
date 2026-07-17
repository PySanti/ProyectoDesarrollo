import test from "node:test";
import assert from "node:assert/strict";
import { submitCreateTeam } from "../src/features/teams/createTeamFlow.js";

test("submitCreateTeam should fail validation when name is empty", async () => {
  const result = await submitCreateTeam({
    apiBaseUrl: "http://localhost:5001",
    token: "token",
    teamName: "   ",
    fetchImpl: async () => ({ ok: true, status: 201, json: async () => ({}) }),
  });

  assert.equal(result.ok, false);
  assert.equal(result.type, "validation");
});

test("submitCreateTeam should fail validation when name has no letters", async () => {
  const result = await submitCreateTeam({
    apiBaseUrl: "http://localhost:5001",
    token: "token",
    teamName: "****",
    fetchImpl: async () => ({ ok: true, status: 201, json: async () => ({}) }),
  });

  assert.equal(result.ok, false);
  assert.equal(result.type, "validation");
  assert.match(result.message, /al menos una letra/i);
});

test("submitCreateTeam should return conflict message on 409", async () => {
  const result = await submitCreateTeam({
    apiBaseUrl: "http://localhost:5001",
    token: "token",
    teamName: "Exploradores",
    fetchImpl: async () => ({ ok: false, status: 409 }),
  });

  assert.equal(result.ok, false);
  assert.equal(result.type, "conflict");
  assert.match(result.message, /Ya perteneces a un equipo activo/);
});

test("submitCreateTeam should return created payload on success", async () => {
  const payload = {
    equipoId: "00000000-0000-0000-0000-000000000001",
    nombreEquipo: "Exploradores",
    estado: "Activo",
    liderUserId: "00000000-0000-0000-0000-000000000001",
    integrantes: [{ userId: "00000000-0000-0000-0000-000000000001", esLider: true }],
  };

  const result = await submitCreateTeam({
    apiBaseUrl: "http://localhost:5001",
    token: "token",
    teamName: "Exploradores",
    fetchImpl: async () => ({ ok: true, status: 201, json: async () => payload }),
  });

  assert.equal(result.ok, true);
  assert.deepEqual(result.data, payload);
});

test("submitCreateTeam should return retryable network error when fetch throws", async () => {
  const result = await submitCreateTeam({
    apiBaseUrl: "http://localhost:5001",
    token: "token",
    teamName: "Exploradores",
    fetchImpl: async () => {
      throw new Error("network down");
    },
  });

  assert.equal(result.ok, false);
  assert.equal(result.type, "network");
  assert.match(result.message, /No se pudo conectar con el servidor/);
});
