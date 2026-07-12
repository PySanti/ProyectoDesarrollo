import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { HistorialPartidaPage } from "./HistorialPartidaPage";
import * as puntuacionesApi from "../../api/puntuacionesApi";
import { PuntuacionesApiError } from "../../api/puntuacionesApi";

const historial = {
  partidaId: "p1",
  total: 150,
  entradas: [
    {
      occurredAt: "2026-07-10T12:00:00Z",
      tipoEvento: "EtapaBDTGanada",
      juegoId: "abcdef12-0000-0000-0000-000000000000",
      participanteId: "11223344-0000-0000-0000-000000000000",
      equipoId: null,
      detalle: { puntaje: 50 }
    }
  ]
};

function renderPage() {
  return render(
    <MemoryRouter initialEntries={["/partidas/p1/historial"]}>
      <Routes>
        <Route path="/partidas/:partidaId/historial" element={<HistorialPartidaPage accessToken="tok" />} />
      </Routes>
    </MemoryRouter>
  );
}

afterEach(() => vi.restoreAllMocks());

describe("HistorialPartidaPage", () => {
  it("muestra la tabla con eventos y el rango de paginación", async () => {
    vi.spyOn(puntuacionesApi, "getHistorialPartida").mockResolvedValue(historial);
    renderPage();
    expect(await screen.findByTestId("tabla-historial")).toBeInTheDocument();
    expect(screen.getByText("EtapaBDTGanada")).toBeInTheDocument();
    expect(screen.getByText("abcdef12")).toBeInTheDocument();
    expect(screen.getByText(/1–1 de 150/)).toBeInTheDocument();
    expect(screen.getByText('{"puntaje":50}')).toBeInTheDocument();
  });

  it("cambiar el filtro de tipo resetea offset y re-consulta con tipo", async () => {
    const spy = vi.spyOn(puntuacionesApi, "getHistorialPartida").mockResolvedValue(historial);
    renderPage();
    await screen.findByTestId("tabla-historial");
    await userEvent.click(screen.getByText("Siguiente"));
    await userEvent.selectOptions(
      screen.getByLabelText("Filtrar por tipo de evento"),
      "PistaEnviada"
    );
    const ultima = spy.mock.calls[spy.mock.calls.length - 1];
    expect(ultima[2]).toMatchObject({ offset: 0, tipo: "PistaEnviada" });
  });

  it("404 muestra el mensaje de proyección", async () => {
    vi.spyOn(puntuacionesApi, "getHistorialPartida").mockRejectedValue(
      new PuntuacionesApiError("no existe", 404)
    );
    renderPage();
    expect(
      await screen.findByText("La partida no existe en la proyección de Puntuaciones.")
    ).toBeInTheDocument();
  });

  it("200 sin eventos muestra vacío", async () => {
    vi.spyOn(puntuacionesApi, "getHistorialPartida").mockResolvedValue({
      partidaId: "p1",
      total: 0,
      entradas: []
    });
    renderPage();
    expect(await screen.findByText("Sin eventos registrados.")).toBeInTheDocument();
  });
});
