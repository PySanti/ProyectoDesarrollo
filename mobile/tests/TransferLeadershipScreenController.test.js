import test from "node:test";
import assert from "node:assert/strict";
import React from "react";
import TestRenderer from "react-test-renderer";
import { TransferLeadershipScreenController } from "../src/features/teams/TransferLeadershipScreenController.js";

const { act, create } = TestRenderer;

const components = {
  ActivityIndicator: "ActivityIndicator",
  Pressable: "Pressable",
  SafeAreaView: "SafeAreaView",
  Text: "Text",
  TextInput: "TextInput",
  View: "View",
};

const currentLeaderUserId = "11111111-1111-1111-1111-111111111111";
const targetUserId = "22222222-2222-2222-2222-222222222222";

test("TransferLeadershipScreenController selects eligible member and shows success guidance", async () => {
  const originalFetch = globalThis.fetch;
  const transferred = [];
  let requestedBody;

  globalThis.fetch = async (_url, options) => {
    requestedBody = options.body;
    return new Response(
      JSON.stringify({
        equipoId: "33333333-3333-3333-3333-333333333333",
        liderAnteriorUserId: currentLeaderUserId,
        nuevoLiderUserId: targetUserId,
        equipoEstado: "Activo",
      }),
      { status: 200, headers: { "Content-Type": "application/json" } },
    );
  };

  try {
    let renderer;
    await act(async () => {
      renderer = create(
        React.createElement(TransferLeadershipScreenController, {
          apiBaseUrl: "https://api.test",
          token: "token",
          currentLeaderUserId,
          members: [
            { userId: currentLeaderUserId, nombre: "Lider", esLider: true },
            { userId: targetUserId, nombre: "Nuevo lider", esLider: false },
          ],
          onTransferred: (result) => transferred.push(result),
          components,
        }),
      );
    });

    const renderedBefore = renderer.root.findAllByType("Text").map((node) => node.props.children).join(" ");
    assert.doesNotMatch(renderedBefore, /Lider Nuevo lider/);
    assert.match(renderedBefore, /Nuevo lider/);

    const buttons = renderer.root.findAllByType("Pressable");
    const memberButton = buttons[0];
    const submitButton = buttons[1];

    await act(async () => {
      memberButton.props.onPress();
    });

    await act(async () => {
      await submitButton.props.onPress();
    });

    const renderedAfter = renderer.root.findAllByType("Text").map((node) => node.props.children).join(" ");
    assert.deepEqual(JSON.parse(requestedBody), { nuevoLiderUserId: targetUserId });
    assert.match(renderedAfter, /Liderazgo transferido correctamente/);
    assert.match(renderedAfter, /HU-07/);
    assert.equal(transferred.length, 1);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("TransferLeadershipScreenController shows not-found and conflict messages", async () => {
  const originalFetch = globalThis.fetch;
  let status = 404;

  globalThis.fetch = async () => new Response("{}", { status });

  try {
    let renderer;
    await act(async () => {
      renderer = create(
        React.createElement(TransferLeadershipScreenController, {
          apiBaseUrl: "https://api.test",
          token: "token",
          components,
        }),
      );
    });

    const input = renderer.root.findByType("TextInput");
    const button = renderer.root.findByType("Pressable");

    await act(async () => {
      input.props.onChangeText(targetUserId);
    });

    await act(async () => {
      await button.props.onPress();
    });

    let renderedText = renderer.root.findAllByType("Text").map((node) => node.props.children).join(" ");
    assert.match(renderedText, /No perteneces a ningun equipo activo/);

    status = 409;
    await act(async () => {
      await button.props.onPress();
    });

    renderedText = renderer.root.findAllByType("Text").map((node) => node.props.children).join(" ");
    assert.match(renderedText, /No se pudo transferir el liderazgo/);
  } finally {
    globalThis.fetch = originalFetch;
  }
});
