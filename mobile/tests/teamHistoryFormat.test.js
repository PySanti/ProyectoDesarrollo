import test from "node:test";
import assert from "node:assert/strict";
import { formatFechaRegistro } from "../src/features/teams/teamHistoryFormat.js";

test("formatFechaRegistro formats an ISO UTC date to long Spanish date", () => {
  assert.equal(formatFechaRegistro("2026-07-08T00:00:00Z"), "8 de julio de 2026");
});

test("formatFechaRegistro uses UTC so midnight Z does not shift the day", () => {
  assert.equal(formatFechaRegistro("2026-01-01T00:00:00Z"), "1 de enero de 2026");
  assert.equal(formatFechaRegistro("2026-12-31T00:00:00Z"), "31 de diciembre de 2026");
});

test("formatFechaRegistro passes through an invalid date string unchanged", () => {
  assert.equal(formatFechaRegistro("no-es-fecha"), "no-es-fecha");
});

test("formatFechaRegistro returns empty string for empty or non-string input", () => {
  assert.equal(formatFechaRegistro(""), "");
  assert.equal(formatFechaRegistro(undefined), "");
  assert.equal(formatFechaRegistro(null), "");
});
