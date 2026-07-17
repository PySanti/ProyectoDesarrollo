import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import * as identityApi from "../api/identityApi";
import * as partidasApi from "../api/partidasApi";
import * as puntuacionesApi from "../api/puntuacionesApi";
import { authProvider } from "../auth/keycloak";
import { REFRESH_INTERVAL_MS } from "../auth/useSessionRefresh";

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
  it("muestra la pantalla de sin accesos a un participante sin ningún privilegio", async () => {
    initMock.mockResolvedValueOnce({
      username: "participante",
      roles: ["Participante"],
      permisos: [],
      token: "token"
    });

    render(<App />);

    expect(
      await screen.findByText(/esta cuenta no tiene ningún panel disponible/i)
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
      permisos: ["GestionarPartidas"],
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
      permisos: ["GestionarEquipos"],
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
      permisos: ["GestionarEquipos"],
      token: "token"
    });

    render(<App />);

    expect(await screen.findByRole("heading", { name: /equipos/i })).toBeInTheDocument();
  });

  /* El privilegio, no el rol, decide dentro del área: el área Partidas exige GestionarPartidas para
     entrar, así que quien entra siempre puede operar. Ya no existe un admin "de sólo lectura" ahí. */
  it("allows an admin with GestionarPartidas to reach the partidas list and operate", async () => {
    vi.spyOn(partidasApi, "getPartidas").mockResolvedValue([]);
    window.history.pushState({}, "", "/partidas");
    initMock.mockResolvedValueOnce({
      username: "admin",
      roles: ["Administrador"],
      permisos: ["GestionarPartidas"],
      token: "token"
    });

    render(<App />);

    expect(await screen.findByTestId("lista-partidas")).toBeInTheDocument();
    expect(await screen.findByTestId("btn-nueva-partida")).toBeInTheDocument();
  });

  it("allows an admin to reach the sesion console", async () => {
    window.history.pushState({}, "", "/partidas/11111111-1111-1111-1111-111111111111/sesion");
    initMock.mockResolvedValueOnce({
      username: "admin",
      roles: ["Administrador"],
      permisos: ["GestionarPartidas"],
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

  /* Regresión del fix real: el refresh de token (RNF-24, cada 270s) debe reemplazar el usuario
     entero, no fusionar sólo el string del token con el usuario del login. Si App.tsx volviera a
     `{ ...prev.user, token }`, el rol del login (Operador) sobreviviría al refresh y este test
     fallaría porque el área Identidad del Administrador nunca aparecería. */
  it("adopta el rol nuevo del token al refrescar, sin quedarse con el del login", async () => {
    vi.spyOn(partidasApi, "getPartidas").mockResolvedValue([]);
    initMock.mockResolvedValueOnce({
      username: "operador",
      roles: ["Operador"],
      permisos: ["GestionarPartidas"],
      token: "token-login"
    });
    vi.mocked(authProvider.refresh).mockResolvedValueOnce({
      username: "operador",
      roles: ["Administrador"],
      permisos: ["GestionarPartidas"],
      token: "token-refrescado"
    });

    // Los timers deben ser falsos ANTES del render: el interval de refresh se registra
    // dentro del efecto de useSessionRefresh apenas authState pasa a "ready", y si ese
    // registro ocurre con timers reales, avanzar el reloj falso después no lo dispara.
    vi.useFakeTimers();

    render(<App />);

    // Deja resolver authProvider.init() y montar el landing de Operador (Partidas).
    await vi.advanceTimersByTimeAsync(0);

    expect(screen.getByRole("link", { name: /nueva partida/i })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: /gesti[oó]n de usuarios/i })).not.toBeInTheDocument();

    window.dispatchEvent(new Event("pointerdown"));
    await vi.advanceTimersByTimeAsync(REFRESH_INTERVAL_MS);
    vi.useRealTimers();

    // Rol nuevo (Administrador): aparece su área Identidad, que el Operador no tiene. Con la
    // fusión, `roles` seguiría siendo ["Operador"] y esta área nunca saldría.
    expect(
      await screen.findByRole("link", { name: /gesti[oó]n de usuarios/i })
    ).toBeInTheDocument();
    // Y la barra superior deja de anunciarlo como Operador: es `roles` lo que se reemplazó.
    expect(screen.queryByText("Operador")).not.toBeInTheDocument();
  });

  /* El síntoma que originó todo: el privilegio autoriza, no el rol base. El backend ya lo aplica;
     la web debe coincidir. */
  it("deja entrar a la creación de partidas a un admin con GestionarPartidas", async () => {
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

  it("muestra el área Partidas en el nav a un admin con GestionarPartidas", async () => {
    vi.spyOn(partidasApi, "getPartidas").mockResolvedValue([]);
    window.history.pushState({}, "", "/partidas");
    initMock.mockResolvedValueOnce({
      username: "admin",
      roles: ["Administrador"],
      permisos: ["GestionarPartidas"],
      token: "token"
    });

    render(<App />);

    // Antes de este privilegio el área ni existía para el admin. "Nueva partida" sigue siendo un
    // item exclusivo de Operador dentro del área (ver el test de refresco de rol, arriba), así que
    // aquí sólo se afirma que el área en sí es navegable.
    expect(await screen.findByRole("link", { name: /^partidas$/i })).toBeInTheDocument();
  });

  it("oculta el área Partidas a un operador sin GestionarPartidas", async () => {
    vi.spyOn(identityApi, "getEquipos").mockResolvedValue([]);
    initMock.mockResolvedValueOnce({
      username: "operador",
      roles: ["Operador"],
      permisos: ["GestionarEquipos"],
      token: "token"
    });

    render(<App />);

    // Sin GestionarPartidas, Equipos es la única área del operador: aterriza ahí.
    expect(await screen.findByRole("heading", { name: /^equipos$/i })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: /^partidas$/i })).not.toBeInTheDocument();
  });

  /* El privilegio autoriza, el rol base no veta: GestionarEquipos abre los tres paneles de
     equipos, incluido «Creación de equipos», sea cual sea el rol. Antes exigía además
     Administrador, así que un Operador con el privilegio veía el link en el nav pero
     rebotaba al hacer click. Ningún test cubría esta ruta, por eso pasó desapercibido. */
  it("deja a un operador con GestionarEquipos entrar a la creación de equipos", async () => {
    window.history.pushState({}, "", "/identidad/equipos");
    initMock.mockResolvedValueOnce({
      username: "operador",
      roles: ["Operador"],
      permisos: ["GestionarEquipos"],
      token: "token"
    });

    render(<App />);

    expect(await screen.findByTestId("create-team-submit")).toBeInTheDocument();
  });

  it("muestra la pantalla de sin accesos a un operador sin ningún privilegio", async () => {
    initMock.mockResolvedValueOnce({
      username: "operador",
      roles: ["Operador"],
      permisos: [],
      token: "token"
    });

    render(<App />);

    expect(
      await screen.findByText(/esta cuenta no tiene ningún panel disponible/i)
    ).toBeInTheDocument();
  });

  /* Paridad total: un Participante con el privilegio entra al mismo panel que vería un Operador
     con ese privilegio — mismo mecanismo, D2 del spec de privilegio-sin-rol. */
  it("deja entrar a la creación de partidas a un participante con GestionarPartidas", async () => {
    vi.spyOn(partidasApi, "getPartidas").mockResolvedValue([]);
    window.history.pushState({}, "", "/partidas/crear");
    initMock.mockResolvedValueOnce({
      username: "participante",
      roles: ["Participante"],
      permisos: ["GestionarPartidas"],
      token: "token"
    });

    render(<App />);

    expect(await screen.findByRole("heading", { name: /crear partida/i })).toBeInTheDocument();
  });

  it("deja entrar a la creación de equipos a un participante con GestionarEquipos", async () => {
    window.history.pushState({}, "", "/identidad/equipos");
    initMock.mockResolvedValueOnce({
      username: "participante",
      roles: ["Participante"],
      permisos: ["GestionarEquipos"],
      token: "token"
    });

    render(<App />);

    expect(await screen.findByTestId("create-team-submit")).toBeInTheDocument();
  });
});
