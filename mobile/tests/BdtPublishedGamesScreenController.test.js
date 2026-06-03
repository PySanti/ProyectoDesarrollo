import test from "node:test";
import assert from "node:assert/strict";
import React from "react";
import TestRenderer from "react-test-renderer";
import { BdtPublishedGamesScreenController } from "../src/features/bdt/BdtPublishedGamesScreenController.js";

const { act, create } = TestRenderer;

const components = {
  ActivityIndicator: "ActivityIndicator",
  Pressable: "Pressable",
  SafeAreaView: "SafeAreaView",
  ScrollView: "ScrollView",
  Text: "Text",
  View: "View",
};

test("BdtPublishedGamesScreenController renders published BDT games", async () => {
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
    let renderer;
    await act(async () => {
      renderer = create(
        React.createElement(BdtPublishedGamesScreenController, {
          apiBaseUrl: "https://api.test",
          token: "token",
          components,
        }),
      );
    });

    const renderedText = renderer.root.findAllByType("Text").map((node) => node.props.children).join(" ");

    assert.match(renderedText, /Ruta nocturna/);
    assert.match(renderedText, /Modalidad: Equipo/);
    assert.match(renderedText, /Etapas: 3/);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("BdtPublishedGamesScreenController sends Equipo filter when selected", async () => {
  const originalFetch = globalThis.fetch;
  const requestedUrls = [];
  globalThis.fetch = async (url) => {
    requestedUrls.push(url);
    return new Response(JSON.stringify([]), { status: 200, headers: { "Content-Type": "application/json" } });
  };

  try {
    let renderer;
    await act(async () => {
      renderer = create(
        React.createElement(BdtPublishedGamesScreenController, {
          apiBaseUrl: "https://api.test",
          token: "token",
          components,
        }),
      );
    });

    const equipoButton = renderer.root
      .findAllByType("Pressable")
      .find((node) => node.findAllByType("Text").some((textNode) => textNode.props.children === "Equipo"));

    assert.ok(equipoButton, "Equipo filter button should render");

    await act(async () => {
      equipoButton.props.onPress();
    });

    assert.equal(requestedUrls.at(-1), "https://api.test/api/bdt/games/published?modalidad=Equipo");
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("BdtPublishedGamesScreenController renders empty and error states", async () => {
  const originalFetch = globalThis.fetch;
  globalThis.fetch = async () => new Response(JSON.stringify([]), { status: 400, headers: { "Content-Type": "application/json" } });

  try {
    let renderer;
    await act(async () => {
      renderer = create(
        React.createElement(BdtPublishedGamesScreenController, {
          apiBaseUrl: "https://api.test",
          token: "token",
          components,
        }),
      );
    });

    const renderedText = renderer.root.findAllByType("Text").map((node) => node.props.children).join(" ");

    assert.match(renderedText, /El filtro de modalidad no es valido/);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("BdtPublishedGamesScreenController renders join button for individual games and waiting screen after success", async () => {
  const originalFetch = globalThis.fetch;
  const requestedUrls = [];
  globalThis.fetch = async (url, options) => {
    requestedUrls.push({ url, options });
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
    let renderer;
    await act(async () => {
      renderer = create(
        React.createElement(BdtPublishedGamesScreenController, {
          apiBaseUrl: "https://api.test",
          token: "token",
          components,
        }),
      );
    });

    const joinButton = renderer.root
      .findAllByType("Pressable")
      .find((node) => node.findAllByType("Text").some((textNode) => textNode.props.children === "Unirme individualmente"));

    assert.ok(joinButton, "Individual join button should render");

    await act(async () => {
      joinButton.props.onPress();
    });

    const postCall = requestedUrls.find((request) => request.options?.method === "POST");
    assert.equal(postCall.url, "https://api.test/api/bdt/games/00000000-0000-0000-0000-000000000039/individual-inscriptions");
    assert.equal(postCall.options.headers.Authorization, "Bearer token");

    const renderedText = renderer.root.findAllByType("Text").map((node) => node.props.children).join(" ");
    assert.match(renderedText, /Espera de BDT/);
    assert.match(renderedText, /Ruta individual/);
    assert.match(renderedText, /Posicion en lobby: 1/);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("BdtPublishedGamesScreenController should not render individual join action for team games", async () => {
  const originalFetch = globalThis.fetch;
  globalThis.fetch = async () =>
    new Response(
      JSON.stringify([
        {
          partidaId: "00000000-0000-0000-0000-000000000040",
          nombre: "Ruta equipo",
          modalidad: "Equipo",
          estado: "Lobby",
          areaBusqueda: "Campus central",
          cantidadEtapas: 1,
        },
      ]),
      { status: 200, headers: { "Content-Type": "application/json" } },
    );

  try {
    let renderer;
    await act(async () => {
      renderer = create(
        React.createElement(BdtPublishedGamesScreenController, {
          apiBaseUrl: "https://api.test",
          token: "token",
          components,
        }),
      );
    });

    const renderedText = renderer.root.findAllByType("Text").map((node) => node.props.children).join(" ");
    assert.doesNotMatch(renderedText, /Unirme individualmente/);
    assert.match(renderedText, /La union por equipo se gestiona con el lider/);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("BdtPublishedGamesScreenController renders join conflict errors", async () => {
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
    let renderer;
    await act(async () => {
      renderer = create(
        React.createElement(BdtPublishedGamesScreenController, {
          apiBaseUrl: "https://api.test",
          token: "token",
          components,
        }),
      );
    });

    const joinButton = renderer.root
      .findAllByType("Pressable")
      .find((node) => node.findAllByType("Text").some((textNode) => textNode.props.children === "Unirme individualmente"));

    await act(async () => {
      joinButton.props.onPress();
    });

    const renderedText = renderer.root.findAllByType("Text").map((node) => node.props.children).join(" ");
    assert.match(renderedText, /No puedes unirte a esta BDT/);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("BdtPublishedGamesScreenController disables join button while request is in flight", async () => {
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
                inscripcionId: "00000000-0000-0000-0000-000000000001",
                participanteUserId: "00000000-0000-0000-0000-000000000002",
                posicionEnLobby: 1,
                mensaje: "Te uniste a la BDT. Espera el inicio de la partida.",
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
    let renderer;
    await act(async () => {
      renderer = create(
        React.createElement(BdtPublishedGamesScreenController, {
          apiBaseUrl: "https://api.test",
          token: "token",
          components,
        }),
      );
    });

    const joinButton = renderer.root
      .findAllByType("Pressable")
      .find((node) => node.findAllByType("Text").some((textNode) => textNode.props.children === "Unirme individualmente"));

    await act(async () => {
      joinButton.props.onPress();
    });

    const pendingButton = renderer.root
      .findAllByType("Pressable")
      .find((node) => node.findAllByType("Text").some((textNode) => textNode.props.children === "Uniendote..."));

    assert.equal(pendingButton.props.disabled, true);

    await act(async () => {
      resolveJoin();
    });
  } finally {
    globalThis.fetch = originalFetch;
  }
});
