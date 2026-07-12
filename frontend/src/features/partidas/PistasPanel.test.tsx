import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { PistasPanel } from "./PistasPanel";
import { enviarPista, getLobby, OperacionesApiError, type LobbyDto } from "../../api/operacionesApi";

vi.mock("../../api/operacionesApi", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../../api/operacionesApi")>();
  return { ...actual, getLobby: vi.fn(), enviarPista: vi.fn() };
});

const lobbyIndividual: LobbyDto = {
  partidaId: "p1", sesionPartidaId: "s1", estado: "Iniciada", modalidad: "Individual",
  minimosParticipacion: 1, maximosParticipacion: 10, inscritosActivos: 2,
  participantes: ["11111111-aaaa", "22222222-bbbb"], equipos: []
};
const lobbyEquipo: LobbyDto = {
  ...lobbyIndividual, modalidad: "Equipo", participantes: [],
  equipos: [{ equipoId: "eq111111-cccc", convocados: 3, aceptados: 2 }]
};

describe("PistasPanel", () => {
  beforeEach(() => {
    vi.mocked(getLobby).mockReset();
    vi.mocked(enviarPista).mockReset();
  });

  it("Individual: envia pista al participante elegido", async () => {
    vi.mocked(getLobby).mockResolvedValue(lobbyIndividual);
    vi.mocked(enviarPista).mockResolvedValue({ partidaId: "p1", juegoId: "j1", participanteDestinoId: "11111111-aaaa", equipoDestinoId: null, timestampUtc: "2026-07-08T12:00:00Z" });
    render(<PistasPanel partidaId="p1" accessToken="tok" />);
    await screen.findByTestId("pistas-panel");
    await userEvent.selectOptions(screen.getByTestId("pista-destino"), "11111111-aaaa");
    await userEvent.type(screen.getByTestId("pista-texto"), "mira bajo el banco");
    await userEvent.click(screen.getByTestId("btn-enviar-pista"));
    expect(vi.mocked(enviarPista)).toHaveBeenCalledWith("p1", { texto: "mira bajo el banco", participanteDestinoId: "11111111-aaaa" }, "tok");
    expect(await screen.findByTestId("pista-enviada")).toBeInTheDocument();
  });

  it("Equipo: el destino se envia como equipoDestinoId", async () => {
    vi.mocked(getLobby).mockResolvedValue(lobbyEquipo);
    vi.mocked(enviarPista).mockResolvedValue({ partidaId: "p1", juegoId: "j1", participanteDestinoId: null, equipoDestinoId: "eq111111-cccc", timestampUtc: "2026-07-08T12:00:00Z" });
    render(<PistasPanel partidaId="p1" accessToken="tok" />);
    await screen.findByTestId("pistas-panel");
    await userEvent.selectOptions(screen.getByTestId("pista-destino"), "eq111111-cccc");
    await userEvent.type(screen.getByTestId("pista-texto"), "pista de equipo");
    await userEvent.click(screen.getByTestId("btn-enviar-pista"));
    expect(vi.mocked(enviarPista)).toHaveBeenCalledWith("p1", { texto: "pista de equipo", equipoDestinoId: "eq111111-cccc" }, "tok");
  });

  it("error 404 (destino no inscrito) se muestra inline", async () => {
    vi.mocked(getLobby).mockResolvedValue(lobbyIndividual);
    vi.mocked(enviarPista).mockRejectedValue(new OperacionesApiError("destino no inscrito", 404));
    render(<PistasPanel partidaId="p1" accessToken="tok" />);
    await screen.findByTestId("pistas-panel");
    await userEvent.selectOptions(screen.getByTestId("pista-destino"), "11111111-aaaa");
    await userEvent.type(screen.getByTestId("pista-texto"), "x");
    await userEvent.click(screen.getByTestId("btn-enviar-pista"));
    expect(await screen.findByRole("alert")).toBeInTheDocument();
  });

  it("roster vacio muestra leyenda", async () => {
    vi.mocked(getLobby).mockResolvedValue({ ...lobbyIndividual, participantes: [] });
    render(<PistasPanel partidaId="p1" accessToken="tok" />);
    expect(await screen.findByText(/sin inscritos/i)).toBeInTheDocument();
  });
});
