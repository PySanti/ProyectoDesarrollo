import test from "node:test";
import assert from "node:assert/strict";
import { pickBdtTreasureImage } from "../src/permissions/bdtTreasureImagePicker.js";

test("pickBdtTreasureImage pide base64 y lo expone en el resultado", async () => {
  const opciones = [];
  const fakePicker = {
    MediaTypeOptions: { Images: "Images" },
    launchImageLibraryAsync: async (opts) => {
      opciones.push(opts);
      return { canceled: false, assets: [{ uri: "file:///a/foto.jpg", base64: "QkFTRTY0", mimeType: "image/jpeg" }] };
    },
  };
  const r = await pickBdtTreasureImage(async () => fakePicker, "library");
  assert.equal(opciones[0].base64, true);
  assert.equal(r.image.base64, "QkFTRTY0");
  assert.equal(r.image.uri, "file:///a/foto.jpg");
});

test("pickBdtTreasureImage camera usa launchCameraAsync y cancelado devuelve cancelled", async () => {
  let usoCamara = false;
  const fakePicker = {
    MediaTypeOptions: { Images: "Images" },
    launchCameraAsync: async (opts) => {
      usoCamara = true;
      assert.equal(opts.base64, true);
      return { canceled: true };
    },
    launchImageLibraryAsync: async () => {
      throw new Error("no debía usar galería");
    },
  };
  const r = await pickBdtTreasureImage(async () => fakePicker, "camera");
  assert.equal(usoCamara, true);
  assert.equal(r.cancelled, true);
});
