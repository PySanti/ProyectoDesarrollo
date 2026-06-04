import test from "node:test";
import assert from "node:assert/strict";
import { calculateRemainingSeconds, loadActiveBdtStage } from "../src/features/bdt/bdtActiveStageFlow.js";

test("loadActiveBdtStage calls HU-44 active-stage endpoint", async () => {
  const requested = [];
  const fetchImpl = async (url, options) => {
    requested.push({ url, options });
    return new Response(
      JSON.stringify({
        partidaId: "partida-1",
        nombre: "Ruta QR",
        estado: "Iniciada",
        modalidad: "Individual",
        exploradorId: "explorador-1",
        etapaActiva: {
          etapaId: "etapa-1",
          orden: 1,
          estado: "Activa",
          tiempoLimiteSegundos: 300,
          iniciadaEnUtc: "2026-01-01T00:00:00Z",
          cierraEnUtc: "2026-01-01T00:05:00Z",
        },
        puedeSubirTesoro: true,
        requiereGeolocalizacion: true,
        mensaje: "Etapa activa disponible.",
      }),
      { status: 200, headers: { "Content-Type": "application/json" } },
    );
  };

  const result = await loadActiveBdtStage({
    apiBaseUrl: "https://api.test",
    token: "token",
    partidaId: "partida-1",
    fetchImpl,
  });

  assert.equal(result.ok, true);
  assert.equal(requested[0].url, "https://api.test/api/bdt/games/partida-1/active-stage");
  assert.equal(requested[0].options.method, "GET");
  assert.equal(requested[0].options.headers.Authorization, "Bearer token");
});

test("loadActiveBdtStage maps no-active-stage conflict to unavailable", async () => {
  const fetchImpl = async () => new Response(JSON.stringify({ message: "conflict" }), { status: 409 });

  const result = await loadActiveBdtStage({
    apiBaseUrl: "https://api.test",
    token: "token",
    partidaId: "partida-1",
    fetchImpl,
  });

  assert.equal(result.ok, false);
  assert.equal(result.type, "unavailable");
  assert.match(result.message, /etapa activa/i);
});

test("calculateRemainingSeconds uses backend close timestamp", () => {
  const remaining = calculateRemainingSeconds(
    "2026-01-01T00:05:00Z",
    new Date("2026-01-01T00:03:15Z"),
  );

  assert.equal(remaining, 105);
});
