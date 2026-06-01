import test from "node:test";
import assert from "node:assert/strict";
import { submitJoinTeamByCode } from "../src/features/teams/joinTeamFlow.js";

test("submitJoinTeamByCode should fail validation when access code is empty", async () => {
  const result = await submitJoinTeamByCode({
    apiBaseUrl: "http://localhost:5001",
    token: "token",
    accessCode: "   ",
    fetchImpl: async () => ({ ok: true, status: 200, json: async () => ({}) }),
  });

  assert.equal(result.ok, false);
  assert.equal(result.type, "validation");
});

test("submitJoinTeamByCode should send normalized code and return joined payload on success", async () => {
  const payload = {
    equipoId: "00000000-0000-0000-0000-000000000001",
    nombreEquipo: "Exploradores",
    codigoAcceso: "ABCD1234",
    estado: "Activo",
    liderUserId: "00000000-0000-0000-0000-000000000002",
    integrantes: [
      { userId: "00000000-0000-0000-0000-000000000002", esLider: true },
      { userId: "00000000-0000-0000-0000-000000000003", esLider: false },
    ],
  };
  let requestBody;

  const result = await submitJoinTeamByCode({
    apiBaseUrl: "http://localhost:5001",
    token: "token",
    accessCode: " abcd1234 ",
    fetchImpl: async (_url, options) => {
      requestBody = JSON.parse(options.body);
      return { ok: true, status: 200, json: async () => payload };
    },
  });

  assert.equal(requestBody.codigoAcceso, "ABCD1234");
  assert.equal(result.ok, true);
  assert.deepEqual(result.data, payload);
});

test("submitJoinTeamByCode should return invalid-code message on 404", async () => {
  const result = await submitJoinTeamByCode({
    apiBaseUrl: "http://localhost:5001",
    token: "token",
    accessCode: "MISSING99",
    fetchImpl: async () => ({ ok: false, status: 404 }),
  });

  assert.equal(result.ok, false);
  assert.equal(result.type, "notFound");
  assert.match(result.message, /no corresponde a un equipo activo/);
});

test("submitJoinTeamByCode should return conflict message on 409", async () => {
  const result = await submitJoinTeamByCode({
    apiBaseUrl: "http://localhost:5001",
    token: "token",
    accessCode: "ABCD1234",
    fetchImpl: async () => ({ ok: false, status: 409 }),
  });

  assert.equal(result.ok, false);
  assert.equal(result.type, "conflict");
  assert.match(result.message, /Ya perteneces a un equipo activo|equipo destino esta lleno/);
});

test("submitJoinTeamByCode should return retryable network error when fetch throws", async () => {
  const result = await submitJoinTeamByCode({
    apiBaseUrl: "http://localhost:5001",
    token: "token",
    accessCode: "ABCD1234",
    fetchImpl: async () => {
      throw new Error("network down");
    },
  });

  assert.equal(result.ok, false);
  assert.equal(result.type, "network");
  assert.match(result.message, /No se pudo conectar con el servidor/);
});
