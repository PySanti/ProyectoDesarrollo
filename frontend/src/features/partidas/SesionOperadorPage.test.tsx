import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen, act } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { SesionOperadorPage } from "./SesionOperadorPage";
import {
  aceptarInscripcion,
  cancelarPartida,
  getEstadoSesion,
  getLobby,
  iniciarPartida,
  rechazarInscripcion,
  OperacionesApiError,
  type CancelacionPartidaResponse,
  type EstadoSesionDto,
  type LobbyDto
} from "../../api/operacionesApi";
import { getPartida, type PartidaDetail } from "../../api/partidasApi";
import { useSesionHub, type SesionHubHandlers } from "./useSesionHub";

vi.mock("../../api/operacionesApi", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../../api/operacionesApi")>();
  return {
    ...actual,
    getEstadoSesion: vi.fn(),
    getLobby: vi.fn(),
    iniciarPartida: vi.fn(),
    aceptarInscripcion: vi.fn(),
    rechazarInscripcion: vi.fn(),
    cancelarPartida: vi.fn()
  };
});
vi.mock("../../api/partidasApi", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../../api/partidasApi")>();
  return { ...actual, getPartida: vi.fn() };
});
vi.mock("./useSesionHub", () => ({ useSesionHub: vi.fn() }));
vi.mock("./useRankingHub", () => ({ useRankingHub: vi.fn() }));
vi.mock("./TriviaRuntimePanel", () => ({ TriviaRuntimePanel: vi.fn(() => <div data-testid="trivia-runtime-mock" />) }));
vi.mock("./BdtRuntimePanel", () => ({
  BdtRuntimePanel: vi.fn(({ resultadosEtapas }: { resultadosEtapas?: unknown[] }) => (
    <div data-testid="bdt-runtime-mock">{resultadosEtapas?.length ?? 0}</div>
  ))
}));
vi.mock("./PistasPanel", () => ({ PistasPanel: vi.fn(() => <div data-testid="pistas-mock" />) }));
vi.mock("./EnviosTesoroPanel", () => ({ EnviosTesoroPanel: vi.fn(() => <div data-testid="envios-tesoro-mock" />) }));
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
  equipos: [],
  solicitudesPendientesIndividual: [],
  solicitudesPendientesEquipo: []
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
    vi.mocked(aceptarInscripcion).mockReset();
    vi.mocked(rechazarInscripcion).mockReset();
    vi.mocked(cancelarPartida).mockReset();
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

  it("en modalidad Individual lista los participantes inscritos (IDs crudos)", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue(estadoLobby);
    vi.mocked(getLobby).mockResolvedValue({ ...lobby, participantes: ["u1", "u2"] });
    vi.mocked(getPartida).mockResolvedValue(configManual);
    renderPage();

    const lista = await screen.findByTestId("lobby-participantes");
    expect(lista).toHaveTextContent("u1");
    expect(lista).toHaveTextContent("u2");
  });

  it("sin participantes inscritos no renderiza la lista", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue(estadoLobby);
    vi.mocked(getLobby).mockResolvedValue(lobby);
    vi.mocked(getPartida).mockResolvedValue(configManual);
    renderPage();

    await screen.findByTestId("lobby-panel");
    expect(screen.queryByTestId("lobby-participantes")).toBeNull();
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
    const vista = await screen.findByTestId("sesion-cancelada");
    expect(vista).toHaveTextContent("La partida fue cancelada.");
    expect(vista).not.toHaveTextContent("mínimos de participación no alcanzados");
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
    expect(screen.getByTestId("envios-tesoro-mock")).toBeInTheDocument();
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
    const vistaCancelada = await screen.findByTestId("sesion-cancelada");
    expect(vistaCancelada).toHaveTextContent("La partida fue cancelada.");
    expect(vistaCancelada).toHaveTextContent("Cancelada por el operador.");

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

  it("push de cancelacion con motivo MinimosNoAlcanzados muestra el texto legible correspondiente", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue(estadoLobby);
    vi.mocked(getLobby).mockResolvedValue(lobby);
    vi.mocked(getPartida).mockResolvedValue(configManual);

    let capturedHandlers: SesionHubHandlers = {};
    vi.mocked(useSesionHub).mockImplementation((_id, _tok, handlers) => {
      capturedHandlers = handlers;
    });

    renderPage();
    await act(async () => {
      capturedHandlers.onCancelada?.({ partidaId: "p1", motivo: "MinimosNoAlcanzados" });
    });
    const vistaCancelada = await screen.findByTestId("sesion-cancelada");
    expect(vistaCancelada).toHaveTextContent("La partida fue cancelada.");
    expect(vistaCancelada).toHaveTextContent("Mínimos de participación no alcanzados.");
  });

  it("oculta 'Iniciar ahora' cuando puedeOperar es false", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue(estadoLobby);
    vi.mocked(getLobby).mockResolvedValue(lobby);
    vi.mocked(getPartida).mockResolvedValue(configManual);
    renderPage({ puedeOperar: false });

    expect(await screen.findByTestId("lobby-panel")).toBeInTheDocument();
    expect(screen.queryByTestId("btn-iniciar")).toBeNull();
  });

  it("muestra las solicitudes pendientes con botones para el operador", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue(estadoLobby);
    vi.mocked(getLobby).mockResolvedValue({
      ...lobby,
      solicitudesPendientesIndividual: [{ inscripcionId: "i1", participanteId: "u1", fechaInscripcion: "2026-07-12T10:00:00Z" }]
    });
    vi.mocked(getPartida).mockResolvedValue(configManual);
    renderPage({ puedeOperar: true });

    expect(await screen.findByTestId("solicitudes-panel")).toBeInTheDocument();
    expect(screen.getAllByTestId("btn-aceptar-solicitud")).toHaveLength(1);
    expect(screen.getAllByTestId("btn-rechazar-solicitud")).toHaveLength(1);
  });

  it("oculta los botones de aprobar/rechazar al admin observador (puedeOperar=false) pero muestra la lista", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue(estadoLobby);
    vi.mocked(getLobby).mockResolvedValue({
      ...lobby,
      solicitudesPendientesIndividual: [{ inscripcionId: "i1", participanteId: "u1", fechaInscripcion: "2026-07-12T10:00:00Z" }]
    });
    vi.mocked(getPartida).mockResolvedValue(configManual);
    renderPage({ puedeOperar: false });

    expect(await screen.findByTestId("solicitudes-panel")).toBeInTheDocument();
    expect(screen.queryByTestId("btn-aceptar-solicitud")).toBeNull();
    expect(screen.queryByTestId("btn-rechazar-solicitud")).toBeNull();
  });

  it("sin solicitudes pendientes no renderiza el panel", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue(estadoLobby);
    vi.mocked(getLobby).mockResolvedValue(lobby);
    vi.mocked(getPartida).mockResolvedValue(configManual);
    renderPage({ puedeOperar: true });

    expect(await screen.findByTestId("lobby-panel")).toBeInTheDocument();
    expect(screen.queryByTestId("solicitudes-panel")).toBeNull();
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
    // El panel de envios de tesoro es de solo lectura: visible tambien al admin observador.
    expect(screen.getByTestId("envios-tesoro-mock")).toBeInTheDocument();
  });

  it("un push EtapaCerrada/EtapaGanada acumula el historico y se lo pasa a BdtRuntimePanel", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue({
      ...estadoLobby, estado: "Iniciada",
      juegos: [{ juegoId: "j1", orden: 1, tipoJuego: "BusquedaDelTesoro", estado: "Activo" }],
      juegoActualOrden: 1
    });
    vi.mocked(getPartida).mockResolvedValue(configManual);
    let capturedHandlers: SesionHubHandlers = {};
    vi.mocked(useSesionHub).mockImplementation((_id, _tok, handlers) => { capturedHandlers = handlers; });
    renderPage();
    await screen.findByTestId("bdt-runtime-mock");
    expect(screen.getByTestId("bdt-runtime-mock")).toHaveTextContent("0");

    await act(async () => {
      capturedHandlers.onEtapaCerrada?.({ partidaId: "p1", juegoId: "j1", etapaId: "e1", ganadorEquipoId: "eq1" });
    });
    expect(screen.getByTestId("bdt-runtime-mock")).toHaveTextContent("1");

    await act(async () => {
      capturedHandlers.onEtapaGanada?.({ partidaId: "p1", juegoId: "j1", etapaId: "e2", ganadorParticipanteId: "u1" });
    });
    expect(screen.getByTestId("bdt-runtime-mock")).toHaveTextContent("2");
  });

  it("EtapaCerrada y EtapaGanada de la MISMA etapa colapsan en una sola entrada (dedup por etapaId)", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue({
      ...estadoLobby, estado: "Iniciada",
      juegos: [{ juegoId: "j1", orden: 1, tipoJuego: "BusquedaDelTesoro", estado: "Activo" }],
      juegoActualOrden: 1
    });
    vi.mocked(getPartida).mockResolvedValue(configManual);
    let capturedHandlers: SesionHubHandlers = {};
    vi.mocked(useSesionHub).mockImplementation((_id, _tok, handlers) => { capturedHandlers = handlers; });
    renderPage();
    await screen.findByTestId("bdt-runtime-mock");

    // La misma etapa cierra y luego se resuelve el ganador: ambos eventos portan el mismo etapaId.
    await act(async () => {
      capturedHandlers.onEtapaCerrada?.({ partidaId: "p1", juegoId: "j1", etapaId: "e1" });
      capturedHandlers.onEtapaGanada?.({ partidaId: "p1", juegoId: "j1", etapaId: "e1", ganadorEquipoId: "eq1" });
    });
    expect(screen.getByTestId("bdt-runtime-mock")).toHaveTextContent("1");
  });

  it("clic en aceptar solicitud llama a aceptarInscripcion y aplica el lobby devuelto", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue(estadoLobby);
    vi.mocked(getLobby).mockResolvedValue({
      ...lobby,
      solicitudesPendientesIndividual: [{ inscripcionId: "i1", participanteId: "u1", fechaInscripcion: "2026-07-12T10:00:00Z" }]
    });
    vi.mocked(getPartida).mockResolvedValue(configManual);
    vi.mocked(aceptarInscripcion).mockResolvedValue(lobby);
    renderPage({ puedeOperar: true });

    await userEvent.click(await screen.findByTestId("btn-aceptar-solicitud"));

    expect(aceptarInscripcion).toHaveBeenCalledWith("p1", "i1", "tok");
    await screen.findByTestId("lobby-panel");
    expect(screen.queryByTestId("solicitudes-panel")).toBeNull();
  });

  it("clic en rechazar solicitud llama a rechazarInscripcion y aplica el lobby devuelto", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue(estadoLobby);
    vi.mocked(getLobby).mockResolvedValue({
      ...lobby,
      solicitudesPendientesIndividual: [{ inscripcionId: "i1", participanteId: "u1", fechaInscripcion: "2026-07-12T10:00:00Z" }]
    });
    vi.mocked(getPartida).mockResolvedValue(configManual);
    vi.mocked(rechazarInscripcion).mockResolvedValue(lobby);
    renderPage({ puedeOperar: true });

    await userEvent.click(await screen.findByTestId("btn-rechazar-solicitud"));

    expect(rechazarInscripcion).toHaveBeenCalledWith("p1", "i1", "tok");
    await screen.findByTestId("lobby-panel");
    expect(screen.queryByTestId("solicitudes-panel")).toBeNull();
  });

  it("muestra la fila de solicitud de Equipo con equipoId, miembros y botones cuando puedeOperar", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue(estadoLobby);
    vi.mocked(getLobby).mockResolvedValue({
      ...lobby,
      solicitudesPendientesEquipo: [{ inscripcionId: "ie1", equipoId: "e1", miembros: 3, fechaInscripcion: "2026-07-12T10:00:00Z" }]
    });
    vi.mocked(getPartida).mockResolvedValue(configManual);
    renderPage({ puedeOperar: true });

    const panel = await screen.findByTestId("solicitudes-panel");
    expect(panel).toHaveTextContent("e1");
    expect(panel).toHaveTextContent("3");
    expect(screen.getAllByTestId("btn-aceptar-solicitud")).toHaveLength(1);
    expect(screen.getAllByTestId("btn-rechazar-solicitud")).toHaveLength(1);
  });

  it("en lobby con puedeOperar: primer clic en cancelar muestra confirmacion sin llamar la API; confirmar la llama con (partidaId, token)", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue(estadoLobby);
    vi.mocked(getLobby).mockResolvedValue(lobby);
    vi.mocked(getPartida).mockResolvedValue(configManual);
    vi.mocked(cancelarPartida).mockResolvedValue({ partidaId: "p1", estado: "Cancelada" });
    renderPage({ puedeOperar: true });

    const btn = await screen.findByTestId("btn-cancelar-partida");
    expect(screen.queryByTestId("btn-cancelar-partida-confirm")).not.toBeInTheDocument();

    await userEvent.click(btn);
    expect(cancelarPartida).not.toHaveBeenCalled();
    const confirmBtn = await screen.findByTestId("btn-cancelar-partida-confirm");

    await userEvent.click(confirmBtn);
    expect(cancelarPartida).toHaveBeenCalledWith("p1", "tok");
  });

  it("una transicion de vista (push onIniciada) resetea la confirmacion de cancelar armada en el lobby", async () => {
    vi.mocked(getEstadoSesion)
      .mockResolvedValueOnce(estadoLobby)
      .mockResolvedValueOnce({
        ...estadoLobby,
        estado: "Iniciada",
        juegos: [{ juegoId: "j1", orden: 1, tipoJuego: "Trivia", estado: "Activo" }],
        juegoActualOrden: 1
      });
    vi.mocked(getLobby).mockResolvedValue(lobby);
    vi.mocked(getPartida).mockResolvedValue(configManual);

    let capturedHandlers: SesionHubHandlers = {};
    vi.mocked(useSesionHub).mockImplementation((_id, _tok, handlers) => {
      capturedHandlers = handlers;
    });

    renderPage({ puedeOperar: true });

    const btn = await screen.findByTestId("btn-cancelar-partida");
    await userEvent.click(btn);
    expect(await screen.findByTestId("btn-cancelar-partida-confirm")).toBeInTheDocument();

    // Push del hub: la partida arranco (ej. inicio automatico) mientras la confirmacion estaba armada.
    await act(async () => {
      capturedHandlers.onIniciada?.({ partidaId: "p1" });
    });

    expect(await screen.findByTestId("sesion-iniciada")).toBeInTheDocument();
    expect(screen.getByTestId("btn-cancelar-partida")).toBeInTheDocument();
    expect(screen.queryByTestId("btn-cancelar-partida-confirm")).not.toBeInTheDocument();
  });

  it("el boton de confirmar cancelacion se deshabilita mientras la cancelacion esta en curso", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue(estadoLobby);
    vi.mocked(getLobby).mockResolvedValue(lobby);
    vi.mocked(getPartida).mockResolvedValue(configManual);
    let resolveCancelar: (v: CancelacionPartidaResponse) => void;
    const pendingCancelacion = new Promise<CancelacionPartidaResponse>((res) => {
      resolveCancelar = res;
    });
    vi.mocked(cancelarPartida).mockReturnValue(pendingCancelacion);
    renderPage({ puedeOperar: true });

    await userEvent.click(await screen.findByTestId("btn-cancelar-partida"));
    const confirmBtn = await screen.findByTestId("btn-cancelar-partida-confirm");
    expect(confirmBtn).not.toBeDisabled();

    await userEvent.click(confirmBtn);
    expect(await screen.findByTestId("btn-cancelar-partida-confirm")).toBeDisabled();

    await act(async () => {
      resolveCancelar!({ partidaId: "p1", estado: "Cancelada" });
    });
  });

  it("oculta el boton cancelar partida (y su confirmacion) cuando puedeOperar es false", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue(estadoLobby);
    vi.mocked(getLobby).mockResolvedValue(lobby);
    vi.mocked(getPartida).mockResolvedValue(configManual);
    renderPage({ puedeOperar: false });

    expect(await screen.findByTestId("lobby-panel")).toBeInTheDocument();
    expect(screen.queryByTestId("btn-cancelar-partida")).not.toBeInTheDocument();
    expect(screen.queryByTestId("btn-cancelar-partida-confirm")).not.toBeInTheDocument();
  });

  it("en la vista de sesion iniciada (runtime) el boton cancelar partida tambien esta disponible", async () => {
    vi.mocked(getEstadoSesion).mockResolvedValue({
      ...estadoLobby,
      estado: "Iniciada",
      juegos: [{ juegoId: "j1", orden: 1, tipoJuego: "Trivia", estado: "Activo" }],
      juegoActualOrden: 1
    });
    vi.mocked(getPartida).mockResolvedValue(configManual);
    renderPage({ puedeOperar: true });

    expect(await screen.findByTestId("sesion-iniciada")).toBeInTheDocument();
    expect(screen.getByTestId("btn-cancelar-partida")).toBeInTheDocument();
  });
});
