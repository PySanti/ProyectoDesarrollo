import test from "node:test";
import assert from "node:assert/strict";
import { rankingHubUrl } from "../src/features/partidas/rankingHub.js";

test("rankingHubUrl apunta al hub de puntuaciones via gateway sin doble slash", () => {
  assert.equal(rankingHubUrl("http://gw:5080/"), "http://gw:5080/puntuaciones/hubs/ranking");
  assert.equal(rankingHubUrl("http://gw:5080"), "http://gw:5080/puntuaciones/hubs/ranking");
});
