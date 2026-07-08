import test from "node:test";
import assert from "node:assert/strict";
import {
  loadInvitations,
  acceptInvitation,
  rejectInvitation,
} from "../src/features/teams/invitationsApi.js";

const API_BASE = "https://api.test";
const TOKEN = "test-token";
const INV_ID = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

// ── loadInvitations ──────────────────────────────────────────────────────────

test("loadInvitations should call GET /identity/teams/invitations with Bearer token", async () => {
  let requestedUrl;
  let requestedHeaders;

  const result = await loadInvitations(API_BASE, TOKEN, async (url, options) => {
    requestedUrl = url;
    requestedHeaders = options.headers;
    return {
      ok: true,
      status: 200,
      json: async () => [
        {
          invitacionId: INV_ID,
          equipoId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
          nombreEquipo: "Exploradores",
          liderUserId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
          estado: "Pendiente",
        },
      ],
    };
  });

  assert.equal(requestedUrl, `${API_BASE}/identity/teams/invitations`);
  assert.equal(requestedHeaders["Authorization"], `Bearer ${TOKEN}`);
  assert.equal(result.ok, true);
  assert.equal(result.data.length, 1);
  assert.equal(result.data[0].invitacionId, INV_ID);
});

test("loadInvitations should return network error when fetch throws", async () => {
  const result = await loadInvitations(API_BASE, TOKEN, async () => {
    throw new Error("network down");
  });

  assert.equal(result.ok, false);
  assert.equal(result.type, "network");
  assert.match(result.message, /No se pudo conectar con el servidor/);
});

test("loadInvitations should return unauthorized on 401", async () => {
  const result = await loadInvitations(API_BASE, TOKEN, async () => ({
    ok: false,
    status: 401,
  }));

  assert.equal(result.ok, false);
  assert.equal(result.type, "unauthorized");
});

test("loadInvitations should return error on unexpected status", async () => {
  const result = await loadInvitations(API_BASE, TOKEN, async () => ({
    ok: false,
    status: 500,
  }));

  assert.equal(result.ok, false);
  assert.equal(result.type, "error");
});

// ── acceptInvitation ─────────────────────────────────────────────────────────

test("acceptInvitation should call POST /identity/teams/invitations/{id}/acceptance", async () => {
  let requestedUrl;
  let requestedMethod;

  const result = await acceptInvitation(API_BASE, TOKEN, INV_ID, async (url, options) => {
    requestedUrl = url;
    requestedMethod = options.method;
    return {
      ok: true,
      status: 200,
      json: async () => ({
        invitacionId: INV_ID,
        equipoId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
        estado: "Aceptada",
      }),
    };
  });

  assert.equal(requestedUrl, `${API_BASE}/identity/teams/invitations/${INV_ID}/acceptance`);
  assert.equal(requestedMethod, "POST");
  assert.equal(result.ok, true);
  assert.equal(result.data.estado, "Aceptada");
});

test("acceptInvitation should return conflict on 409", async () => {
  const result = await acceptInvitation(API_BASE, TOKEN, INV_ID, async () => ({
    ok: false,
    status: 409,
  }));

  assert.equal(result.ok, false);
  assert.equal(result.type, "conflict");
});

test("acceptInvitation should return notFound on 404", async () => {
  const result = await acceptInvitation(API_BASE, TOKEN, INV_ID, async () => ({
    ok: false,
    status: 404,
  }));

  assert.equal(result.ok, false);
  assert.equal(result.type, "notFound");
});

test("acceptInvitation should return network error when fetch throws", async () => {
  const result = await acceptInvitation(API_BASE, TOKEN, INV_ID, async () => {
    throw new Error("network down");
  });

  assert.equal(result.ok, false);
  assert.equal(result.type, "network");
});

// ── rejectInvitation ─────────────────────────────────────────────────────────

test("rejectInvitation should call POST /identity/teams/invitations/{id}/rejection", async () => {
  let requestedUrl;
  let requestedMethod;

  const result = await rejectInvitation(API_BASE, TOKEN, INV_ID, async (url, options) => {
    requestedUrl = url;
    requestedMethod = options.method;
    return {
      ok: true,
      status: 200,
      json: async () => ({
        invitacionId: INV_ID,
        estado: "Rechazada",
      }),
    };
  });

  assert.equal(requestedUrl, `${API_BASE}/identity/teams/invitations/${INV_ID}/rejection`);
  assert.equal(requestedMethod, "POST");
  assert.equal(result.ok, true);
  assert.equal(result.data.estado, "Rechazada");
});

test("rejectInvitation should return notFound on 404", async () => {
  const result = await rejectInvitation(API_BASE, TOKEN, INV_ID, async () => ({
    ok: false,
    status: 404,
  }));

  assert.equal(result.ok, false);
  assert.equal(result.type, "notFound");
});

test("rejectInvitation should return network error when fetch throws", async () => {
  const result = await rejectInvitation(API_BASE, TOKEN, INV_ID, async () => {
    throw new Error("network down");
  });

  assert.equal(result.ok, false);
  assert.equal(result.type, "network");
});
