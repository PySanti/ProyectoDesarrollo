import { afterEach, describe, expect, it, vi } from "vitest";
import { renderHook } from "@testing-library/react";
import { useRankingHub } from "./useRankingHub";
import { crearRankingHub } from "../../api/rankingHub";

vi.mock("../../api/rankingHub", () => ({ crearRankingHub: vi.fn() }));

function fakeConnection() {
  const handlers: Record<string, (p: unknown) => void> = {};
  return {
    handlers,
    on: vi.fn((event: string, cb: (p: unknown) => void) => {
      handlers[event] = cb;
    }),
    onreconnected: vi.fn(),
    start: vi.fn().mockResolvedValue(undefined),
    stop: vi.fn().mockResolvedValue(undefined),
    invoke: vi.fn().mockResolvedValue(undefined)
  };
}

describe("useRankingHub", () => {
  afterEach(() => vi.clearAllMocks());

  it("suscribe a la partida y enruta ambos mensajes de juego a onRankingJuego", async () => {
    const conn = fakeConnection();
    vi.mocked(crearRankingHub).mockReturnValue(conn as never);
    const onRankingJuego = vi.fn();
    const onConsolidado = vi.fn();

    const { unmount } = renderHook(() =>
      useRankingHub("p1", "tok", { onRankingJuego, onConsolidado })
    );
    await Promise.resolve();
    await Promise.resolve();

    expect(conn.start).toHaveBeenCalled();
    expect(conn.invoke).toHaveBeenCalledWith("SuscribirAPartida", "p1");

    const payload = { juegoId: "j1", tipoJuego: "Trivia", generadoEn: "t", entradas: [] };
    conn.handlers["RankingTriviaActualizado"](payload);
    expect(onRankingJuego).toHaveBeenCalledWith(payload);
    conn.handlers["RankingBDTActualizado"]({ ...payload, tipoJuego: "BusquedaDelTesoro" });
    expect(onRankingJuego).toHaveBeenCalledTimes(2);

    const consolidado = { partidaId: "p1", generadoEn: "t", entradas: [] };
    conn.handlers["RankingConsolidadoCalculado"](consolidado);
    expect(onConsolidado).toHaveBeenCalledWith(consolidado);

    unmount();
    expect(conn.invoke).toHaveBeenCalledWith("DesuscribirDePartida", "p1");
    expect(conn.stop).toHaveBeenCalled();
  });

  it("partidaId vacío no crea conexión", () => {
    renderHook(() => useRankingHub("", "tok", {}));
    expect(crearRankingHub).not.toHaveBeenCalled();
  });
});
