import test from "node:test";
import assert from "node:assert/strict";
import React from "react";
import TestRenderer from "react-test-renderer";
import { BdtTreasureUploadScreenController } from "../src/features/bdt/BdtTreasureUploadScreenController.js";

const { act, create } = TestRenderer;

const components = {
  ActivityIndicator: "ActivityIndicator",
  Pressable: "Pressable",
  SafeAreaView: "SafeAreaView",
  ScrollView: "ScrollView",
  Text: "Text",
  View: "View",
};

test("BdtTreasureUploadScreenController selects image and uploads to backend", async () => {
  const appended = [];
  let uploadedUrl = null;
  const fetchImpl = async (url) => {
    uploadedUrl = url;
    return uploadResponse("Decodificado", "QR-1");
  };
  const formDataFactory = () => ({ append: (...args) => appended.push(args) });

  let renderer;
  await act(async () => {
    renderer = create(
      React.createElement(BdtTreasureUploadScreenController, {
        apiBaseUrl: "https://api.test",
        token: "token",
        partidaId: "partida-1",
        etapaId: "etapa-1",
        components,
        requestGeolocationPermission: async () => ({ granted: true }),
        requestImagePermission: async () => ({ granted: true }),
        pickImage: async () => ({ image: { uri: "file://qr.jpg", name: "qr.jpg", type: "image/jpeg", size: 100 } }),
        fetchImpl,
        formDataFactory,
      }),
    );
  });

  const buttons = () => renderer.root.findAllByType("Pressable");
  await act(async () => {
    buttons()[0].props.onPress();
  });

  let renderedText = renderer.root.findAllByType("Text").map((node) => node.props.children).join(" ");
  assert.match(renderedText, /Imagen seleccionada/);
  assert.match(renderedText, /qr.jpg/);

  await act(async () => {
    buttons()[1].props.onPress();
  });

  renderedText = renderer.root.findAllByType("Text").map((node) => node.props.children).join(" ");
  assert.match(renderedText, /Tesoro recibido/);
  assert.equal(uploadedUrl, "https://api.test/api/bdt/games/partida-1/stages/etapa-1/treasures");
  assert.equal(appended[0][0], "image");
});

test("BdtTreasureUploadScreenController blocks upload when image permission is denied", async () => {
  let fetchCalls = 0;
  let renderer;
  await act(async () => {
    renderer = create(
      React.createElement(BdtTreasureUploadScreenController, {
        apiBaseUrl: "https://api.test",
        token: "token",
        partidaId: "partida-1",
        etapaId: "etapa-1",
        components,
        requestGeolocationPermission: async () => ({ granted: true }),
        requestImagePermission: async () => ({ granted: false }),
        pickImage: async () => ({ image: { uri: "file://qr.jpg", name: "qr.jpg", type: "image/jpeg", size: 100 } }),
        fetchImpl: async () => {
          fetchCalls++;
          return uploadResponse();
        },
      }),
    );
  });

  await act(async () => {
    renderer.root.findAllByType("Pressable")[0].props.onPress();
  });

  const renderedText = renderer.root.findAllByType("Text").map((node) => node.props.children).join(" ");
  assert.match(renderedText, /permitir camara o imagenes/);
  assert.equal(fetchCalls, 0);
});

test("BdtTreasureUploadScreenController blocks active upload when geolocation is denied", async () => {
  let renderer;
  await act(async () => {
    renderer = create(
      React.createElement(BdtTreasureUploadScreenController, {
        apiBaseUrl: "https://api.test",
        token: "token",
        partidaId: "partida-1",
        etapaId: "etapa-1",
        components,
        requestGeolocationPermission: async () => ({ granted: false }),
        requestImagePermission: async () => ({ granted: true }),
      }),
    );
  });

  const renderedText = renderer.root.findAllByType("Text").map((node) => node.props.children).join(" ");
  assert.match(renderedText, /permitir geolocalizacion/);
});

test("BdtTreasureUploadScreenController enables upload through runtime adapter boundaries", async () => {
  let fetchCalls = 0;
  let imagePermissionCalls = 0;
  let geolocationPermissionCalls = 0;
  let pickCalls = 0;
  let renderer;

  await act(async () => {
    renderer = create(
      React.createElement(BdtTreasureUploadScreenController, {
        apiBaseUrl: "https://api.test",
        token: "token",
        partidaId: "partida-1",
        etapaId: "etapa-1",
        components,
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
      }),
    );
  });

  await act(async () => {
    renderer.root.findAllByType("Pressable")[0].props.onPress();
  });
  await act(async () => {
    renderer.root.findAllByType("Pressable")[1].props.onPress();
  });

  assert.equal(geolocationPermissionCalls, 1);
  assert.equal(imagePermissionCalls, 1);
  assert.equal(pickCalls, 1);
  assert.equal(fetchCalls, 1);
});

test("BdtTreasureUploadScreenController shows backend upload errors", async () => {
  let renderer;
  await act(async () => {
    renderer = create(
      React.createElement(BdtTreasureUploadScreenController, {
        apiBaseUrl: "https://api.test",
        token: "token",
        partidaId: "partida-1",
        etapaId: "etapa-1",
        components,
        requestGeolocationPermission: async () => ({ granted: true }),
        requestImagePermission: async () => ({ granted: true }),
        pickImage: async () => ({ image: { uri: "file://qr.gif", name: "qr.gif", type: "image/gif", size: 100 } }),
      }),
    );
  });

  await act(async () => {
    renderer.root.findAllByType("Pressable")[0].props.onPress();
  });

  const renderedText = renderer.root.findAllByType("Text").map((node) => node.props.children).join(" ");
  assert.match(renderedText, /JPEG o PNG/);
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
