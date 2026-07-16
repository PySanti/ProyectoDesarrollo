import test from "node:test";
import assert from "node:assert/strict";
import {
  getEligibleLeaderMembers,
  submitTransferLeadership,
} from "../src/features/teams/transferLeadershipFlow.js";

const leaderUserId = "11111111-1111-1111-1111-111111111111";
const targetUserId = "22222222-2222-2222-2222-222222222222";

test("getEligibleLeaderMembers should exclude current leader", () => {
  const members = [
    { usuarioId: leaderUserId, nombre: "Lider", esLider: true },
    { usuarioId: targetUserId, nombre: "Nuevo lider", esLider: false },
  ];

  const result = getEligibleLeaderMembers(members, leaderUserId);

  assert.equal(result.length, 1);
  assert.equal(result[0].usuarioId, targetUserId);
});

test("getEligibleLeaderMembers should return empty when leader is the only member", () => {
  const members = [{ usuarioId: leaderUserId, nombre: "Lider", esLider: true }];

  const result = getEligibleLeaderMembers(members, leaderUserId);

  assert.equal(result.length, 0);
});

test("getEligibleLeaderMembers should exclude a leader whose id differs from currentLeaderUserId", () => {
  // Regression guard for the `esLider` clause: two members could satisfy the id-inequality
  // check alone, so this asserts esLider is what actually hides the real leader here.
  const members = [
    { usuarioId: targetUserId, nombre: "Otro lider", esLider: true },
    { usuarioId: "33333333-3333-3333-3333-333333333333", nombre: "Miembro", esLider: false },
  ];

  const result = getEligibleLeaderMembers(members, leaderUserId);

  assert.equal(result.length, 1);
  assert.equal(result[0].usuarioId, "33333333-3333-3333-3333-333333333333");
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
          liderAnteriorUserId: leaderUserId,
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

test("submitTransferLeadership should map HTTP statuses to the exact user-facing messages", async () => {
  const statusToMessage = {
    400: "Selecciona un nuevo lider valido.",
    401: "Sesion expirada o no autorizada.",
    403: "Debes tener rol Participante para transferir liderazgo.",
    404: "No perteneces a ningun equipo activo.",
    409: "No se pudo transferir el liderazgo. Verifica que seas lider y que el nuevo lider pertenezca al equipo.",
    500: "No se pudo transferir el liderazgo.",
  };

  for (const [status, message] of Object.entries(statusToMessage)) {
    const result = await submitTransferLeadership({
      apiBaseUrl: "https://api.test",
      token: "token",
      nuevoLiderUserId: targetUserId,
      fetchImpl: async () => ({ ok: false, status: Number(status) }),
    });

    assert.equal(result.message, message, `status ${status}`);
  }
});
