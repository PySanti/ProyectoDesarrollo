import test from "node:test";
import assert from "node:assert/strict";
import {
  getEligibleLeaderMembers,
  submitTransferLeadership,
  validateNewLeaderUserId,
} from "../src/features/teams/transferLeadershipFlow.js";

const targetUserId = "22222222-2222-2222-2222-222222222222";

test("validateNewLeaderUserId should reject invalid ids", () => {
  const result = validateNewLeaderUserId("not-a-guid");

  assert.equal(result.ok, false);
  assert.equal(result.type, "validation");
});

test("getEligibleLeaderMembers should exclude current leader", () => {
  const members = [
    { userId: "11111111-1111-1111-1111-111111111111", nombre: "Lider", esLider: true },
    { userId: targetUserId, nombre: "Nuevo lider", esLider: false },
  ];

  const result = getEligibleLeaderMembers(members, "11111111-1111-1111-1111-111111111111");

  assert.equal(result.length, 1);
  assert.equal(result[0].userId, targetUserId);
});

test("submitTransferLeadership should call PATCH leadership endpoint", async () => {
  let requestedUrl;
  let requestedBody;
  const result = await submitTransferLeadership({
    apiBaseUrl: "https://api.test",
    token: "token",
    nuevoLiderUserId: targetUserId,
    fetchImpl: async (url, options) => {
      requestedUrl = url;
      requestedBody = options.body;
      return {
        ok: true,
        status: 200,
        json: async () => ({
          equipoId: "33333333-3333-3333-3333-333333333333",
          liderAnteriorUserId: "11111111-1111-1111-1111-111111111111",
          nuevoLiderUserId: targetUserId,
          equipoEstado: "Activo",
        }),
      };
    },
  });

  assert.equal(requestedUrl, "https://api.test/identity/teams/leadership");
  assert.deepEqual(JSON.parse(requestedBody), { nuevoLiderUserId: targetUserId });
  assert.equal(result.ok, true);
  assert.equal(result.data.nuevoLiderUserId, targetUserId);
});

test("submitTransferLeadership should map 404 and 409 errors", async () => {
  const notFound = await submitTransferLeadership({
    apiBaseUrl: "https://api.test",
    token: "token",
    nuevoLiderUserId: targetUserId,
    fetchImpl: async () => ({ ok: false, status: 404 }),
  });
  const conflict = await submitTransferLeadership({
    apiBaseUrl: "https://api.test",
    token: "token",
    nuevoLiderUserId: targetUserId,
    fetchImpl: async () => ({ ok: false, status: 409 }),
  });

  assert.equal(notFound.type, "notFound");
  assert.equal(conflict.type, "conflict");
});
