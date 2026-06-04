import test from "node:test";
import assert from "node:assert/strict";
import React from "react";
import TestRenderer from "react-test-renderer";
import { buildBdtTreasureUploadParams } from "../src/features/bdt/bdtActiveStageNavigation.js";
import { BdtActiveStageScreenController } from "../src/features/bdt/BdtActiveStageScreenController.js";

const { act, create } = TestRenderer;

const components = {
  ActivityIndicator: "ActivityIndicator",
  Pressable: "Pressable",
  SafeAreaView: "SafeAreaView",
  ScrollView: "ScrollView",
  Text: "Text",
  View: "View",
};

test("BdtActiveStageScreenController renders active stage, countdown and upload action", async () => {
  const originalFetch = globalThis.fetch;
  globalThis.fetch = async () => activeStageResponse();

  try {
    let renderer;
    await act(async () => {
      renderer = create(
        React.createElement(BdtActiveStageScreenController, {
          apiBaseUrl: "https://api.test",
          token: "token",
          partidaId: "partida-1",
          components,
          requestGeolocationPermission: async () => ({ granted: true }),
          now: () => new Date("2026-01-01T00:03:15Z"),
          createCountdownInterval: () => () => undefined,
        }),
      );
    });

    const renderedText = renderer.root.findAllByType("Text").map((node) => node.props.children).join(" ");
    assert.match(renderedText, /Ruta QR/);
    assert.match(renderedText, /Etapa: 1/);
    assert.match(renderedText, /Estado etapa: Activa/);
    assert.match(renderedText, /Tiempo restante: 105s/);
    assert.match(renderedText, /Subir tesoro/);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("BdtActiveStageScreenController updates countdown while mounted", async () => {
  const originalFetch = globalThis.fetch;
  globalThis.fetch = async () => activeStageResponse();
  let current = new Date("2026-01-01T00:03:15Z");
  let tick = null;

  try {
    let renderer;
    await act(async () => {
      renderer = create(
        React.createElement(BdtActiveStageScreenController, {
          apiBaseUrl: "https://api.test",
          token: "token",
          partidaId: "partida-1",
          components,
          requestGeolocationPermission: async () => ({ granted: true }),
          now: () => current,
          createCountdownInterval: (callback) => {
            tick = callback;
            return () => undefined;
          },
        }),
      );
    });

    let renderedText = renderer.root.findAllByType("Text").map((node) => node.props.children).join(" ");
    assert.match(renderedText, /Tiempo restante: 105s/);

    current = new Date("2026-01-01T00:04:00Z");
    await act(async () => {
      tick();
    });

    renderedText = renderer.root.findAllByType("Text").map((node) => node.props.children).join(" ");
    assert.match(renderedText, /Tiempo restante: 60s/);

    current = new Date("2026-01-01T00:05:01Z");
    await act(async () => {
      tick();
    });

    renderedText = renderer.root.findAllByType("Text").map((node) => node.props.children).join(" ");
    assert.match(renderedText, /Tiempo restante: 0s/);
    assert.match(renderedText, /La etapa ya expiro/);
    assert.doesNotMatch(renderedText, /Subir tesoro/);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("BdtActiveStageScreenController blocks active participation when geolocation is denied", async () => {
  const originalFetch = globalThis.fetch;
  let fetchCalls = 0;
  globalThis.fetch = async () => {
    fetchCalls++;
    return activeStageResponse();
  };

  try {
    let renderer;
    await act(async () => {
      renderer = create(
        React.createElement(BdtActiveStageScreenController, {
          apiBaseUrl: "https://api.test",
          token: "token",
          partidaId: "partida-1",
          components,
          requestGeolocationPermission: async () => ({ granted: false }),
        }),
      );
    });

    const renderedText = renderer.root.findAllByType("Text").map((node) => node.props.children).join(" ");
    assert.match(renderedText, /Debes permitir geolocalizacion/);
    assert.doesNotMatch(renderedText, /Subir tesoro/);
    assert.equal(fetchCalls, 0);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("BdtActiveStageScreenController shows unavailable geolocation adapter state", async () => {
  const originalFetch = globalThis.fetch;
  let fetchCalls = 0;
  globalThis.fetch = async () => {
    fetchCalls++;
    return activeStageResponse();
  };

  try {
    let renderer;
    await act(async () => {
      renderer = create(
        React.createElement(BdtActiveStageScreenController, {
          apiBaseUrl: "https://api.test",
          token: "token",
          partidaId: "partida-1",
          components,
          requestGeolocationPermission: async () => ({ granted: false, unavailable: true }),
        }),
      );
    });

    const renderedText = renderer.root.findAllByType("Text").map((node) => node.props.children).join(" ");
    assert.match(renderedText, /geolocalizacion no esta disponible/);
    assert.equal(fetchCalls, 0);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("BdtActiveStageScreenController maps no-active-stage conflict without upload action", async () => {
  const originalFetch = globalThis.fetch;
  globalThis.fetch = async () => new Response(JSON.stringify({ message: "conflict" }), { status: 409 });

  try {
    let renderer;
    await act(async () => {
      renderer = create(
        React.createElement(BdtActiveStageScreenController, {
          apiBaseUrl: "https://api.test",
          token: "token",
          partidaId: "partida-1",
          components,
          requestGeolocationPermission: async () => ({ granted: true }),
        }),
      );
    });

    const renderedText = renderer.root.findAllByType("Text").map((node) => node.props.children).join(" ");
    assert.match(renderedText, /La etapa activa no esta disponible/);
    assert.doesNotMatch(renderedText, /Subir tesoro/);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("BdtActiveStageScreenController calls HU-45 navigation callback from upload action", async () => {
  const originalFetch = globalThis.fetch;
  globalThis.fetch = async () => activeStageResponse();
  let uploadedStage = null;

  try {
    let renderer;
    await act(async () => {
      renderer = create(
        React.createElement(BdtActiveStageScreenController, {
          apiBaseUrl: "https://api.test",
          token: "token",
          partidaId: "partida-1",
          components,
          requestGeolocationPermission: async () => ({ granted: true }),
          now: () => new Date("2026-01-01T00:03:15Z"),
          createCountdownInterval: () => () => undefined,
          onUploadTreasure: (stageData) => {
            uploadedStage = stageData;
          },
        }),
      );
    });

    const uploadButton = renderer.root
      .findAllByType("Pressable")
      .find((node) => node.findAllByType("Text").some((textNode) => textNode.props.children === "Subir tesoro"));

    await act(async () => {
      uploadButton.props.onPress();
    });

    assert.equal(uploadedStage.partidaId, "partida-1");
    assert.equal(uploadedStage.etapaActiva.orden, 1);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("buildBdtTreasureUploadParams maps HU-44 active stage to HU-45 route params", () => {
  assert.deepEqual(
    buildBdtTreasureUploadParams({
      partidaId: "partida-1",
      etapaActiva: {
        etapaId: "etapa-1",
      },
    }),
    {
      partidaId: "partida-1",
      etapaId: "etapa-1",
    },
  );
});

test("BdtActiveStageScreenController refreshes on documented PartidaBDTIniciada message only", async () => {
  const originalFetch = globalThis.fetch;
  let fetchCalls = 0;
  globalThis.fetch = async () => {
    fetchCalls++;
    return activeStageResponse();
  };
  const subscriptions = [];
  const realtimeClient = {
    subscribe(eventName, handler) {
      subscriptions.push({ eventName, handler });
      return () => undefined;
    },
  };

  try {
    await act(async () => {
      create(
        React.createElement(BdtActiveStageScreenController, {
          apiBaseUrl: "https://api.test",
          token: "token",
          partidaId: "partida-1",
          components,
          requestGeolocationPermission: async () => ({ granted: true }),
          realtimeClient,
          createCountdownInterval: () => () => undefined,
        }),
      );
    });

    assert.deepEqual(subscriptions.map((subscription) => subscription.eventName), ["PartidaBDTIniciada"]);
    assert.equal(fetchCalls, 1);

    await act(async () => {
      subscriptions[0].handler({ partidaId: "other" });
    });
    assert.equal(fetchCalls, 1);

    await act(async () => {
      subscriptions[0].handler({ partidaId: "partida-1" });
    });
    assert.equal(fetchCalls, 2);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

function activeStageResponse() {
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
}
