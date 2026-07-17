import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { RendimientoEquipoPage } from "./RendimientoEquipoPage";
import * as puntuacionesApi from "../../api/puntuacionesApi";
import * as partidasApi from "../../api/partidasApi";
import * as adminTeamsApi from "../../api/adminTeamsApi";
import type { AdminTeam } from "../../api/adminTeamsApi";

const GUID = "11111111-2222-3333-4444-555555555555";
const OTHER_GUID = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
const PARTIDA_ID = "aabbccdd-0000-0000-0000-000000000000";

// El backend devuelve los equipos ya ordenados por nombre y sin filtrar por estado.
const equipos: AdminTeam[] = [
  { equipoId: GUID, nombreEquipo: "Los Lobos", estado: "Activo", integrantes: [] },
  { equipoId: OTHER_GUID, nombreEquipo: "Las Águilas", estado: "Activo", integrantes: [] }
];

function renderPage(initialEntry = "/puntuaciones/equipos") {
  return render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <RendimientoEquipoPage accessToken="tok" />
    </MemoryRouter>
  );
}

const rendimiento = {
  equipoId: GUID,
  partidas: [
    {
      partidaId: PARTIDA_ID,
      fechaFin: "2026-07-10T12:00:00Z",
      posicion: 1,
      gano: true
    },
    {
      partidaId: "eeff0011-0000-0000-0000-000000000000",
      fechaFin: "2026-07-09T12:00:00Z",
      posicion: 2,
      gano: false
    }
  ]
};

beforeEach(() => {
  vi.spyOn(adminTeamsApi, "listAdminTeams").mockResolvedValue(equipos);
});
afterEach(() => vi.restoreAllMocks());

describe("RendimientoEquipoPage", () => {
  it("ofrece los equipos por nombre en vez de pedir un GUID a mano", async () => {
    renderPage();
    const selector = await screen.findByLabelText("Equipo");
    expect(await screen.findByRole("option", { name: "Los Lobos" })).toBeInTheDocument();
    expect(screen.getByRole("option", { name: "Las Águilas" })).toBeInTheDocument();
    expect(selector).toBeInTheDocument();
    // El input de GUID desaparece del flujo normal.
    expect(screen.queryByLabelText("ID del equipo")).toBeNull();
  });

  it("elegir un equipo consulta su rendimiento sin pasar por un boton", async () => {
    vi.spyOn(puntuacionesApi, "getRendimientoEquipo").mockResolvedValue(rendimiento);
    renderPage();
    await screen.findByRole("option", { name: "Los Lobos" });
    await userEvent.selectOptions(screen.getByLabelText("Equipo"), GUID);

    expect(await screen.findByTestId("tabla-rendimiento")).toBeInTheDocument();
    expect(puntuacionesApi.getRendimientoEquipo).toHaveBeenCalledWith(GUID, "tok");
    expect(screen.getByText("aabbccdd")).toBeInTheDocument();
    expect(screen.getByText("✓")).toHaveAttribute("aria-label", "Sí");
    expect(screen.getByText("—")).toHaveAttribute("aria-label", "No");
  });

  it("lista tambien los equipos no activos, marcando su estado", async () => {
    // El rendimiento es historial: un equipo eliminado conserva el suyo y debe poder consultarse.
    vi.spyOn(adminTeamsApi, "listAdminTeams").mockResolvedValue([
      ...equipos,
      { equipoId: "cccccccc-0000-0000-0000-000000000000", nombreEquipo: "Panteras", estado: "Eliminado", integrantes: [] },
      { equipoId: "dddddddd-0000-0000-0000-000000000000", nombreEquipo: "Tiburones", estado: "Desactivado", integrantes: [] }
    ]);
    renderPage();

    expect(await screen.findByRole("option", { name: "Panteras (eliminado)" })).toBeInTheDocument();
    expect(screen.getByRole("option", { name: "Tiburones (desactivado)" })).toBeInTheDocument();
  });

  it("equipo sin participaciones muestra el vacío", async () => {
    vi.spyOn(puntuacionesApi, "getRendimientoEquipo").mockResolvedValue({ equipoId: GUID, partidas: [] });
    renderPage();
    await screen.findByRole("option", { name: "Los Lobos" });
    await userEvent.selectOptions(screen.getByLabelText("Equipo"), GUID);

    expect(
      await screen.findByText("El equipo no tiene participaciones en partidas terminadas.")
    ).toBeInTheDocument();
  });

  it("volver a la opcion vacia limpia el resultado anterior", async () => {
    vi.spyOn(puntuacionesApi, "getRendimientoEquipo").mockResolvedValue(rendimiento);
    renderPage();
    await screen.findByRole("option", { name: "Los Lobos" });
    const selector = screen.getByLabelText("Equipo");
    await userEvent.selectOptions(selector, GUID);
    expect(await screen.findByTestId("tabla-rendimiento")).toBeInTheDocument();

    await userEvent.selectOptions(selector, "");

    expect(screen.queryByTestId("tabla-rendimiento")).not.toBeInTheDocument();
  });

  it("el selector se bloquea mientras la consulta esta en vuelo", async () => {
    let resolveRequest!: (value: typeof rendimiento) => void;
    vi.spyOn(puntuacionesApi, "getRendimientoEquipo").mockImplementation(
      () => new Promise((resolve) => (resolveRequest = resolve))
    );
    renderPage();
    await screen.findByRole("option", { name: "Los Lobos" });
    const selector = screen.getByLabelText("Equipo");
    await userEvent.selectOptions(selector, GUID);

    expect(selector).toBeDisabled();

    resolveRequest(rendimiento);
    expect(await screen.findByTestId("tabla-rendimiento")).toBeInTheDocument();
    expect(selector).toBeEnabled();
  });

  it("si la lista de equipos falla, cae al ID manual y la pantalla sigue sirviendo", async () => {
    vi.spyOn(adminTeamsApi, "listAdminTeams").mockRejectedValue(new Error("identity caido"));
    vi.spyOn(puntuacionesApi, "getRendimientoEquipo").mockResolvedValue(rendimiento);
    renderPage();

    expect(await screen.findByRole("alert")).toHaveTextContent(/no se pudo cargar la lista/i);
    await userEvent.type(await screen.findByLabelText("ID del equipo"), GUID);
    await userEvent.click(screen.getByText("Consultar"));

    expect(await screen.findByTestId("tabla-rendimiento")).toBeInTheDocument();
  });

  it("en modo manual un GUID invalido no llama a la api", async () => {
    vi.spyOn(adminTeamsApi, "listAdminTeams").mockRejectedValue(new Error("identity caido"));
    const spy = vi.spyOn(puntuacionesApi, "getRendimientoEquipo");
    renderPage();

    await userEvent.type(await screen.findByLabelText("ID del equipo"), "no-es-guid");
    await userEvent.click(screen.getByText("Consultar"));

    expect(await screen.findByText(/ID de equipo válido/)).toBeInTheDocument();
    expect(spy).not.toHaveBeenCalled();
  });

  it("con ?equipoId= válido consulta sola y deja el equipo seleccionado", async () => {
    vi.spyOn(puntuacionesApi, "getRendimientoEquipo").mockResolvedValue(rendimiento);
    renderPage(`/puntuaciones/equipos?equipoId=${GUID}`);

    expect(await screen.findByTestId("tabla-rendimiento")).toBeInTheDocument();
    expect(puntuacionesApi.getRendimientoEquipo).toHaveBeenCalledWith(GUID, "tok");
    await waitFor(() => expect(screen.getByLabelText("Equipo")).toHaveValue(GUID));
  });

  it("un ?equipoId= que no esta en la lista no deja el selector mudo", async () => {
    // Deep-link a un equipo que el listado no trae: el selector debe reflejar que hay algo
    // consultado en vez de aparentar que no se eligio nada.
    const AJENO = "99999999-0000-0000-0000-000000000000";
    vi.spyOn(puntuacionesApi, "getRendimientoEquipo").mockResolvedValue({ equipoId: AJENO, partidas: [] });
    renderPage(`/puntuaciones/equipos?equipoId=${AJENO}`);

    await waitFor(() => expect(screen.getByLabelText("Equipo")).toHaveValue(AJENO));
    expect(screen.getByRole("option", { name: /99999999/ })).toBeInTheDocument();
  });

  it("con ?equipoId= inválido no consulta y deja elegir a mano", async () => {
    const spy = vi.spyOn(puntuacionesApi, "getRendimientoEquipo");
    renderPage("/puntuaciones/equipos?equipoId=no-es-guid");
    await screen.findByRole("option", { name: "Los Lobos" });
    expect(spy).not.toHaveBeenCalled();
  });

  it("muestra el nombre de la partida en vez del GUID corto", async () => {
    vi.spyOn(puntuacionesApi, "getRendimientoEquipo").mockResolvedValue(rendimiento);
    vi.spyOn(partidasApi, "getPartidas").mockResolvedValue([
      { partidaId: PARTIDA_ID, nombrePartida: "Copa UCAB" } as never
    ]);
    renderPage(`/puntuaciones/equipos?equipoId=${GUID}`);

    expect(await screen.findByText("Copa UCAB")).toBeInTheDocument();
  });

  it("mantiene el GUID corto si el listado de partidas falla", async () => {
    vi.spyOn(puntuacionesApi, "getRendimientoEquipo").mockResolvedValue(rendimiento);
    vi.spyOn(partidasApi, "getPartidas").mockRejectedValue(new Error("caido"));
    renderPage(`/puntuaciones/equipos?equipoId=${GUID}`);

    await waitFor(() => expect(screen.getByText(PARTIDA_ID.slice(0, 8))).toBeInTheDocument());
  });
});
