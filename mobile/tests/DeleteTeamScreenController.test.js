import test from "node:test";
import assert from "node:assert/strict";
import React from "react";
import TestRenderer from "react-test-renderer";
import { DeleteTeamScreenController } from "../src/features/teams/DeleteTeamScreenController.js";

const { act, create } = TestRenderer;

const components = {
  ActivityIndicator: "ActivityIndicator",
  Pressable: "Pressable",
  SafeAreaView: "SafeAreaView",
  Text: "Text",
  View: "View",
};

function findButtonByLabel(root, label) {
  return root.findAllByType("Pressable").find((node) => {
    if (node.props.accessibilityRole !== "button") return false;
    try {
      return node.findAllByType("Text").some((textNode) => textNode.props.children === label);
    } catch {
      return false;
    }
  });
}

function renderedText(root) {
  return root.findAllByType("Text").map((node) => node.props.children).join(" ");
}

test("DeleteTeamScreenController gates the delete call behind a confirmation step", async () => {
  const originalFetch = globalThis.fetch;
  const fetchCalls = [];

  globalThis.fetch = async (...args) => {
    fetchCalls.push(args);
    return new Response(null, { status: 204 });
  };

  try {
    let renderer;
    await act(async () => {
      renderer = create(
        React.createElement(DeleteTeamScreenController, {
          apiBaseUrl: "https://api.test",
          token: "token",
          components,
        }),
      );
    });

    const deleteButton = findButtonByLabel(renderer.root, "Eliminar equipo");
    assert.ok(deleteButton, "initial delete button should render");

    await act(async () => {
      deleteButton.props.onPress();
    });

    assert.equal(fetchCalls.length, 0, "pressing the initial button must not submit yet");
    assert.match(renderedText(renderer.root), /Seguro.*Esta acción no se puede deshacer/);

    const confirmButton = findButtonByLabel(renderer.root, "Confirmar eliminación");
    assert.ok(confirmButton, "confirm button should render after requesting deletion");

    await act(async () => {
      await confirmButton.props.onPress();
    });

    assert.equal(fetchCalls.length, 1, "confirming should submit exactly once");
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("DeleteTeamScreenController shows the activeParticipation message on 409", async () => {
  const originalFetch = globalThis.fetch;

  globalThis.fetch = async () => new Response(null, { status: 409 });

  try {
    let renderer;
    await act(async () => {
      renderer = create(
        React.createElement(DeleteTeamScreenController, {
          apiBaseUrl: "https://api.test",
          token: "token",
          components,
        }),
      );
    });

    await act(async () => {
      findButtonByLabel(renderer.root, "Eliminar equipo").props.onPress();
    });

    await act(async () => {
      await findButtonByLabel(renderer.root, "Confirmar eliminación").props.onPress();
    });

    assert.match(
      renderedText(renderer.root),
      /Tu equipo participa en una partida activa y no puede eliminarse/,
    );
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("DeleteTeamScreenController invokes onDeleted when the deletion succeeds", async () => {
  const originalFetch = globalThis.fetch;
  const deletedResults = [];

  globalThis.fetch = async () => new Response(null, { status: 204 });

  try {
    let renderer;
    await act(async () => {
      renderer = create(
        React.createElement(DeleteTeamScreenController, {
          apiBaseUrl: "https://api.test",
          token: "token",
          onDeleted: (result) => deletedResults.push(result),
          components,
        }),
      );
    });

    await act(async () => {
      findButtonByLabel(renderer.root, "Eliminar equipo").props.onPress();
    });

    await act(async () => {
      await findButtonByLabel(renderer.root, "Confirmar eliminación").props.onPress();
    });

    assert.equal(deletedResults.length, 1);
    assert.match(renderedText(renderer.root), /El equipo fue eliminado con exito/);
  } finally {
    globalThis.fetch = originalFetch;
  }
});
