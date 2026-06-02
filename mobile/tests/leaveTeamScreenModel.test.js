import test from "node:test";
import assert from "node:assert/strict";
import { submitLeaveTeamFromScreen } from "../src/features/teams/leaveTeamScreenModel.js";

function createStateSpies() {
  const loadingCalls = [];
  const errorCalls = [];
  const successCalls = [];
  const hasActiveTeamCalls = [];

  return {
    loadingCalls,
    errorCalls,
    successCalls,
    hasActiveTeamCalls,
    setLoading: (value) => loadingCalls.push(value),
    setErrorMessage: (value) => errorCalls.push(value),
    setSuccessMessage: (value) => successCalls.push(value),
    setHasActiveTeam: (value) => hasActiveTeamCalls.push(value),
  };
}

test("submitLeaveTeamFromScreen should toggle loading and set success on happy path", async () => {
  const spies = createStateSpies();
  const leftPayload = { equipoId: "team-1", resultado: "SalioDelEquipo" };
  let onLeftData;

  await submitLeaveTeamFromScreen({
    apiBaseUrl: "http://localhost:7000",
    token: "token",
    submitFn: async () => ({ ok: true, data: leftPayload }),
    onLeft: (data) => {
      onLeftData = data;
    },
    setLoading: spies.setLoading,
    setErrorMessage: spies.setErrorMessage,
    setSuccessMessage: spies.setSuccessMessage,
    setHasActiveTeam: spies.setHasActiveTeam,
  });

  assert.deepEqual(spies.loadingCalls, [true, false]);
  assert.deepEqual(spies.errorCalls, [null]);
  assert.deepEqual(spies.successCalls, [null, "Saliste del equipo con exito. Ya no perteneces a ningun equipo activo."]);
  assert.deepEqual(spies.hasActiveTeamCalls, [false]);
  assert.deepEqual(onLeftData, leftPayload);
});

test("submitLeaveTeamFromScreen should render no active team error", async () => {
  const spies = createStateSpies();

  await submitLeaveTeamFromScreen({
    apiBaseUrl: "http://localhost:7000",
    token: "token",
    submitFn: async () => ({ ok: false, message: "No perteneces a ningun equipo activo." }),
    setLoading: spies.setLoading,
    setErrorMessage: spies.setErrorMessage,
    setSuccessMessage: spies.setSuccessMessage,
  });

  assert.deepEqual(spies.loadingCalls, [true, false]);
  assert.deepEqual(spies.errorCalls, [null, "No perteneces a ningun equipo activo."]);
  assert.deepEqual(spies.successCalls, [null]);
});

test("submitLeaveTeamFromScreen should render leader transfer error", async () => {
  const spies = createStateSpies();

  await submitLeaveTeamFromScreen({
    apiBaseUrl: "http://localhost:7000",
    token: "token",
    submitFn: async () => ({ ok: false, message: "Debes transferir el liderazgo antes de salir del equipo." }),
    setLoading: spies.setLoading,
    setErrorMessage: spies.setErrorMessage,
    setSuccessMessage: spies.setSuccessMessage,
  });

  assert.deepEqual(spies.loadingCalls, [true, false]);
  assert.deepEqual(spies.errorCalls, [null, "Debes transferir el liderazgo antes de salir del equipo."]);
  assert.deepEqual(spies.successCalls, [null]);
});
