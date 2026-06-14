import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import * as bdtApi from "../api/bdtApi";
import * as identityApi from "../api/identityApi";
import * as triviaApi from "../api/triviaApi";

const { initMock } = vi.hoisted(() => ({
  initMock: vi.fn()
}));

vi.mock("../auth/keycloak", () => {
  return {
    authProvider: {
      init: initMock,
      login: vi.fn(),
      logout: vi.fn()
    }
  };
});

import { App } from "./App";

beforeEach(() => {
  initMock.mockReset();
  // Reset the URL so each test starts from the index route (role landing).
  window.history.pushState({}, "", "/");
});

describe("App shell + auth guard", () => {
  it("blocks users without admin or operator role", async () => {
    initMock.mockResolvedValueOnce({
      username: "participante",
      roles: ["Participante"],
      token: "token"
    });

    render(<App />);

    expect(await screen.findByText(/acceso restringido/i)).toBeInTheDocument();
  });

  it("lands an operator on Operar Trivia and navigates to Crear BDT", async () => {
    vi.spyOn(triviaApi, "getOperatorTriviaGamesForSupervision").mockResolvedValue([]);
    initMock.mockResolvedValueOnce({
      username: "operador",
      roles: ["Operador"],
      token: "token"
    });

    render(<App />);

    // Operador landing = Operar Trivia.
    expect(await screen.findByRole("heading", { name: /operaci[oó]n trivia/i })).toBeInTheDocument();

    await userEvent.click(screen.getByRole("link", { name: /crear bdt/i }));

    expect(await screen.findByRole("heading", { name: /crear partida bdt/i })).toBeInTheDocument();
  });

  it("navigates an operator to published BDT games", async () => {
    vi.spyOn(triviaApi, "getOperatorTriviaGamesForSupervision").mockResolvedValue([]);
    vi.spyOn(bdtApi, "getOperatorPublishedBdtGames").mockResolvedValue([]);
    initMock.mockResolvedValueOnce({
      username: "operador",
      roles: ["Operador"],
      token: "token"
    });

    render(<App />);

    await userEvent.click(await screen.findByRole("link", { name: /partidas bdt/i }));

    expect(
      await screen.findByRole("heading", { name: /partidas bdt publicadas/i })
    ).toBeInTheDocument();
  });

  it("lands an admin on user management and navigates to Crear usuario", async () => {
    vi.spyOn(identityApi, "getIdentityUsers").mockResolvedValue([]);
    initMock.mockResolvedValueOnce({
      username: "admin",
      roles: ["Administrador"],
      token: "token"
    });

    render(<App />);

    // Administrador landing = Gestión de usuarios.
    expect(
      await screen.findByRole("heading", { name: /gesti[oó]n de usuarios/i })
    ).toBeInTheDocument();

    await userEvent.click(screen.getByRole("link", { name: /crear usuario/i }));

    expect(await screen.findByRole("heading", { name: /crear usuario/i })).toBeInTheDocument();
  });

  it("does not show admin areas to an operator", async () => {
    vi.spyOn(triviaApi, "getOperatorTriviaGamesForSupervision").mockResolvedValue([]);
    initMock.mockResolvedValueOnce({
      username: "operador",
      roles: ["Operador"],
      token: "token"
    });

    render(<App />);

    await screen.findByRole("heading", { name: /operaci[oó]n trivia/i });

    expect(screen.queryByRole("link", { name: /gestión de usuarios/i })).not.toBeInTheDocument();
  });
});
