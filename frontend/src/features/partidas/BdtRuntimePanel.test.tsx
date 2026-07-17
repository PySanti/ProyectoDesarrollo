import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { BdtRuntimePanel } from "./BdtRuntimePanel";
import {
  avanzarEtapa,
  finalizarJuegoActual,
  getEtapaActual,
  OperacionesApiError,
  type EtapaActualDto
} from "../../api/operacionesApi";
import { getRankingJuego, type RankingJuegoDto } from "../../api/puntuacionesApi";

vi.mock("../../api/operacionesApi", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../../api/operacionesApi")>();
  return { ...actual, getEtapaActual: vi.fn(), avanzarEtapa: vi.fn(), finalizarJuegoActual: vi.fn() };
});
vi.mock("../../api/puntuacionesApi", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../../api/puntuacionesApi")>();
  return { ...actual, getRankingJuego: vi.fn() };
});

const etapa: EtapaActualDto = {
  partidaId: "p1", juegoId: "j1", etapaId: "e1", orden: 1,
  areaBusqueda: "Plaza central", tiempoLimiteSegundos: 120, fechaActivacion: new Date().toISOString()
};
const ranking: RankingJuegoDto = {
  juegoId: "j1", tipoJuego: "BusquedaDelTesoro", generadoEn: "2026-07-08T12:00:00Z",
  entradas: [{ posicion: 1, competidorId: "abcdef12-0000-0000-0000-000000000000", tipoCompetidor: "Participante", puntos: 50, tiempoAcumuladoMs: 61000, unidadesGanadas: 1 }]
};

function renderPanel(props: Partial<Parameters<typeof BdtRuntimePanel>[0]> = {}) {
  return render(
    <BdtRuntimePanel
      partidaId="p1" juegoId="j1" accessToken="tok" puedeOperar={true} refetchSignal={0}
      onTerminada={vi.fn()} onJuegoAvanzado={vi.fn()} {...props}
    />
  );
}

describe("BdtRuntimePanel", () => {
  beforeEach(() => {
    vi.mocked(getEtapaActual).mockReset();
    vi.mocked(avanzarEtapa).mockReset();
    vi.mocked(finalizarJuegoActual).mockReset();
    vi.mocked(getRankingJuego).mockReset();
  });

  it("con etapa activa muestra area, countdown, avance y ranking", async () => {
    vi.mocked(getEtapaActual).mockResolvedValue(etapa);
    vi.mocked(getRankingJuego).mockResolvedValue(ranking);
    renderPanel();
    expect(await screen.findByTestId("etapa-activa")).toBeInTheDocument();
    expect(screen.getByText(/Plaza central/)).toBeInTheDocument();
    expect(screen.getByTestId("btn-avanzar-etapa")).toBeInTheDocument();
    const tabla = screen.getByTestId("ranking-juego");
    expect(tabla).toHaveTextContent("abcdef12");
    expect(tabla).toHaveTextContent("50");
    expect(tabla).toHaveTextContent("01:01");
  });

  it("con 409 muestra sin-etapa-activa y Finalizar; terminada llama onTerminada", async () => {
    vi.mocked(getEtapaActual).mockRejectedValue(new OperacionesApiError("sin etapa", 409));
    vi.mocked(getRankingJuego).mockResolvedValue(ranking);
    vi.mocked(finalizarJuegoActual).mockResolvedValue({ partidaId: "p1", estado: "Terminada", juegoFinalizadoOrden: 1, juegoActivadoOrden: null, terminada: true });
    const onTerminada = vi.fn();
    renderPanel({ onTerminada });
    expect(await screen.findByTestId("sin-etapa-activa")).toBeInTheDocument();
    await userEvent.click(screen.getByTestId("btn-finalizar-juego"));
    expect(onTerminada).toHaveBeenCalled();
  });

  it("avanzar etapa refetchea la etapa", async () => {
    vi.mocked(getEtapaActual)
      .mockResolvedValueOnce(etapa)
      .mockResolvedValue({ ...etapa, etapaId: "e2", orden: 2, areaBusqueda: "Parque norte" });
    vi.mocked(getRankingJuego).mockResolvedValue(ranking);
    vi.mocked(avanzarEtapa).mockResolvedValue({ partidaId: "p1", etapaCerradaOrden: 1, etapaActivadaOrden: 2, sinMasEtapas: false });
    renderPanel();
    await userEvent.click(await screen.findByTestId("btn-avanzar-etapa"));
    expect(await screen.findByText(/Parque norte/)).toBeInTheDocument();
  });

  it("ranking 404 muestra leyenda sin datos", async () => {
    vi.mocked(getEtapaActual).mockResolvedValue(etapa);
    const { PuntuacionesApiError } = await import("../../api/puntuacionesApi");
    vi.mocked(getRankingJuego).mockRejectedValue(new PuntuacionesApiError("no proyectado", 404));
    renderPanel();
    expect(await screen.findByText(/sin datos de ranking/i)).toBeInTheDocument();
  });

  it("con etapa activa oculta btn-avanzar-etapa cuando puedeOperar es false", async () => {
    vi.mocked(getEtapaActual).mockResolvedValue(etapa);
    vi.mocked(getRankingJuego).mockResolvedValue(ranking);
    renderPanel({ puedeOperar: false });

    expect(await screen.findByTestId("etapa-activa")).toBeInTheDocument();
    expect(screen.queryByTestId("btn-avanzar-etapa")).toBeNull();
  });

  it("sin etapa activa oculta btn-finalizar-juego cuando puedeOperar es false", async () => {
    vi.mocked(getEtapaActual).mockRejectedValue(new OperacionesApiError("sin etapa", 409));
    vi.mocked(getRankingJuego).mockResolvedValue(ranking);
    renderPanel({ puedeOperar: false });

    expect(await screen.findByTestId("sin-etapa-activa")).toBeInTheDocument();
    expect(screen.queryByTestId("btn-finalizar-juego")).toBeNull();
  });

  it("resultadosEtapas con ganadorEquipoId muestra 'Ganada por' el equipo", async () => {
    vi.mocked(getEtapaActual).mockResolvedValue(etapa);
    vi.mocked(getRankingJuego).mockResolvedValue(ranking);
    renderPanel({
      resultadosEtapas: [{ etapaId: "e0", juegoId: "j1", ganadorEquipoId: "eq-1" }]
    });
    const fila = await screen.findByTestId("resultado-etapa");
    expect(fila).toHaveTextContent("Ganada por eq-1");
  });

  it("resultadosEtapas con ganadorParticipanteId (sin equipo) muestra 'Ganada por' el participante", async () => {
    vi.mocked(getEtapaActual).mockResolvedValue(etapa);
    vi.mocked(getRankingJuego).mockResolvedValue(ranking);
    renderPanel({
      resultadosEtapas: [{ etapaId: "e0", juegoId: "j1", ganadorParticipanteId: "part-1" }]
    });
    const fila = await screen.findByTestId("resultado-etapa");
    expect(fila).toHaveTextContent("Ganada por part-1");
  });

  it("resultadosEtapas sin ganador (timeout) muestra 'Nadie consiguió el tesoro'", async () => {
    vi.mocked(getEtapaActual).mockResolvedValue(etapa);
    vi.mocked(getRankingJuego).mockResolvedValue(ranking);
    renderPanel({
      resultadosEtapas: [{ etapaId: "e0", juegoId: "j1" }]
    });
    const fila = await screen.findByTestId("resultado-etapa");
    expect(fila).toHaveTextContent("Nadie consiguió el tesoro");
  });

  it("resultadosEtapas de otro juego no se muestran (filtra por juegoId)", async () => {
    vi.mocked(getEtapaActual).mockResolvedValue(etapa);
    vi.mocked(getRankingJuego).mockResolvedValue(ranking);
    renderPanel({
      resultadosEtapas: [{ etapaId: "e0", juegoId: "otro-juego", ganadorEquipoId: "eq-1" }]
    });
    await screen.findByTestId("etapa-activa");
    expect(screen.queryByTestId("resultado-etapa")).toBeNull();
  });
});
