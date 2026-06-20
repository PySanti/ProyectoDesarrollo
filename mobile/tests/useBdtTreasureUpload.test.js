import test from "node:test";
import assert from "node:assert/strict";
import React from "react";
import TestRenderer from "react-test-renderer";
import { useBdtTreasureUpload } from "../src/features/bdt/useBdtTreasureUpload.js";

const { act, create } = TestRenderer;

/** Arnés que ejecuta el hook y expone su valor de retorno para aserciones de comportamiento. */
function renderHook(props) {
  const box = { latest: null };
  function Harness(harnessProps) {
    box.latest = useBdtTreasureUpload(harnessProps);
    return null;
  }
  create(React.createElement(Harness, props));
  return box;
}

test("useBdtTreasureUpload selecciona imagen y la sube al backend", async () => {
  const appended = [];
  let uploadedUrl = null;
  const fetchImpl = async (url) => {
    uploadedUrl = url;
    return uploadResponse("Decodificado", "QR-1");
  };
  const formDataFactory = () => ({ append: (...args) => appended.push(args) });

  let box;
  await act(async () => {
    box = renderHook({
      apiBaseUrl: "https://api.test",
      token: "token",
      partidaId: "partida-1",
      etapaId: "etapa-1",
      requestGeolocationPermission: async () => ({ granted: true }),
      requestImagePermission: async () => ({ granted: true }),
      pickImage: async () => ({ image: { uri: "file://qr.jpg", name: "qr.jpg", type: "image/jpeg", size: 100 } }),
      fetchImpl,
      formDataFactory,
    });
  });

  await act(async () => {
    await box.latest.selectImage();
  });
  assert.equal(box.latest.selectedImage.name, "qr.jpg");

  await act(async () => {
    await box.latest.submit();
  });

  assert.match(box.latest.successMessage, /Tesoro recibido/);
  assert.equal(box.latest.uploadResult.estadoProcesamiento, "Decodificado");
  assert.equal(uploadedUrl, "https://api.test/api/bdt/games/partida-1/stages/etapa-1/treasures");
  assert.equal(appended[0][0], "image");
});

test("useBdtTreasureUpload bloquea la subida si se niega el permiso de imagen", async () => {
  let fetchCalls = 0;
  let box;
  await act(async () => {
    box = renderHook({
      apiBaseUrl: "https://api.test",
      token: "token",
      partidaId: "partida-1",
      etapaId: "etapa-1",
      requestGeolocationPermission: async () => ({ granted: true }),
      requestImagePermission: async () => ({ granted: false }),
      pickImage: async () => ({ image: { uri: "file://qr.jpg", name: "qr.jpg", type: "image/jpeg", size: 100 } }),
      fetchImpl: async () => {
        fetchCalls++;
        return uploadResponse();
      },
    });
  });

  await act(async () => {
    await box.latest.selectImage();
  });

  assert.equal(box.latest.imagePermissionDenied, true);
  assert.equal(box.latest.canSubmit, false);
  assert.equal(fetchCalls, 0);
});

test("useBdtTreasureUpload bloquea la subida activa si se niega la geolocalización", async () => {
  let box;
  await act(async () => {
    box = renderHook({
      apiBaseUrl: "https://api.test",
      token: "token",
      partidaId: "partida-1",
      etapaId: "etapa-1",
      requestGeolocationPermission: async () => ({ granted: false }),
      requestImagePermission: async () => ({ granted: true }),
    });
  });

  assert.equal(box.latest.geolocationDenied, true);
  assert.equal(box.latest.canSubmit, false);
});

test("useBdtTreasureUpload habilita la subida cruzando los límites de adaptadores en runtime", async () => {
  let fetchCalls = 0;
  let imagePermissionCalls = 0;
  let geolocationPermissionCalls = 0;
  let pickCalls = 0;
  let box;

  await act(async () => {
    box = renderHook({
      apiBaseUrl: "https://api.test",
      token: "token",
      partidaId: "partida-1",
      etapaId: "etapa-1",
      requestGeolocationPermission: async () => {
        geolocationPermissionCalls++;
        return { granted: true };
      },
      requestImagePermission: async () => {
        imagePermissionCalls++;
        return { granted: true };
      },
      pickImage: async () => {
        pickCalls++;
        return { image: { uri: "file://qr.png", name: "qr.png", type: "image/png", size: 100 } };
      },
      fetchImpl: async () => {
        fetchCalls++;
        return uploadResponse("NoLegible", null);
      },
      formDataFactory: () => ({ append: () => undefined }),
    });
  });

  await act(async () => {
    await box.latest.selectImage();
  });
  await act(async () => {
    await box.latest.submit();
  });

  assert.equal(geolocationPermissionCalls, 1);
  assert.equal(imagePermissionCalls, 1);
  assert.equal(pickCalls, 1);
  assert.equal(fetchCalls, 1);
  assert.equal(box.latest.uploadResult.estadoProcesamiento, "NoLegible");
});

test("useBdtTreasureUpload muestra errores de validación de imagen", async () => {
  let box;
  await act(async () => {
    box = renderHook({
      apiBaseUrl: "https://api.test",
      token: "token",
      partidaId: "partida-1",
      etapaId: "etapa-1",
      requestGeolocationPermission: async () => ({ granted: true }),
      requestImagePermission: async () => ({ granted: true }),
      pickImage: async () => ({ image: { uri: "file://qr.gif", name: "qr.gif", type: "image/gif", size: 100 } }),
    });
  });

  await act(async () => {
    await box.latest.selectImage();
  });

  assert.match(box.latest.errorMessage, /JPEG o PNG/);
  assert.equal(box.latest.selectedImage, null);
});

function uploadResponse(estadoProcesamiento = "NoLegible", qrDecodificado = null) {
  return new Response(
    JSON.stringify({
      tesoroId: "tesoro-1",
      partidaId: "partida-1",
      etapaId: "etapa-1",
      exploradorId: "explorador-1",
      fechaEnvioUtc: "2026-01-01T00:03:00Z",
      estadoProcesamiento,
      qrDecodificado,
      mensaje: "Tesoro recibido para validacion.",
    }),
    { status: 201, headers: { "Content-Type": "application/json" } },
  );
}
