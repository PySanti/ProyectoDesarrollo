import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import * as identityApi from "../api/identityApi";
import * as partidasApi from "../api/partidasApi";
import * as puntuacionesApi from "../api/puntuacionesApi";

const { initMock } = vi.hoisted(() => ({
  initMock: vi.fn()
}));

vi.mock("../auth/keycloak", () => {
  return {
    authProvider: {
      init: initMock,
      login: vi.fn(),
      logout: vi.fn(),
      refresh: vi.fn().mockResolvedValue("tok")
    }
  };
});

// La consola de sesion abre conexiones SignalR reales (useSesionHub/useRankingHub)
// que requieren VITE_GATEWAY_BASE_URL; sin mock, el efecto lanza sincronicamente
// y rompe el render. Mismo patron que SesionOperadorPage.test.tsx.
vi.mock("../features/partidas/useSesionHub", () => ({ useSesionHub: vi.fn() }));
vi.mock("../features/partidas/useRankingHub", () => ({ useRankingHub: vi.fn() }));

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
      permisos: [],
      token: "token"
    });

    render(<App />);

    expect(
      await screen.findByText(/el panel web es exclusivo para administradores y operadores/i)
    ).toBeInTheDocument();
  });

  it("lands an operator on Partidas and navigates to Nueva partida", async () => {
    vi.spyOn(partidasApi, "getPartidas").mockResolvedValue([]);
    initMock.mockResolvedValueOnce({
      username: "operador",
      roles: ["Operador"],
      permisos: ["GestionarPartidas"],
      token: "token"
    });

    render(<App />);

    // Operador landing = Partidas.
    expect(await screen.findByRole("heading", { name: /^partidas$/i })).toBeInTheDocument();

    await userEvent.click(screen.getByRole("link", { name: /nueva partida/i }));

    expect(await screen.findByRole("heading", { name: /crear partida/i })).toBeInTheDocument();
  });

  it("lands an admin on user management and navigates to Crear usuario", async () => {
    vi.spyOn(identityApi, "getIdentityUsers").mockResolvedValue([]);
    initMock.mockResolvedValueOnce({
      username: "admin",
      roles: ["Administrador"],
      permisos: [],
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
    vi.spyOn(partidasApi, "getPartidas").mockResolvedValue([]);
    initMock.mockResolvedValueOnce({
      username: "operador",
      roles: ["Operador"],
      permisos: ["GestionarPartidas"],
      token: "token"
    });

    render(<App />);

    await screen.findByRole("heading", { name: /^partidas$/i });

    expect(screen.queryByRole("link", { name: /gestión de usuarios/i })).not.toBeInTheDocument();
  });

  it("allows an admin to reach a partida history", async () => {
    vi.spyOn(puntuacionesApi, "getHistorialPartida").mockResolvedValue({
      partidaId: "p1",
      total: 0,
      entradas: []
    });
    window.history.pushState({}, "", "/partidas/p1/historial");
    initMock.mockResolvedValueOnce({
      username: "admin",
      roles: ["Administrador"],
      permisos: [],
      token: "token"
    });

    render(<App />);

    expect(
      await screen.findByRole("heading", { name: /historial de la partida/i })
    ).toBeInTheDocument();
  });

  it("allows an admin to reach team performance", async () => {
    window.history.pushState({}, "", "/puntuaciones/equipos");
    initMock.mockResolvedValueOnce({
      username: "admin",
      roles: ["Administrador"],
      permisos: [],
      token: "token"
    });

    render(<App />);

    expect(
      await screen.findByRole("heading", { name: /rendimiento de equipo/i })
    ).toBeInTheDocument();
  });

  it("allows an operator to reach the teams list", async () => {
    vi.spyOn(identityApi, "getEquipos").mockResolvedValue([]);
    window.history.pushState({}, "", "/equipos");
    initMock.mockResolvedValueOnce({
      username: "operador",
      roles: ["Operador"],
      permisos: ["GestionarPartidas"],
      token: "token"
    });

    render(<App />);

    expect(await screen.findByRole("heading", { name: /equipos/i })).toBeInTheDocument();
  });

  it("allows an admin to reach the partidas list in read-only mode", async () => {
    vi.spyOn(partidasApi, "getPartidas").mockResolvedValue([]);
    window.history.pushState({}, "", "/partidas");
    initMock.mockResolvedValueOnce({
      username: "admin",
      roles: ["Administrador"],
      permisos: [],
      token: "token"
    });

    render(<App />);

    expect(await screen.findByTestId("lista-partidas")).toBeInTheDocument();
    expect(screen.queryByTestId("btn-nueva-partida")).toBeNull();
  });

  it("allows an admin to reach the sesion console", async () => {
    window.history.pushState({}, "", "/partidas/11111111-1111-1111-1111-111111111111/sesion");
    initMock.mockResolvedValueOnce({
      username: "admin",
      roles: ["Administrador"],
      permisos: [],
      token: "token"
    });

    render(<App />);

    expect(await screen.findByTestId("sesion-operador")).toBeInTheDocument();
  });

  it("keeps partida creation unavailable to an admin without GestionarPartidas", async () => {
    vi.spyOn(identityApi, "getIdentityUsers").mockResolvedValue([]);
    window.history.pushState({}, "", "/partidas/crear");
    initMock.mockResolvedValueOnce({
      username: "admin",
      roles: ["Administrador"],
      permisos: [],
      token: "token"
    });

    render(<App />);

    expect(
      await screen.findByRole("heading", { name: /gesti[oó]n de usuarios/i })
    ).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: /crear partida/i })).not.toBeInTheDocument();
  });

  /* BR-R02: el permiso autoriza, no el rol. El backend ya lo aplica
     (Partidas: policy GestionarPartidas); la web debe coincidir. */
  it("lets an admin granted GestionarPartidas reach partida creation", async () => {
    window.history.pushState({}, "", "/partidas/crear");
    initMock.mockResolvedValueOnce({
      username: "admin",
      roles: ["Administrador"],
      permisos: ["GestionarPartidas"],
      token: "token"
    });

    render(<App />);

    expect(await screen.findByRole("heading", { name: /crear partida/i })).toBeInTheDocument();
  });

  it("shows 'Nueva partida' in the nav to an admin granted GestionarPartidas", async () => {
    vi.spyOn(partidasApi, "getPartidas").mockResolvedValue([]);
    window.history.pushState({}, "", "/partidas");
    initMock.mockResolvedValueOnce({
      username: "admin",
      roles: ["Administrador"],
      permisos: ["GestionarPartidas"],
      token: "token"
    });

    render(<App />);

    await screen.findByTestId("lista-partidas");
    expect(screen.getByRole("link", { name: /nueva partida/i })).toBeInTheDocument();
    expect(screen.getByTestId("btn-nueva-partida")).toBeInTheDocument();
  });

  it("hides operate actions from an operator whose GestionarPartidas was revoked", async () => {
    vi.spyOn(partidasApi, "getPartidas").mockResolvedValue([]);
    window.history.pushState({}, "", "/partidas");
    initMock.mockResolvedValueOnce({
      username: "operador",
      roles: ["Operador"],
      permisos: [],
      token: "token"
    });

    render(<App />);

    await screen.findByTestId("lista-partidas");
    expect(screen.queryByTestId("btn-nueva-partida")).toBeNull();
    expect(screen.queryByRole("link", { name: /nueva partida/i })).not.toBeInTheDocument();
  });
});
