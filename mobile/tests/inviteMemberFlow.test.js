import test from "node:test";
import assert from "node:assert/strict";
import {
  loadEligibleParticipants,
  sendInvitation,
} from "../src/features/teams/inviteMemberApi.js";

const API_BASE = "https://api.test";
const TOKEN = "test-token";
const INVITADO_USER_ID = "dddddddd-dddd-dddd-dddd-dddddddddddd";

// ── loadEligibleParticipants ──────────────────────────────────────────────────

test("loadEligibleParticipants should call GET /identity/teams/eligible-participants with Bearer token", async () => {
  let requestedUrl;
  let requestedHeaders;

  const result = await loadEligibleParticipants(API_BASE, TOKEN, async (url, options) => {
    requestedUrl = url;
    requestedHeaders = options.headers;
    return {
      ok: true,
      status: 200,
      json: async () => [
        { userId: INVITADO_USER_ID, nombre: "Maria Lopez", email: "maria@test.com" },
      ],
    };
  });

  assert.equal(requestedUrl, `${API_BASE}/identity/teams/eligible-participants`);
  assert.equal(requestedHeaders["Authorization"], `Bearer ${TOKEN}`);
  assert.equal(result.ok, true);
  assert.equal(result.data.length, 1);
  assert.equal(result.data[0].userId, INVITADO_USER_ID);
});

test("loadEligibleParticipants should return empty array when 200 with empty list", async () => {
  const result = await loadEligibleParticipants(API_BASE, TOKEN, async () => ({
    ok: true,
    status: 200,
    json: async () => [],
  }));

  assert.equal(result.ok, true);
  assert.equal(result.data.length, 0);
});

test("loadEligibleParticipants should return forbidden on 403", async () => {
  const result = await loadEligibleParticipants(API_BASE, TOKEN, async () => ({
    ok: false,
    status: 403,
  }));

  assert.equal(result.ok, false);
  assert.equal(result.type, "forbidden");
});

test("loadEligibleParticipants should return network error when fetch throws", async () => {
  const result = await loadEligibleParticipants(API_BASE, TOKEN, async () => {
    throw new Error("network down");
  });

  assert.equal(result.ok, false);
  assert.equal(result.type, "network");
  assert.match(result.message, /No se pudo conectar con el servidor/);
});

test("loadEligibleParticipants should return error on unexpected status", async () => {
  const result = await loadEligibleParticipants(API_BASE, TOKEN, async () => ({
    ok: false,
    status: 500,
  }));

  assert.equal(result.ok, false);
  assert.equal(result.type, "error");
});

// ── sendInvitation ────────────────────────────────────────────────────────────

test("sendInvitation should call POST /identity/teams/invitations with invitadoUserId in body", async () => {
  let requestedUrl;
  let requestedMethod;
  let requestedBody;
  let requestedHeaders;

  const result = await sendInvitation(API_BASE, TOKEN, INVITADO_USER_ID, async (url, options) => {
    requestedUrl = url;
    requestedMethod = options.method;
    requestedBody = JSON.parse(options.body);
    requestedHeaders = options.headers;
    return {
      ok: true,
      status: 201,
      json: async () => ({
        invitacionId: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
        equipoId: "ffffffff-ffff-ffff-ffff-ffffffffffff",
        invitadoUserId: INVITADO_USER_ID,
        estado: "Pendiente",
      }),
    };
  });

  assert.equal(requestedUrl, `${API_BASE}/identity/teams/invitations`);
  assert.equal(requestedMethod, "POST");
  assert.deepEqual(requestedBody, { invitadoUserId: INVITADO_USER_ID });
  assert.equal(requestedHeaders["Content-Type"], "application/json");
  assert.equal(requestedHeaders["Authorization"], `Bearer ${TOKEN}`);
  assert.equal(result.ok, true);
  assert.equal(result.data.invitadoUserId, INVITADO_USER_ID);
});

test("sendInvitation should return conflict on 409 (team full or user already in team)", async () => {
  const result = await sendInvitation(API_BASE, TOKEN, INVITADO_USER_ID, async () => ({
    ok: false,
    status: 409,
  }));

  assert.equal(result.ok, false);
  assert.equal(result.type, "conflict");
});

test("sendInvitation should map 409 code InvitacionPendienteYaExisteException to specific message", async () => {
  const result = await sendInvitation(API_BASE, TOKEN, INVITADO_USER_ID, async () => ({
    ok: false,
    status: 409,
    json: async () => ({ code: "InvitacionPendienteYaExisteException" }),
  }));

  assert.equal(result.ok, false);
  assert.equal(result.type, "conflict");
  assert.equal(result.message, "Ya hay una invitacion activa para este participante.");
});

test("sendInvitation should fall back to generic message on 409 with unknown/absent code", async () => {
  const result = await sendInvitation(API_BASE, TOKEN, INVITADO_USER_ID, async () => ({
    ok: false,
    status: 409,
    json: async () => ({}),
  }));

  assert.equal(result.ok, false);
  assert.equal(result.type, "conflict");
  assert.match(result.message, /El equipo puede estar lleno/);
});

test("sendInvitation should return forbidden on 403 (not leader)", async () => {
  const result = await sendInvitation(API_BASE, TOKEN, INVITADO_USER_ID, async () => ({
    ok: false,
    status: 403,
  }));

  assert.equal(result.ok, false);
  assert.equal(result.type, "forbidden");
});

test("sendInvitation should return notFound on 404", async () => {
  const result = await sendInvitation(API_BASE, TOKEN, INVITADO_USER_ID, async () => ({
    ok: false,
    status: 404,
  }));

  assert.equal(result.ok, false);
  assert.equal(result.type, "notFound");
});

test("sendInvitation should return network error when fetch throws", async () => {
  const result = await sendInvitation(API_BASE, TOKEN, INVITADO_USER_ID, async () => {
    throw new Error("network down");
  });

  assert.equal(result.ok, false);
  assert.equal(result.type, "network");
  assert.match(result.message, /No se pudo conectar con el servidor/);
});
