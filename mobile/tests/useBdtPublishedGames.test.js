import test from "node:test";
import assert from "node:assert/strict";
import React from "react";
import TestRenderer from "react-test-renderer";
import { useBdtPublishedGames } from "../src/features/bdt/useBdtPublishedGames.js";

const { act, create } = TestRenderer;

/** Arnés que ejecuta el hook y expone su valor de retorno para aserciones de comportamiento. */
function renderHook(props) {
  const box = { latest: null };
  function Harness(harnessProps) {
    box.latest = useBdtPublishedGames(harnessProps);
    return null;
  }
  create(React.createElement(Harness, props));
  return box;
}

test("useBdtPublishedGames carga las BDT publicadas", async () => {
  const originalFetch = globalThis.fetch;
  globalThis.fetch = async () =>
    new Response(
      JSON.stringify([
        {
          partidaId: "00000000-0000-0000-0000-000000000001",
          nombre: "Ruta nocturna",
          modalidad: "Equipo",
          estado: "Lobby",
          areaBusqueda: "Campus central",
          cantidadEtapas: 3,
        },
      ]),
      { status: 200, headers: { "Content-Type": "application/json" } },
    );

  try {
    let box;
    await act(async () => {
      box = renderHook({ apiBaseUrl: "https://api.test", token: "token" });
    });

    assert.equal(box.latest.games.length, 1);
    assert.equal(box.latest.games[0].nombre, "Ruta nocturna");
    assert.equal(box.latest.games[0].modalidad, "Equipo");
    assert.equal(box.latest.games[0].cantidadEtapas, 3);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("useBdtPublishedGames envía el filtro Equipo al seleccionarlo", async () => {
  const originalFetch = globalThis.fetch;
  const requestedUrls = [];
  globalThis.fetch = async (url) => {
    requestedUrls.push(url);
    return new Response(JSON.stringify([]), { status: 200, headers: { "Content-Type": "application/json" } });
  };

  try {
    let box;
    await act(async () => {
      box = renderHook({ apiBaseUrl: "https://api.test", token: "token" });
    });

    await act(async () => {
      box.latest.setFilter("Equipo");
    });

    assert.equal(requestedUrls.at(-1), "https://api.test/api/bdt/games/published?modalidad=Equipo");
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("useBdtPublishedGames expone el error de filtro inválido", async () => {
  const originalFetch = globalThis.fetch;
  globalThis.fetch = async () => new Response(JSON.stringify([]), { status: 400, headers: { "Content-Type": "application/json" } });

  try {
    let box;
    await act(async () => {
      box = renderHook({ apiBaseUrl: "https://api.test", token: "token" });
    });

    assert.match(box.latest.errorMessage, /filtro de modalidad no es valido/i);
    assert.equal(box.latest.games.length, 0);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("useBdtPublishedGames inscribe individual y entrega datos de espera", async () => {
  const originalFetch = globalThis.fetch;
  const requested = [];
  globalThis.fetch = async (url, options) => {
    requested.push({ url, options });
    if (options?.method === "POST") {
      return new Response(
        JSON.stringify({
          partidaId: "00000000-0000-0000-0000-000000000039",
          nombre: "Ruta individual",
          modalidad: "Individual",
          estado: "Lobby",
          inscripcionId: "00000000-0000-0000-0000-000000000001",
          participanteUserId: "00000000-0000-0000-0000-000000000002",
          posicionEnLobby: 1,
          mensaje: "Te uniste a la BDT. Espera el inicio de la partida.",
        }),
        { status: 200, headers: { "Content-Type": "application/json" } },
      );
    }

    return new Response(
      JSON.stringify([
        {
          partidaId: "00000000-0000-0000-0000-000000000039",
          nombre: "Ruta individual",
          modalidad: "Individual",
          estado: "Lobby",
          areaBusqueda: "Campus central",
          cantidadEtapas: 1,
        },
      ]),
      { status: 200, headers: { "Content-Type": "application/json" } },
    );
  };

  try {
    let box;
    await act(async () => {
      box = renderHook({ apiBaseUrl: "https://api.test", token: "token" });
    });

    await act(async () => {
      box.latest.joinIndividual(box.latest.games[0]);
    });

    const postCall = requested.find((request) => request.options?.method === "POST");
    assert.equal(postCall.url, "https://api.test/api/bdt/games/00000000-0000-0000-0000-000000000039/individual-inscriptions");
    assert.equal(postCall.options.headers.Authorization, "Bearer token");
    assert.equal(box.latest.waitingData.posicionEnLobby, 1);
    assert.equal(box.latest.waitingData.nombre, "Ruta individual");
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("useBdtPublishedGames expone errores de conflicto de inscripción", async () => {
  const originalFetch = globalThis.fetch;
  globalThis.fetch = async (url, options) => {
    if (options?.method === "POST") {
      return new Response(JSON.stringify({ message: "duplicado" }), { status: 409, headers: { "Content-Type": "application/json" } });
    }

    return new Response(
      JSON.stringify([
        {
          partidaId: "00000000-0000-0000-0000-000000000039",
          nombre: "Ruta individual",
          modalidad: "Individual",
          estado: "Lobby",
          areaBusqueda: "Campus central",
          cantidadEtapas: 1,
        },
      ]),
      { status: 200, headers: { "Content-Type": "application/json" } },
    );
  };

  try {
    let box;
    await act(async () => {
      box = renderHook({ apiBaseUrl: "https://api.test", token: "token" });
    });

    await act(async () => {
      box.latest.joinIndividual(box.latest.games[0]);
    });

    assert.match(box.latest.joinErrorMessage, /No puedes unirte a esta BDT/);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("useBdtPublishedGames marca la partida en curso mientras la inscripción está en vuelo", async () => {
  const originalFetch = globalThis.fetch;
  let resolveJoin;
  globalThis.fetch = async (url, options) => {
    if (options?.method === "POST") {
      return new Promise((resolve) => {
        resolveJoin = () =>
          resolve(
            new Response(
              JSON.stringify({
                partidaId: "00000000-0000-0000-0000-000000000039",
                nombre: "Ruta individual",
                modalidad: "Individual",
                estado: "Lobby",
                posicionEnLobby: 1,
                mensaje: "ok",
              }),
              { status: 200, headers: { "Content-Type": "application/json" } },
            ),
          );
      });
    }

    return new Response(
      JSON.stringify([
        {
          partidaId: "00000000-0000-0000-0000-000000000039",
          nombre: "Ruta individual",
          modalidad: "Individual",
          estado: "Lobby",
          areaBusqueda: "Campus central",
          cantidadEtapas: 1,
        },
      ]),
      { status: 200, headers: { "Content-Type": "application/json" } },
    );
  };

  try {
    let box;
    await act(async () => {
      box = renderHook({ apiBaseUrl: "https://api.test", token: "token" });
    });

    await act(async () => {
      box.latest.joinIndividual(box.latest.games[0]);
    });

    assert.equal(box.latest.joiningPartidaId, "00000000-0000-0000-0000-000000000039");

    await act(async () => {
      resolveJoin();
    });

    assert.equal(box.latest.joiningPartidaId, null);
  } finally {
    globalThis.fetch = originalFetch;
  }
});
