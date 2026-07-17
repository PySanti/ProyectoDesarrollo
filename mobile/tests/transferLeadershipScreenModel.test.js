import test from "node:test";
import assert from "node:assert/strict";
import {
  getTransferLeadershipSuccessMessage,
  submitTransferLeadershipFromScreen,
} from "../src/features/teams/transferLeadershipScreenModel.js";

function createStateSpies() {
  const loadingCalls = [];
  const errorCalls = [];
  const successCalls = [];

  return {
    loadingCalls,
    errorCalls,
    successCalls,
    setLoading: (value) => loadingCalls.push(value),
    setErrorMessage: (value) => errorCalls.push(value),
    setSuccessMessage: (value) => successCalls.push(value),
  };
}

test("getTransferLeadershipSuccessMessage should return the exact success copy", () => {
  assert.equal(
    getTransferLeadershipSuccessMessage(),
    "Liderazgo transferido correctamente. Ahora puedes salir del equipo si lo deseas."
  );
});

test("submitTransferLeadershipFromScreen should toggle loading, set success and return the result on happy path", async () => {
  const spies = createStateSpies();
  const transferPayload = { equipoId: "team-1", nuevoLiderUserId: "user-2" };
  let onTransferredData;

  const result = await submitTransferLeadershipFromScreen({
    apiBaseUrl: "http://localhost:7000",
    token: "token",
    nuevoLiderUserId: "user-2",
    submitFn: async () => ({ ok: true, data: transferPayload }),
    onTransferred: (data) => {
      onTransferredData = data;
    },
    setLoading: spies.setLoading,
    setErrorMessage: spies.setErrorMessage,
    setSuccessMessage: spies.setSuccessMessage,
  });

  assert.deepEqual(spies.loadingCalls, [true, false]);
  assert.deepEqual(spies.errorCalls, [null]);
  assert.deepEqual(spies.successCalls, [
    null,
    "Liderazgo transferido correctamente. Ahora puedes salir del equipo si lo deseas.",
  ]);
  assert.deepEqual(onTransferredData, transferPayload);
  assert.deepEqual(result, { ok: true, data: transferPayload });
});

test("submitTransferLeadershipFromScreen should render the failure error and return it", async () => {
  const spies = createStateSpies();
  const failureResult = {
    ok: false,
    type: "conflict",
    message:
      "No se pudo transferir el liderazgo. Verifica que seas lider y que el nuevo lider pertenezca al equipo.",
  };

  const result = await submitTransferLeadershipFromScreen({
    apiBaseUrl: "http://localhost:7000",
    token: "token",
    nuevoLiderUserId: "user-2",
    submitFn: async () => failureResult,
    setLoading: spies.setLoading,
    setErrorMessage: spies.setErrorMessage,
    setSuccessMessage: spies.setSuccessMessage,
  });

  assert.deepEqual(spies.loadingCalls, [true, false]);
  assert.deepEqual(spies.errorCalls, [null, failureResult.message]);
  assert.deepEqual(spies.successCalls, [null]);
  assert.deepEqual(result, failureResult);
});

test("submitTransferLeadershipFromScreen should render a generic error and return it when submitFn throws", async () => {
  const spies = createStateSpies();

  const result = await submitTransferLeadershipFromScreen({
    apiBaseUrl: "http://localhost:7000",
    token: "token",
    nuevoLiderUserId: "user-2",
    submitFn: async () => {
      throw new Error("boom");
    },
    setLoading: spies.setLoading,
    setErrorMessage: spies.setErrorMessage,
    setSuccessMessage: spies.setSuccessMessage,
  });

  assert.deepEqual(spies.loadingCalls, [true, false]);
  assert.deepEqual(spies.errorCalls, [null, "Ocurrio un error inesperado. Intenta nuevamente."]);
  assert.deepEqual(spies.successCalls, [null]);
  assert.deepEqual(result, { ok: false, message: "Ocurrio un error inesperado. Intenta nuevamente." });
});
