import test from "node:test";
import assert from "node:assert/strict";
import { submitJoinTeamFromScreen } from "../src/features/teams/joinTeamScreenModel.js";

function createStateSpies() {
  const loadingCalls = [];
  const errorCalls = [];
  const successCalls = [];
  const accessCodeCalls = [];

  return {
    loadingCalls,
    errorCalls,
    successCalls,
    accessCodeCalls,
    setLoading: (value) => loadingCalls.push(value),
    setErrorMessage: (value) => errorCalls.push(value),
    setSuccessMessage: (value) => successCalls.push(value),
    setAccessCode: (value) => accessCodeCalls.push(value),
  };
}

test("submitJoinTeamFromScreen should toggle loading and set success on happy path", async () => {
  const spies = createStateSpies();
  const joinedPayload = { equipoId: "team-1" };
  let onJoinedData;

  await submitJoinTeamFromScreen({
    apiBaseUrl: "http://localhost:7000",
    token: "token",
    accessCode: "ABCD1234",
    submitFn: async () => ({ ok: true, data: joinedPayload }),
    onJoined: (data) => {
      onJoinedData = data;
    },
    setLoading: spies.setLoading,
    setErrorMessage: spies.setErrorMessage,
    setSuccessMessage: spies.setSuccessMessage,
    setAccessCode: spies.setAccessCode,
  });

  assert.deepEqual(spies.loadingCalls, [true, false]);
  assert.deepEqual(spies.errorCalls, [null]);
  assert.deepEqual(spies.successCalls, [null, "Te uniste al equipo con exito."]);
  assert.deepEqual(spies.accessCodeCalls, [""]);
  assert.deepEqual(onJoinedData, joinedPayload);
});

test("submitJoinTeamFromScreen should render business error after submit", async () => {
  const spies = createStateSpies();

  await submitJoinTeamFromScreen({
    apiBaseUrl: "http://localhost:7000",
    token: "token",
    accessCode: "MISSING99",
    submitFn: async () => ({ ok: false, message: "El codigo ingresado no corresponde a un equipo activo." }),
    setLoading: spies.setLoading,
    setErrorMessage: spies.setErrorMessage,
    setSuccessMessage: spies.setSuccessMessage,
    setAccessCode: spies.setAccessCode,
  });

  assert.deepEqual(spies.loadingCalls, [true, false]);
  assert.deepEqual(spies.errorCalls, [null, "El codigo ingresado no corresponde a un equipo activo."]);
  assert.deepEqual(spies.successCalls, [null]);
  assert.deepEqual(spies.accessCodeCalls, []);
});

test("submitJoinTeamFromScreen should stop loading and set unexpected error when submit throws", async () => {
  const spies = createStateSpies();

  await submitJoinTeamFromScreen({
    apiBaseUrl: "http://localhost:7000",
    token: "token",
    accessCode: "ABCD1234",
    submitFn: async () => {
      throw new Error("boom");
    },
    setLoading: spies.setLoading,
    setErrorMessage: spies.setErrorMessage,
    setSuccessMessage: spies.setSuccessMessage,
    setAccessCode: spies.setAccessCode,
  });

  assert.deepEqual(spies.loadingCalls, [true, false]);
  assert.deepEqual(spies.errorCalls, [null, "Ocurrio un error inesperado. Intenta nuevamente."]);
  assert.deepEqual(spies.successCalls, [null]);
  assert.deepEqual(spies.accessCodeCalls, []);
});
