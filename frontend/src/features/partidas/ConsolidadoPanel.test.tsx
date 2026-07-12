import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { ConsolidadoPanel } from "./ConsolidadoPanel";
import * as puntuacionesApi from "../../api/puntuacionesApi";
import { PuntuacionesApiError } from "../../api/puntuacionesApi";

const ranking = {
  partidaId: "p1",
  generadoEn: "2026-07-08T12:00:00Z",
  entradas: [
    {
      posicion: 1,
      competidorId: "abcdef12-0000-0000-0000-000000000000",
      tipoCompetidor: "Participante" as const,
      juegosGanados: 2,
      puntosTotales: 45,
      tiempoTotalMs: 63000
    }
  ]
};

afterEach(() => vi.restoreAllMocks());

describe("ConsolidadoPanel", () => {
  it("muestra la tabla del consolidado al resolver", async () => {
    vi.spyOn(puntuacionesApi, "getRankingConsolidado").mockResolvedValue(ranking);
    render(<ConsolidadoPanel partidaId="p1" accessToken="tok" />);
    const tabla = await screen.findByTestId("ranking-consolidado");
    expect(tabla).toBeInTheDocument();
    expect(screen.getByText("abcdef12")).toBeInTheDocument();
    expect(screen.getByText("45")).toBeInTheDocument();
    expect(screen.getByText("01:03")).toBeInTheDocument(); // 63000ms
  });

  it("200 con entradas vacías muestra 'Sin resultados'", async () => {
    vi.spyOn(puntuacionesApi, "getRankingConsolidado").mockResolvedValue({
      partidaId: "p1",
      generadoEn: "2026-07-08T12:00:00Z",
      entradas: []
    });
    render(<ConsolidadoPanel partidaId="p1" accessToken="tok" />);
    expect(await screen.findByText(/sin resultados/i)).toBeInTheDocument();
  });

  it("reintenta ante 409 y luego muestra la tabla", async () => {
    vi.useFakeTimers();
    const spy = vi
      .spyOn(puntuacionesApi, "getRankingConsolidado")
      .mockRejectedValueOnce(new PuntuacionesApiError("no terminada", 409))
      .mockResolvedValueOnce(ranking);
    render(<ConsolidadoPanel partidaId="p1" accessToken="tok" />);
    await vi.advanceTimersByTimeAsync(1600); // dispara el 2º intento
    vi.useRealTimers();
    expect(await screen.findByTestId("ranking-consolidado")).toBeInTheDocument();
    expect(spy).toHaveBeenCalledTimes(2);
  });

  it("409 persistente muestra aviso y botón Reintentar que vuelve a pedir", async () => {
    vi.useFakeTimers();
    const spy = vi
      .spyOn(puntuacionesApi, "getRankingConsolidado")
      .mockRejectedValue(new PuntuacionesApiError("no terminada", 409));
    render(<ConsolidadoPanel partidaId="p1" accessToken="tok" />);
    await vi.advanceTimersByTimeAsync(3200); // agota los 3 intentos (2 esperas de 1500)
    vi.useRealTimers();
    expect(await screen.findByText(/no disponible aún/i)).toBeInTheDocument();
    expect(spy).toHaveBeenCalledTimes(3);

    spy.mockResolvedValueOnce(ranking);
    await userEvent.click(screen.getByRole("button", { name: /reintentar/i }));
    expect(await screen.findByTestId("ranking-consolidado")).toBeInTheDocument();
  });
});
