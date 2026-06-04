import test from "node:test";
import assert from "node:assert/strict";
import { requestBdtGeolocationPermission } from "../src/permissions/bdtGeolocationPermission.js";

test("requestBdtGeolocationPermission maps granted Expo foreground permission", async () => {
  const result = await requestBdtGeolocationPermission(async () => ({
    requestForegroundPermissionsAsync: async () => ({ status: "granted", granted: true }),
  }));

  assert.deepEqual(result, { granted: true, unavailable: false });
});

test("requestBdtGeolocationPermission maps denied Expo foreground permission", async () => {
  const result = await requestBdtGeolocationPermission(async () => ({
    requestForegroundPermissionsAsync: async () => ({ status: "denied", granted: false }),
  }));

  assert.deepEqual(result, { granted: false, unavailable: false });
});

test("requestBdtGeolocationPermission maps unavailable module", async () => {
  const importFailure = await requestBdtGeolocationPermission(async () => {
    throw new Error("module unavailable");
  });
  const missingApi = await requestBdtGeolocationPermission(async () => ({}));

  assert.deepEqual(importFailure, { granted: false, unavailable: true });
  assert.deepEqual(missingApi, { granted: false, unavailable: true });
});
