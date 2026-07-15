import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { RendimientoEquipoPage } from "./RendimientoEquipoPage";
import * as puntuacionesApi from "../../api/puntuacionesApi";
import * as identityApi from "../../api/identityApi";

const GUID = "11111111-2222-3333-4444-555555555555";
const OTHER_GUID = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";

const equipos: identityApi.EquipoAdminItem[] = [
  { equipoId: GUID, nombreEquipo: "Los Ganadores", estado: "Activo", participantes: [] },
  { equipoId: OTHER_GUID, nombreEquipo: "Los Retadores", estado: "Activo", participantes: [] }
];

function mockEquipos() {
  return vi.spyOn(identityApi, "getEquipos").mockResolvedValue(equipos);
}

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
      partidaId: "aabbccdd-0000-0000-0000-000000000000",
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

afterEach(() => vi.restoreAllMocks());

describe("RendimientoEquipoPage", () => {
  it("muestra el subtítulo del panel", async () => {
    mockEquipos();
    renderPage();
    expect(
      await screen.findByText("Panel para consulta de rendimiento de equipos")
    ).toBeInTheDocument();
  });

  it("lista los equipos en el selector y consulta al seleccionar uno", async () => {
    mockEquipos();
    const spy = vi.spyOn(puntuacionesApi, "getRendimientoEquipo").mockResolvedValue(rendimiento);
    renderPage();
    // Opción disponible una vez cargada la lista.
    await screen.findByRole("option", { name: "Los Ganadores" });
    await userEvent.selectOptions(screen.getByLabelText("Equipo"), GUID);
    expect(await screen.findByTestId("tabla-rendimiento")).toBeInTheDocument();
    expect(spy).toHaveBeenCalledWith(GUID, "tok");
    expect(screen.getByText("aabbccdd")).toBeInTheDocument();
    expect(screen.getByText("✓")).toHaveAttribute("aria-label", "Sí");
    expect(screen.getByText("—")).toHaveAttribute("aria-label", "No");
  });

  it("equipo sin participaciones muestra el vacío", async () => {
    mockEquipos();
    vi.spyOn(puntuacionesApi, "getRendimientoEquipo").mockResolvedValue({
      equipoId: GUID,
      partidas: []
    });
    renderPage();
    await screen.findByRole("option", { name: "Los Ganadores" });
    await userEvent.selectOptions(screen.getByLabelText("Equipo"), GUID);
    expect(
      await screen.findByText("El equipo no tiene participaciones en partidas terminadas.")
    ).toBeInTheDocument();
  });

  it("volver a 'Selecciona un equipo…' limpia el resultado", async () => {
    mockEquipos();
    vi.spyOn(puntuacionesApi, "getRendimientoEquipo").mockResolvedValue(rendimiento);
    renderPage();
    await screen.findByRole("option", { name: "Los Ganadores" });
    const select = screen.getByLabelText("Equipo");
    await userEvent.selectOptions(select, GUID);
    expect(await screen.findByTestId("tabla-rendimiento")).toBeInTheDocument();

    await userEvent.selectOptions(select, "");
    expect(screen.queryByTestId("tabla-rendimiento")).not.toBeInTheDocument();
  });

  it("deshabilita el selector mientras la consulta está en vuelo", async () => {
    mockEquipos();
    let resolveRequest!: (value: typeof rendimiento) => void;
    vi.spyOn(puntuacionesApi, "getRendimientoEquipo").mockImplementation(
      () => new Promise((resolve) => (resolveRequest = resolve))
    );
    renderPage();
    await screen.findByRole("option", { name: "Los Ganadores" });
    const select = screen.getByLabelText("Equipo");
    await userEvent.selectOptions(select, GUID);
    expect(select).toBeDisabled();

    resolveRequest(rendimiento);
    expect(await screen.findByTestId("tabla-rendimiento")).toBeInTheDocument();
    expect(select).toBeEnabled();
  });

  it("muestra error si la lista de equipos no carga", async () => {
    vi.spyOn(identityApi, "getEquipos").mockRejectedValue(
      new identityApi.IdentityApiError("Sin permiso", 403)
    );
    renderPage();
    expect(await screen.findByRole("alert")).toHaveTextContent("Sin permiso");
  });

  it("con ?equipoId= válido consulta sola al montar", async () => {
    mockEquipos();
    vi.spyOn(puntuacionesApi, "getRendimientoEquipo").mockResolvedValue(rendimiento);
    renderPage(`/puntuaciones/equipos?equipoId=${GUID}`);
    expect(await screen.findByTestId("tabla-rendimiento")).toBeInTheDocument();
    expect(puntuacionesApi.getRendimientoEquipo).toHaveBeenCalledWith(GUID, "tok");
  });

  it("con ?equipoId= inválido no consulta y deja el flujo manual", async () => {
    mockEquipos();
    const spy = vi.spyOn(puntuacionesApi, "getRendimientoEquipo");
    renderPage("/puntuaciones/equipos?equipoId=no-es-guid");
    await screen.findByRole("option", { name: "Los Ganadores" });
    expect(spy).not.toHaveBeenCalled();
  });
});
