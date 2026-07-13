import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen } from "@testing-library/react";
import { EnviosTesoroPanel } from "./EnviosTesoroPanel";
import { getEnviosTesoro, OperacionesApiError, type EnviosTesoroDto } from "../../api/operacionesApi";

vi.mock("../../api/operacionesApi", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../../api/operacionesApi")>();
  return { ...actual, getEnviosTesoro: vi.fn() };
});

const envios: EnviosTesoroDto = {
  partidaId: "p1",
  juegoId: "j1",
  etapas: [
    {
      etapaId: "e1",
      orden: 1,
      intentos: [
        { participanteId: "u1", resultado: "Invalido", instante: "2026-07-12T10:00:00Z" },
        { participanteId: "u2", equipoId: "eq1", resultado: "Valido", instante: "2026-07-12T10:01:00Z" }
      ]
    },
    { etapaId: "e2", orden: 2, intentos: [] }
  ]
};

function renderPanel(props: Partial<Parameters<typeof EnviosTesoroPanel>[0]> = {}) {
  return render(<EnviosTesoroPanel partidaId="p1" accessToken="tok" refetchSignal={0} {...props} />);
}

describe("EnviosTesoroPanel", () => {
  beforeEach(() => {
    vi.mocked(getEnviosTesoro).mockReset();
  });

  it("muestra la tabla de intentos por etapa con participante/equipo, resultado y hora", async () => {
    vi.mocked(getEnviosTesoro).mockResolvedValue(envios);
    renderPanel();
    const panel = await screen.findByTestId("envios-tesoro-panel");
    expect(panel).toHaveTextContent("u1");
    expect(panel).toHaveTextContent("Invalido");
    expect(panel).toHaveTextContent("eq1");
    expect(panel).toHaveTextContent("Valido");
  });

  it("sin intentos registrados muestra leyenda vacia", async () => {
    vi.mocked(getEnviosTesoro).mockResolvedValue({ partidaId: "p1", juegoId: "j1", etapas: [] });
    renderPanel();
    const panel = await screen.findByTestId("envios-tesoro-panel");
    expect(panel).toHaveTextContent(/sin envíos/i);
  });

  it("un 409 (juego activo no es BDT) no rompe el panel", async () => {
    vi.mocked(getEnviosTesoro).mockRejectedValue(new OperacionesApiError("no es BDT", 409));
    renderPanel();
    const panel = await screen.findByTestId("envios-tesoro-panel");
    expect(panel).toHaveTextContent(/sin envíos/i);
  });

  it("un cambio de refetchSignal vuelve a pedir los envios", async () => {
    vi.mocked(getEnviosTesoro).mockResolvedValue(envios);
    const { rerender } = renderPanel({ refetchSignal: 0 });
    await screen.findByTestId("envios-tesoro-panel");
    expect(getEnviosTesoro).toHaveBeenCalledTimes(1);
    rerender(<EnviosTesoroPanel partidaId="p1" accessToken="tok" refetchSignal={1} />);
    expect(getEnviosTesoro).toHaveBeenCalledTimes(2);
  });
});
