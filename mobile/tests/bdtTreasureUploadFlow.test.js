import test from "node:test";
import assert from "node:assert/strict";
import { submitTreasureUpload, validateTreasureImage } from "../src/features/bdt/bdtTreasureUploadFlow.js";

test("validateTreasureImage accepts JPEG and PNG up to 5 MB", () => {
  assert.equal(validateTreasureImage({ uri: "file://qr.jpg", name: "qr.jpg", type: "image/jpeg", size: 1024 }).ok, true);
  assert.equal(validateTreasureImage({ uri: "file://qr.png", name: "qr.png", type: "image/png", size: 1024 }).ok, true);
});

test("validateTreasureImage rejects missing image, unsupported type and oversize image", () => {
  assert.equal(validateTreasureImage(null).ok, false);
  assert.match(validateTreasureImage({ uri: "file://qr.gif", name: "qr.gif", type: "image/gif", size: 100 }).message, /JPEG o PNG/);
  assert.match(validateTreasureImage({ uri: "file://qr.jpg", name: "qr.jpg", type: "image/jpeg", size: 6 * 1024 * 1024 }).message, /5 MB/);
});

test("submitTreasureUpload sends multipart image to HU-45 endpoint", async () => {
  const appended = [];
  const requested = [];
  const formDataFactory = () => ({ append: (...args) => appended.push(args) });
  const fetchImpl = async (url, options) => {
    requested.push({ url, options });
    return new Response(
      JSON.stringify({
        tesoroId: "tesoro-1",
        partidaId: "partida-1",
        etapaId: "etapa-1",
        exploradorId: "explorador-1",
        fechaEnvioUtc: "2026-01-01T00:03:00Z",
        estadoProcesamiento: "Decodificado",
        qrDecodificado: "QR-1",
        mensaje: "Tesoro recibido para validacion.",
      }),
      { status: 201, headers: { "Content-Type": "application/json" } },
    );
  };

  const result = await submitTreasureUpload({
    apiBaseUrl: "https://api.test",
    token: "token",
    partidaId: "partida-1",
    etapaId: "etapa-1",
    image: { uri: "file://qr.jpg", name: "qr.jpg", type: "image/jpeg", size: 100 },
    fetchImpl,
    formDataFactory,
  });

  assert.equal(result.ok, true);
  assert.equal(requested[0].url, "https://api.test/api/bdt/games/partida-1/stages/etapa-1/treasures");
  assert.equal(requested[0].options.method, "POST");
  assert.equal(requested[0].options.headers.Authorization, "Bearer token");
  assert.equal(appended[0][0], "image");
  assert.deepEqual(appended[0][1], { uri: "file://qr.jpg", name: "qr.jpg", type: "image/jpeg" });
});

test("submitTreasureUpload maps image constraint errors", async () => {
  const formDataFactory = () => ({ append: () => undefined });
  const unsupported = await submitTreasureUpload({
    apiBaseUrl: "https://api.test",
    token: "token",
    partidaId: "partida-1",
    etapaId: "etapa-1",
    image: { uri: "file://qr.jpg", name: "qr.jpg", type: "image/jpeg", size: 100 },
    fetchImpl: async () => new Response("", { status: 415 }),
    formDataFactory,
  });
  const tooLarge = await submitTreasureUpload({
    apiBaseUrl: "https://api.test",
    token: "token",
    partidaId: "partida-1",
    etapaId: "etapa-1",
    image: { uri: "file://qr.jpg", name: "qr.jpg", type: "image/jpeg", size: 100 },
    fetchImpl: async () => new Response("", { status: 413 }),
    formDataFactory,
  });

  assert.equal(unsupported.type, "unsupportedType");
  assert.equal(tooLarge.type, "tooLarge");
});
