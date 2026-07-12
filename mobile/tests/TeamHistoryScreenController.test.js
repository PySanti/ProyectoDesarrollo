import test from "node:test";
import assert from "node:assert/strict";
import React from "react";
import TestRenderer from "react-test-renderer";
import { TeamHistoryScreenController } from "../src/features/teams/TeamHistoryScreenController.js";

const { act, create } = TestRenderer;

const components = {
  ActivityIndicator: "ActivityIndicator",
  ScrollView: "ScrollView",
  SafeAreaView: "SafeAreaView",
  Text: "Text",
  View: "View",
};

function renderedText(root) {
  return root.findAllByType("Text").map((node) => node.props.children).join(" ");
}

test("TeamHistoryScreenController renders the history items on a successful load", async () => {
  const originalFetch = globalThis.fetch;

  globalThis.fetch = async () =>
    new Response(JSON.stringify({ historial: [{ nombreEquipo: "Titanes", equipoId: "e1", fechaRegistro: "2026-07-08T00:00:00Z" }] }), {
      status: 200,
    });

  try {
    let renderer;
    await act(async () => {
      renderer = create(
        React.createElement(TeamHistoryScreenController, {
          apiBaseUrl: "https://api.test",
          token: "token",
          components,
        }),
      );
    });

    assert.match(renderedText(renderer.root), /Titanes/);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("TeamHistoryScreenController renders the empty state when the history list is empty", async () => {
  const originalFetch = globalThis.fetch;

  globalThis.fetch = async () => new Response(JSON.stringify({ historial: [] }), { status: 200 });

  try {
    let renderer;
    await act(async () => {
      renderer = create(
        React.createElement(TeamHistoryScreenController, {
          apiBaseUrl: "https://api.test",
          token: "token",
          components,
        }),
      );
    });

    assert.match(renderedText(renderer.root), /no perteneces a ningún equipo/i);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("TeamHistoryScreenController renders the error message when the load fails", async () => {
  const originalFetch = globalThis.fetch;

  globalThis.fetch = async () => new Response(null, { status: 500 });

  try {
    let renderer;
    await act(async () => {
      renderer = create(
        React.createElement(TeamHistoryScreenController, {
          apiBaseUrl: "https://api.test",
          token: "token",
          components,
        }),
      );
    });

    assert.match(renderedText(renderer.root), /No se pudo cargar el historial de equipos/);
  } finally {
    globalThis.fetch = originalFetch;
  }
});
