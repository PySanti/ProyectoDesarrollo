import test from "node:test";
import assert from "node:assert/strict";
import React from "react";
import TestRenderer from "react-test-renderer";
import { LeaveTeamScreenController } from "../src/features/teams/LeaveTeamScreenController.js";

const { act, create } = TestRenderer;

const components = {
  ActivityIndicator: "ActivityIndicator",
  Pressable: "Pressable",
  SafeAreaView: "SafeAreaView",
  Text: "Text",
  View: "View",
};

test("LeaveTeamScreenController renders no-active-team state after a successful leave", async () => {
  const originalFetch = globalThis.fetch;
  const leftResults = [];

  globalThis.fetch = async () =>
    new Response(
      JSON.stringify({
        userId: "11111111-1111-1111-1111-111111111111",
        equipoId: "22222222-2222-2222-2222-222222222222",
        resultado: "SalioDelEquipo",
        equipoEstado: "Activo",
      }),
      { status: 200, headers: { "Content-Type": "application/json" } },
    );

  try {
    let renderer;

    await act(async () => {
      renderer = create(
        React.createElement(LeaveTeamScreenController, {
          apiBaseUrl: "https://api.test",
          token: "token",
          onLeft: (result) => leftResults.push(result),
          components,
        }),
      );
    });

    const button = renderer.root.findAllByType("Pressable").find((node) => node.props.accessibilityRole === "button");
    assert.ok(button, "leave-team button should render");

    await act(async () => {
      await button.props.onPress();
    });

    const renderedText = renderer.root.findAllByType("Text").map((node) => node.props.children).join(" ");
    const disabledButton = renderer.root.findAllByType("Pressable").find((node) => node.props.accessibilityRole === "button");

    assert.match(renderedText, /Ya no perteneces a ningun equipo activo/);
    assert.match(renderedText, /Sin equipo activo/);
    assert.equal(disabledButton?.props.disabled, true);
    assert.equal(leftResults.length, 1);
  } finally {
    globalThis.fetch = originalFetch;
  }
});
