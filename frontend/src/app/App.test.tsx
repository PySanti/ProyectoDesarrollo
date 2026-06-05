import { describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import * as bdtApi from "../api/bdtApi";
import * as identityApi from "../api/identityApi";

const { initMock } = vi.hoisted(() => ({
  initMock: vi.fn()
}));

vi.mock("../auth/keycloak", () => {
  return {
    authProvider: {
      init: initMock,
      logout: vi.fn()
    }
  };
});

import { App } from "./App";

describe("App auth guard", () => {
  it("blocks users without admin or operator role", async () => {
    initMock.mockResolvedValueOnce({
      username: "participante",
      roles: ["Participante"],
      token: "token"
    });

    render(<App />);

    await waitFor(() => {
      expect(screen.getByText(/acceso restringido/i)).toBeInTheDocument();
    });
  });

  it("shows BDT creation flow for operator users", async () => {
    initMock.mockResolvedValueOnce({
      username: "operador",
      roles: ["Operador"],
      token: "token"
    });

    render(<App />);

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /crear bdt/i })).toBeInTheDocument();
    });

    await userEvent.click(screen.getByRole("button", { name: /crear bdt/i }));

    expect(screen.getByRole("heading", { name: /crear partida bdt/i })).toBeInTheDocument();
  });

  it("shows published BDT flow for operator users", async () => {
    vi.spyOn(bdtApi, "getOperatorPublishedBdtGames").mockResolvedValue([]);
    initMock.mockResolvedValueOnce({
      username: "operador",
      roles: ["Operador"],
      token: "token"
    });

    render(<App />);

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /partidas bdt/i })).toBeInTheDocument();
    });

    await userEvent.click(screen.getByRole("button", { name: /partidas bdt/i }));

    expect(await screen.findByRole("heading", { name: /partidas bdt publicadas/i })).toBeInTheDocument();
  });

  it("shows form for admin users", async () => {
    vi.spyOn(identityApi, "getIdentityUsers").mockResolvedValue([]);

    initMock.mockResolvedValueOnce({
      username: "admin",
      roles: ["Administrador"],
      token: "token"
    });

    render(<App />);

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /crear usuario/i })).toBeInTheDocument();
    });

    await userEvent.click(screen.getByRole("button", { name: /gestion de usuarios/i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /gestion de usuarios/i })).toBeInTheDocument();
    });
  });
});
