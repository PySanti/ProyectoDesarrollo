import test from "node:test";
import assert from "node:assert/strict";
import { submitDeleteTeam } from "../src/features/teams/deleteTeamFlow.js";

test("submitDeleteTeam should call DELETE mine endpoint and return ok on 204", async () => {
  let calledUrl;
  let calledOptions;

  const result = await submitDeleteTeam({
    apiBaseUrl: "http://localhost:5001",
    token: "token",
    fetchImpl: async (url, options) => {
      calledUrl = url;
      calledOptions = options;
      return { ok: true, status: 204 };
    },
  });

  assert.equal(calledUrl, "http://localhost:5001/identity/teams/mine");
  assert.equal(calledOptions.method, "DELETE");
  assert.equal(calledOptions.headers.Authorization, "Bearer token");
  assert.equal(result.ok, true);
});

test("submitDeleteTeam should return no-active-team message on 404", async () => {
  const result = await submitDeleteTeam({
    apiBaseUrl: "http://localhost:5001",
    token: "token",
    fetchImpl: async () => ({ ok: false, status: 404 }),
  });

  assert.equal(result.ok, false);
  assert.equal(result.type, "notFound");
  assert.match(result.message, /No perteneces a ningún equipo activo/);
});

test("submitDeleteTeam should return activeParticipation message on 409", async () => {
  const result = await submitDeleteTeam({
    apiBaseUrl: "http://localhost:5001",
    token: "token",
    fetchImpl: async () => ({ ok: false, status: 409 }),
  });

  assert.equal(result.ok, false);
  assert.equal(result.type, "activeParticipation");
  assert.match(result.message, /partida activa/i);
});

test("submitDeleteTeam should return unauthorized message on 401", async () => {
  const result = await submitDeleteTeam({
    apiBaseUrl: "http://localhost:5001",
    token: "token",
    fetchImpl: async () => ({ ok: false, status: 401 }),
  });

  assert.equal(result.ok, false);
  assert.equal(result.type, "unauthorized");
  assert.match(result.message, /Sesión expirada/);
});

test("submitDeleteTeam should return forbidden message on 403", async () => {
  const result = await submitDeleteTeam({
    apiBaseUrl: "http://localhost:5001",
    token: "token",
    fetchImpl: async () => ({ ok: false, status: 403 }),
  });

  assert.equal(result.ok, false);
  assert.equal(result.type, "forbidden");
  assert.match(result.message, /Solo el líder puede eliminar el equipo/);
});

test("submitDeleteTeam should return retryable network error when fetch throws", async () => {
  const result = await submitDeleteTeam({
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
