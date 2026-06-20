import test from "node:test";
import assert from "node:assert/strict";
import React from "react";
import TestRenderer from "react-test-renderer";
import { buildBdtTreasureUploadParams } from "../src/features/bdt/bdtActiveStageNavigation.js";
import { useBdtActiveStage } from "../src/features/bdt/useBdtActiveStage.js";

const { act, create } = TestRenderer;

/**
 * Arnés que ejecuta el hook y expone su valor de retorno (`latest`) para aserciones de comportamiento,
 * sin acoplarse al render de la pantalla (que ahora es presentacional/inmersivo).
 */
function renderHook(props) {
  const box = { latest: null };
  function Harness(harnessProps) {
    box.latest = useBdtActiveStage(harnessProps);
    return null;
  }
  let renderer;
  // create se envuelve fuera por el llamador dentro de act.
  renderer = create(React.createElement(Harness, props));
  return { box, renderer };
}

test("useBdtActiveStage expone etapa activa, countdown y subida habilitada", async () => {
  const originalFetch = globalThis.fetch;
  globalThis.fetch = async () => activeStageResponse();

  try {
    let box;
    await act(async () => {
      ({ box } = renderHook({
        apiBaseUrl: "https://api.test",
        token: "token",
        partidaId: "partida-1",
        requestGeolocationPermission: async () => ({ granted: true }),
        now: () => new Date("2026-01-01T00:03:15Z"),
        createCountdownInterval: () => () => undefined,
      }));
    });

    assert.equal(box.latest.stageData.nombre, "Ruta QR");
    assert.equal(box.latest.stageData.etapaActiva.orden, 1);
    assert.equal(box.latest.stageData.etapaActiva.estado, "Activa");
    assert.equal(box.latest.remainingSeconds, 105);
    assert.equal(box.latest.uploadEnabled, true);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("useBdtActiveStage actualiza el countdown mientras está montado", async () => {
  const originalFetch = globalThis.fetch;
  globalThis.fetch = async () => activeStageResponse();
  let current = new Date("2026-01-01T00:03:15Z");
  let tick = null;

  try {
    let box;
    await act(async () => {
      ({ box } = renderHook({
        apiBaseUrl: "https://api.test",
        token: "token",
        partidaId: "partida-1",
        requestGeolocationPermission: async () => ({ granted: true }),
        now: () => current,
        createCountdownInterval: (callback) => {
          tick = callback;
          return () => undefined;
        },
      }));
    });

    assert.equal(box.latest.remainingSeconds, 105);

    current = new Date("2026-01-01T00:04:00Z");
    await act(async () => {
      tick();
    });
    assert.equal(box.latest.remainingSeconds, 60);

    current = new Date("2026-01-01T00:05:01Z");
    await act(async () => {
      tick();
    });
    assert.equal(box.latest.remainingSeconds, 0);
    assert.equal(box.latest.uploadEnabled, false);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("useBdtActiveStage bloquea la participación y no consulta cuando se niega la geolocalización", async () => {
  const originalFetch = globalThis.fetch;
  let fetchCalls = 0;
  globalThis.fetch = async () => {
    fetchCalls++;
    return activeStageResponse();
  };

  try {
    let box;
    await act(async () => {
      ({ box } = renderHook({
        apiBaseUrl: "https://api.test",
        token: "token",
        partidaId: "partida-1",
        requestGeolocationPermission: async () => ({ granted: false }),
      }));
    });

    assert.equal(box.latest.permissionDenied, true);
    assert.equal(box.latest.permissionUnavailable, false);
    assert.equal(box.latest.uploadEnabled, false);
    assert.equal(fetchCalls, 0);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("useBdtActiveStage marca geolocalización no disponible en el dispositivo", async () => {
  const originalFetch = globalThis.fetch;
  let fetchCalls = 0;
  globalThis.fetch = async () => {
    fetchCalls++;
    return activeStageResponse();
  };

  try {
    let box;
    await act(async () => {
      ({ box } = renderHook({
        apiBaseUrl: "https://api.test",
        token: "token",
        partidaId: "partida-1",
        requestGeolocationPermission: async () => ({ granted: false, unavailable: true }),
      }));
    });

    assert.equal(box.latest.permissionDenied, true);
    assert.equal(box.latest.permissionUnavailable, true);
    assert.equal(fetchCalls, 0);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("useBdtActiveStage mapea el conflicto sin etapa activa (409) sin habilitar subida", async () => {
  const originalFetch = globalThis.fetch;
  globalThis.fetch = async () => new Response(JSON.stringify({ message: "conflict" }), { status: 409 });

  try {
    let box;
    await act(async () => {
      ({ box } = renderHook({
        apiBaseUrl: "https://api.test",
        token: "token",
        partidaId: "partida-1",
        requestGeolocationPermission: async () => ({ granted: true }),
      }));
    });

    assert.match(box.latest.unavailableMessage ?? box.latest.errorMessage ?? "", /no esta disponible/i);
    assert.equal(box.latest.stageData, null);
    assert.equal(box.latest.uploadEnabled, false);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("buildBdtTreasureUploadParams mapea HU-44 etapa activa a params HU-45", () => {
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

test("useBdtActiveStage refresca solo ante el mensaje documentado PartidaBDTIniciada", async () => {
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
      renderHook({
        apiBaseUrl: "https://api.test",
        token: "token",
        partidaId: "partida-1",
        requestGeolocationPermission: async () => ({ granted: true }),
        realtimeClient,
        createCountdownInterval: () => () => undefined,
      });
    });

    assert.deepEqual(
      subscriptions.map((subscription) => subscription.eventName),
      ["PartidaBDTIniciada"],
    );
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
