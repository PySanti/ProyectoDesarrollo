import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { RendimientoEquipoPage } from "./RendimientoEquipoPage";
import * as puntuacionesApi from "../../api/puntuacionesApi";

const GUID = "11111111-2222-3333-4444-555555555555";
const OTHER_GUID = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";

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
  it("GUID inválido muestra error de formato sin llamar la api", async () => {
    const spy = vi.spyOn(puntuacionesApi, "getRendimientoEquipo");
    renderPage();
    await userEvent.type(screen.getByLabelText("ID del equipo"), "no-es-guid");
    await userEvent.click(screen.getByText("Consultar"));
    expect(await screen.findByText(/ID de equipo válido/)).toBeInTheDocument();
    expect(spy).not.toHaveBeenCalled();
  });

  it("consulta y muestra la tabla de partidas", async () => {
    vi.spyOn(puntuacionesApi, "getRendimientoEquipo").mockResolvedValue(rendimiento);
    renderPage();
    await userEvent.type(screen.getByLabelText("ID del equipo"), GUID);
    await userEvent.click(screen.getByText("Consultar"));
    expect(await screen.findByTestId("tabla-rendimiento")).toBeInTheDocument();
    expect(screen.getByText("aabbccdd")).toBeInTheDocument();
    expect(screen.getByText("✓")).toHaveAttribute("aria-label", "Sí");
    expect(screen.getByText("—")).toHaveAttribute("aria-label", "No");
  });

  it("equipo sin participaciones muestra el vacío", async () => {
    vi.spyOn(puntuacionesApi, "getRendimientoEquipo").mockResolvedValue({
      equipoId: GUID,
      partidas: []
    });
    renderPage();
    await userEvent.type(screen.getByLabelText("ID del equipo"), GUID);
    await userEvent.click(screen.getByText("Consultar"));
    expect(
      await screen.findByText("El equipo no tiene participaciones en partidas terminadas.")
    ).toBeInTheDocument();
  });

  it("clears a prior result and error when the team ID changes", async () => {
    vi.spyOn(puntuacionesApi, "getRendimientoEquipo").mockResolvedValue(rendimiento);
    renderPage();
    const input = screen.getByLabelText("ID del equipo");
    await userEvent.type(input, GUID);
    await userEvent.click(screen.getByText("Consultar"));
    expect(await screen.findByTestId("tabla-rendimiento")).toBeInTheDocument();

    await userEvent.clear(input);
    await userEvent.type(input, OTHER_GUID);

    expect(screen.queryByTestId("tabla-rendimiento")).not.toBeInTheDocument();
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });

  it("leaves no prior result visible after an invalid submission", async () => {
    vi.spyOn(puntuacionesApi, "getRendimientoEquipo").mockResolvedValue(rendimiento);
    renderPage();
    const input = screen.getByLabelText("ID del equipo");
    await userEvent.type(input, GUID);
    await userEvent.click(screen.getByText("Consultar"));
    expect(await screen.findByTestId("tabla-rendimiento")).toBeInTheDocument();

    await userEvent.clear(input);
    await userEvent.type(input, "no-es-guid");
    await userEvent.click(screen.getByText("Consultar"));

    expect(await screen.findByText(/ID de equipo válido/)).toBeInTheDocument();
    expect(screen.queryByTestId("tabla-rendimiento")).not.toBeInTheDocument();
  });

  it("disables the team ID input while the request is in flight", async () => {
    let resolveRequest!: (value: typeof rendimiento) => void;
    vi.spyOn(puntuacionesApi, "getRendimientoEquipo").mockImplementation(
      () => new Promise((resolve) => (resolveRequest = resolve))
    );
    renderPage();
    const input = screen.getByLabelText("ID del equipo");
    await userEvent.type(input, GUID);
    await userEvent.click(screen.getByText("Consultar"));

    expect(input).toBeDisabled();

    resolveRequest(rendimiento);
    expect(await screen.findByTestId("tabla-rendimiento")).toBeInTheDocument();
    expect(input).toBeEnabled();
  });

  it("con ?equipoId= válido precarga el campo y consulta sola", async () => {
    vi.spyOn(puntuacionesApi, "getRendimientoEquipo").mockResolvedValue(rendimiento);
    renderPage(`/puntuaciones/equipos?equipoId=${GUID}`);
    expect(await screen.findByTestId("tabla-rendimiento")).toBeInTheDocument();
    expect(screen.getByLabelText("ID del equipo")).toHaveValue(GUID);
    expect(puntuacionesApi.getRendimientoEquipo).toHaveBeenCalledWith(GUID, "tok");
  });

  it("con ?equipoId= inválido no consulta y deja el flujo manual", () => {
    const spy = vi.spyOn(puntuacionesApi, "getRendimientoEquipo");
    renderPage("/puntuaciones/equipos?equipoId=no-es-guid");
    expect(spy).not.toHaveBeenCalled();
  });
});
