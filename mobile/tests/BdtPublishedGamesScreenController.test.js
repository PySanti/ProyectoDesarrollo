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
