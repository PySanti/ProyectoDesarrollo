import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen, act } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { SesionOperadorPage } from "./SesionOperadorPage";
import {
  getEstadoSesion,
  getLobby,
  iniciarPartida,
  OperacionesApiError,
  type EstadoSesionDto,
  type LobbyDto
} from "../../api/operacionesApi";
import { getPartida, type PartidaDetail } from "../../api/partidasApi";
import { useSesionHub, type SesionHubHandlers } from "./useSesionHub";

vi.mock("../../api/operacionesApi", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../../api/operacionesApi")>();
  return { ...actual, getEstadoSesion: vi.fn(), getLobby: vi.fn(), iniciarPartida: vi.fn() };
});
vi.mock("../../api/partidasApi", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../../api/partidasApi")>();
  return { ...actual, getPartida: vi.fn() };
});
vi.mock("./useSesionHub", () => ({ useSesionHub: vi.fn() }));
vi.mock("./useRankingHub", () => ({ useRankingHub: vi.fn() }));
vi.mock("./TriviaRuntimePanel", () => ({ TriviaRuntimePanel: vi.fn(() => <div data-testid="trivia-runtime-mock" />) }));
vi.mock("./BdtRuntimePanel", () => ({ BdtRuntimePanel: vi.fn(() => <div data-testid="bdt-runtime-mock" />) }));
vi.mock("./PistasPanel", () => ({ PistasPanel: vi.fn(() => <div data-testid="pistas-mock" />) }));
vi.mock("./GeoMapPanel", () => ({ GeoMapPanel: vi.fn(({ ubicaciones }: { ubicaciones: unknown[] }) => <div data-testid="geo-mock">{ubicaciones.length}</div>) }));

const estadoLobby: EstadoSesionDto = {
  partidaId: "p1",
  sesionPartidaId: "s1",
  estado: "Lobby",
  modalidad: "Individual",
  juegos: []
};
const lobby: LobbyDto = {
  partidaId: "p1",
  sesionPartidaId: "s1",
  estado: "Lobby",
  modalidad: "Individual",
  minimosParticipacion: 2,
  maximosParticipacion: 10,
  inscritosActivos: 3,
  participantes: [],
  equipos: []
};
const configManual: PartidaDetail = {
  partidaId: "p1",
  nombrePartida: "Copa",
  modalidad: "Individual",
  modoInicioPartida: "Manual",
  tiempoInicio: null,
  minimosParticipacion: 2,
  maximosParticipacion: 10,
  estado: "Lobby",
  juegos: []
};

function renderPage({ puedeOperar = true }: { puedeOperar?: boolean } = {}) {
  return render(
    <MemoryRouter initialEntries={["/partidas/p1/sesion"]}>
      <Routes>
        <Route
          path="/partidas/:partidaId/sesion"
          element={<SesionOperadorPage accessToken="tok" puedeOperar={puedeOperar} />}
        />
        <Route path="/partidas/:partidaId" element={<div>DETALLE</div>} />
      </Routes>
    </MemoryRouter>
  );
}

describe("SesionOperadorPage", () => {
  beforeEach(() => {
    vi.mocked(getEstadoSesion).mockReset();
    vi.mocked(getLobby).mockReset();
    vi.mocked(iniciarPartida).mockReset();
    vi.mocked(getPartida).mockReset();
  });

  it("en Lobby (modo Manual) muestra inscritos y el boton Iniciar", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue(estadoLobby);
    vi.mocked(getLobby).mockResolvedValue(lobby);
    vi.mocked(getPartida).mockResolvedValue(configManual);
    renderPage();

    expect(await screen.findByTestId("lobby-panel")).toBeInTheDocument();
    expect(screen.getByTestId("lobby-inscritos")).toHaveTextContent("3");
    expect(screen.getByTestId("btn-iniciar")).toBeInTheDocument();
    expect(screen.queryByTestId("inicio-countdown")).not.toBeInTheDocument();
  });

  it("inicio manual que devuelve Iniciada muestra el shell con el juego actual", async () => {
    vi.mocked(getEstadoSesion)
      .mockResolvedValueOnce(estadoLobby)
      .mockResolvedValue({
        ...estadoLobby,
        estado: "Iniciada",
        juegos: [
          { juegoId: "j1", orden: 1, tipoJuego: "Trivia", estado: "Activo" },
          { juegoId: "j2", orden: 2, tipoJuego: "BusquedaDelTesoro", estado: "Pendiente" }
        ],
        juegoActualOrden: 1
      });
    vi.mocked(getLobby).mockResolvedValue(lobby);
    vi.mocked(getPartida).mockResolvedValue(configManual);
    vi.mocked(iniciarPartida).mockResolvedValue({ partidaId: "p1", estado: "Iniciada" });
    renderPage();

    await userEvent.click(await screen.findByTestId("btn-iniciar"));

    expect(await screen.findByTestId("sesion-iniciada")).toBeInTheDocument();
    expect(screen.getByTestId("juego-actual")).toHaveTextContent("1");
  });

  it("inicio manual que devuelve Cancelada muestra la pantalla de minimos no alcanzados", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue(estadoLobby);
    vi.mocked(getLobby).mockResolvedValue(lobby);
    vi.mocked(getPartida).mockResolvedValue(configManual);
    vi.mocked(iniciarPartida).mockResolvedValue({ partidaId: "p1", estado: "Cancelada" });
    renderPage();

    await userEvent.click(await screen.findByTestId("btn-iniciar"));
    expect(await screen.findByTestId("sesion-cancelada")).toBeInTheDocument();
  });

  it("modo Automatico muestra countdown y no muestra boton Iniciar", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue(estadoLobby);
    vi.mocked(getLobby).mockResolvedValue(lobby);
    vi.mocked(getPartida).mockResolvedValue({
      ...configManual,
      modoInicioPartida: "Automatico",
      tiempoInicio: new Date(Date.now() + 60000).toISOString()
    });
    renderPage();

    expect(await screen.findByTestId("inicio-countdown")).toBeInTheDocument();
    expect(screen.queryByTestId("btn-iniciar")).not.toBeInTheDocument();
  });

  it("cuando la sesion no existe (404) muestra 'no publicada' con link al detalle", async () => {
    vi.mocked(getEstadoSesion).mockRejectedValue(new OperacionesApiError("no publicada", 404));
    renderPage();

    expect(await screen.findByTestId("sesion-no-publicada")).toBeInTheDocument();
    const link = screen.getByRole("link", { name: /detalle|partida/i });
    expect(link).toHaveAttribute("href", "/partidas/p1");
  });

  it("carga directa con estado Iniciada renderiza el shell sin pasar por lobby", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue({
      ...estadoLobby,
      estado: "Iniciada",
      juegos: [{ juegoId: "j1", orden: 1, tipoJuego: "Trivia", estado: "Activo" }],
      juegoActualOrden: 1
    });
    vi.mocked(getPartida).mockResolvedValue(configManual);
    renderPage();

    expect(await screen.findByTestId("sesion-iniciada")).toBeInTheDocument();
    expect(vi.mocked(getLobby)).not.toHaveBeenCalled();
  });

  it("con juego actual Trivia monta el panel de runtime", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue({
      ...estadoLobby,
      estado: "Iniciada",
      juegos: [{ juegoId: "j1", orden: 1, tipoJuego: "Trivia", estado: "Activo" }],
      juegoActualOrden: 1
    });
    vi.mocked(getPartida).mockResolvedValue(configManual);
    renderPage();
    expect(await screen.findByTestId("trivia-runtime-mock")).toBeInTheDocument();
  });

  it("con juego actual BDT monta BdtRuntimePanel, PistasPanel y GeoMapPanel (no el placeholder)", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue({
      ...estadoLobby, estado: "Iniciada",
      juegos: [{ juegoId: "j1", orden: 1, tipoJuego: "BusquedaDelTesoro", estado: "Activo" }],
      juegoActualOrden: 1
    });
    vi.mocked(getPartida).mockResolvedValue(configManual);
    renderPage();
    expect(await screen.findByTestId("bdt-runtime-mock")).toBeInTheDocument();
    expect(screen.getByTestId("pistas-mock")).toBeInTheDocument();
    expect(screen.getByTestId("geo-mock")).toBeInTheDocument();
    expect(screen.queryByTestId("runtime-bdt-placeholder")).not.toBeInTheDocument();
  });

  it("un push UbicacionActualizada alimenta el GeoMapPanel", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue({
      ...estadoLobby, estado: "Iniciada",
      juegos: [{ juegoId: "j1", orden: 1, tipoJuego: "BusquedaDelTesoro", estado: "Activo" }],
      juegoActualOrden: 1
    });
    vi.mocked(getPartida).mockResolvedValue(configManual);
    let capturedHandlers: SesionHubHandlers = {};
    vi.mocked(useSesionHub).mockImplementation((_id, _tok, handlers) => { capturedHandlers = handlers; });
    renderPage();
    await screen.findByTestId("geo-mock");
    expect(screen.getByTestId("geo-mock")).toHaveTextContent("0");
    await act(async () => {
      capturedHandlers.onUbicacionActualizada?.({ partidaId: "p1", participanteId: "u1", latitud: 10, longitud: 20, timestampUtc: new Date().toISOString() });
    });
    expect(screen.getByTestId("geo-mock")).toHaveTextContent("1");
  });

  it("pinta pills por estado del juego (Activo live, Finalizado done, Pendiente lobby)", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue({
      ...estadoLobby,
      estado: "Iniciada",
      juegos: [
        { juegoId: "j1", orden: 1, tipoJuego: "Trivia", estado: "Finalizado" },
        { juegoId: "j2", orden: 2, tipoJuego: "Trivia", estado: "Activo" },
        { juegoId: "j3", orden: 3, tipoJuego: "BusquedaDelTesoro", estado: "Pendiente" }
      ],
      juegoActualOrden: 2
    });
    vi.mocked(getPartida).mockResolvedValue(configManual);
    renderPage();
    const actual = await screen.findByTestId("juego-actual");
    expect(actual.className).toContain("pill--live");
    expect(screen.getByText(/Juego 1/).closest(".pill")?.className).toContain("pill--done");
    expect(screen.getByText(/Juego 3/).closest(".pill")?.className).toContain("pill--lobby");
  });

  it("seq-guard: una carga vieja que resuelve tarde no pisa la vista nueva", async () => {
    // 1a carga: lenta, resolvera Lobby. 2a carga (via push onIniciada): rapida, Iniciada.
    let resolveSlow: (v: EstadoSesionDto) => void;
    const slow = new Promise<EstadoSesionDto>((res) => (resolveSlow = res));
    vi.mocked(getEstadoSesion)
      .mockReturnValueOnce(slow)
      .mockResolvedValueOnce({
        ...estadoLobby,
        estado: "Iniciada",
        juegos: [{ juegoId: "j1", orden: 1, tipoJuego: "Trivia", estado: "Activo" }],
        juegoActualOrden: 1
      });
    vi.mocked(getPartida).mockResolvedValue(configManual);
    vi.mocked(getLobby).mockResolvedValue(lobby);

    let capturedHandlers: SesionHubHandlers = {};
    vi.mocked(useSesionHub).mockImplementation((_id, _tok, handlers) => {
      capturedHandlers = handlers;
    });

    renderPage();
    // Segunda carga completa (push) mientras la primera sigue pendiente:
    await act(async () => {
      capturedHandlers.onIniciada?.({ partidaId: "p1" });
    });
    expect(await screen.findByTestId("sesion-iniciada")).toBeInTheDocument();
    // Ahora resuelve la carga vieja con Lobby: NO debe pisar la vista iniciada.
    await act(async () => {
      resolveSlow!(estadoLobby);
    });
    expect(screen.getByTestId("sesion-iniciada")).toBeInTheDocument();
    expect(screen.queryByTestId("lobby-panel")).not.toBeInTheDocument();
  });

  it("seq-guard: un push de cancelacion invalida una carga en vuelo", async () => {
    let resolveSlow: (v: EstadoSesionDto) => void;
    const slow = new Promise<EstadoSesionDto>((res) => (resolveSlow = res));
    vi.mocked(getEstadoSesion).mockReturnValueOnce(slow);
    vi.mocked(getPartida).mockResolvedValue(configManual);
    vi.mocked(getLobby).mockResolvedValue(lobby);

    let capturedHandlers: SesionHubHandlers = {};
    vi.mocked(useSesionHub).mockImplementation((_id, _tok, handlers) => {
      capturedHandlers = handlers;
    });

    renderPage();
    await act(async () => {
      capturedHandlers.onCancelada?.({ partidaId: "p1", motivo: "CanceladaPorOperador" });
    });
    expect(await screen.findByTestId("sesion-cancelada")).toBeInTheDocument();

    // La carga vieja resuelve con datos pre-cancelacion: NO debe revivir la vista.
    await act(async () => {
      resolveSlow!({
        ...estadoLobby,
        estado: "Iniciada",
        juegos: [{ juegoId: "j1", orden: 1, tipoJuego: "Trivia", estado: "Activo" }],
        juegoActualOrden: 1
      });
    });
    expect(screen.getByTestId("sesion-cancelada")).toBeInTheDocument();
    expect(screen.queryByTestId("sesion-iniciada")).not.toBeInTheDocument();
  });

  it("oculta 'Iniciar ahora' cuando puedeOperar es false", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue(estadoLobby);
    vi.mocked(getLobby).mockResolvedValue(lobby);
    vi.mocked(getPartida).mockResolvedValue(configManual);
    renderPage({ puedeOperar: false });

    expect(await screen.findByTestId("lobby-panel")).toBeInTheDocument();
    expect(screen.queryByTestId("btn-iniciar")).toBeNull();
  });

  it("oculta el panel de pistas cuando puedeOperar es false, pero el mapa sigue visible", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue({
      ...estadoLobby, estado: "Iniciada",
      juegos: [{ juegoId: "j1", orden: 1, tipoJuego: "BusquedaDelTesoro", estado: "Activo" }],
      juegoActualOrden: 1
    });
    vi.mocked(getPartida).mockResolvedValue(configManual);
    renderPage({ puedeOperar: false });

    expect(await screen.findByTestId("bdt-runtime-mock")).toBeInTheDocument();
    expect(screen.getByTestId("geo-mock")).toBeInTheDocument();
    expect(screen.queryByTestId("pistas-mock")).toBeNull();
  });
});
