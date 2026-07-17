import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { PartidasListPage } from "./PartidasListPage";
import { getPartidas, PartidasApiError, type PartidaSummary } from "../../api/partidasApi";

vi.mock("../../api/partidasApi", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../../api/partidasApi")>();
  return { ...actual, getPartidas: vi.fn() };
});

const getPartidasMock = vi.mocked(getPartidas);

function renderPage({ puedeOperar = true }: { puedeOperar?: boolean } = {}) {
  return render(
    <MemoryRouter>
      <PartidasListPage accessToken="token" puedeOperar={puedeOperar} />
    </MemoryRouter>
  );
}

// Las fechas no son arbitrarias: la publicada (12:00) es más nueva que la sin publicar
// (09:00), y el mock las devuelve en ese orden — como haría el backend ya ordenado. El
// test de orden se apoya en eso.
const summaryPublicada: PartidaSummary = {
  partidaId: "p1",
  nombrePartida: "Trivia de verano",
  modalidad: "Individual",
  modoInicioPartida: "Manual",
  tiempoInicio: null,
  minimosParticipacion: 1,
  maximosParticipacion: 10,
  estado: "Lobby",
  cantidadJuegos: 2,
  fechaCreacion: "2026-07-16T12:00:00Z"
};

const summarySinPublicar: PartidaSummary = {
  partidaId: "p2",
  nombrePartida: "BDT campus",
  modalidad: "Equipo",
  modoInicioPartida: "Automatico",
  tiempoInicio: "2026-08-01T10:00:00Z",
  minimosParticipacion: 2,
  maximosParticipacion: 6,
  estado: null,
  cantidadJuegos: 1,
  fechaCreacion: "2026-07-16T09:00:00Z"
};

describe("PartidasListPage", () => {
  beforeEach(() => {
    getPartidasMock.mockReset();
  });

  it("muestra los nombres de las partidas y 'Sin publicar' cuando estado es null", async () => {
    getPartidasMock.mockResolvedValueOnce([summaryPublicada, summarySinPublicar]);
    renderPage();

    expect(await screen.findByText("Trivia de verano")).toBeInTheDocument();
    expect(screen.getByText("BDT campus")).toBeInTheDocument();
    expect(screen.getByText("Sin publicar")).toBeInTheDocument();
    expect(screen.getByTestId("fila-partida-p1")).toBeInTheDocument();
    expect(screen.getByTestId("fila-partida-p2")).toBeInTheDocument();
    expect(screen.getByTestId("btn-nueva-partida")).toBeInTheDocument();
    expect(screen.getByTestId("lista-partidas")).toBeInTheDocument();
  });

  it("muestra la fecha y hora de creacion de cada partida", async () => {
    getPartidasMock.mockResolvedValueOnce([summaryPublicada]);
    renderPage();

    await screen.findByTestId("fila-partida-p1");
    // toLocaleString, no toLocaleDateString: sin la hora el operador ve varias partidas
    // del mismo dia y el orden vuelve a parecer arbitrario.
    //
    // textContent y no toHaveTextContent: en es-VE el string trae un espacio duro (U+00A0)
    // antes de "m.". toHaveTextContent normaliza el texto del elemento pero no el esperado,
    // asi que nunca casarian — y el diff los pinta identicos porque el caracter es invisible.
    const celda = screen.getByTestId("fecha-creacion-p1");
    expect(celda.textContent).toBe(new Date("2026-07-16T12:00:00Z").toLocaleString());
  });

  it("respeta el orden que llega del backend y no reordena", async () => {
    getPartidasMock.mockResolvedValueOnce([summaryPublicada, summarySinPublicar]);
    renderPage();

    await screen.findByTestId("fila-partida-p1");
    const filas = screen.getAllByTestId(/^fila-partida-/);
    expect(filas[0]).toHaveTextContent("Trivia de verano");
    expect(filas[1]).toHaveTextContent("BDT campus");
  });

  it("muestra el estado vacio cuando no hay partidas", async () => {
    getPartidasMock.mockResolvedValueOnce([]);
    renderPage();

    expect(await screen.findByText(/no hay partidas/i)).toBeInTheDocument();
  });

  it("muestra un aviso de error cuando la API falla", async () => {
    getPartidasMock.mockRejectedValueOnce(new PartidasApiError("boom", 500));
    renderPage();

    const notice = await screen.findByRole("alert");
    expect(notice).toBeInTheDocument();
  });

  it("oculta 'Nueva partida' cuando puedeOperar es false", async () => {
    getPartidasMock.mockResolvedValueOnce([summaryPublicada]);
    renderPage({ puedeOperar: false });

    expect(await screen.findByTestId("lista-partidas")).toBeInTheDocument();
    expect(screen.queryByTestId("btn-nueva-partida")).toBeNull();
  });

  it("muestra 'Nueva partida' cuando puedeOperar es true", async () => {
    getPartidasMock.mockResolvedValueOnce([summaryPublicada]);
    renderPage({ puedeOperar: true });

    expect(await screen.findByTestId("btn-nueva-partida")).toBeInTheDocument();
  });
});
