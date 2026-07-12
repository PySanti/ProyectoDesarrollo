import { afterEach, describe, expect, it, vi } from "vitest";
import { renderHook } from "@testing-library/react";
import { useSesionHub } from "./useSesionHub";
import { crearSesionHub } from "../../api/sesionHub";

vi.mock("../../api/sesionHub", () => ({ crearSesionHub: vi.fn() }));

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

describe("useSesionHub", () => {
  afterEach(() => vi.clearAllMocks());

  it("al montar arranca, se suscribe y registra handlers; el push invoca el callback; al desmontar se desuscribe y detiene", async () => {
    const conn = fakeConnection();
    vi.mocked(crearSesionHub).mockReturnValue(conn as never);
    const onIniciada = vi.fn();

    const { unmount } = renderHook(() => useSesionHub("p1", "tok", { onIniciada }));
    await Promise.resolve();
    await Promise.resolve();

    expect(conn.start).toHaveBeenCalled();
    expect(conn.invoke).toHaveBeenCalledWith("SuscribirAPartida", "p1");
    expect(conn.on).toHaveBeenCalledWith("PartidaIniciada", expect.any(Function));

    conn.handlers["PartidaIniciada"]({ partidaId: "p1" });
    expect(onIniciada).toHaveBeenCalledWith({ partidaId: "p1" });

    unmount();
    expect(conn.invoke).toHaveBeenCalledWith("DesuscribirDePartida", "p1");
    expect(conn.stop).toHaveBeenCalled();
  });

  it("con partidaId vacio no crea conexion", () => {
    renderHook(() => useSesionHub("", "tok", {}));
    expect(crearSesionHub).not.toHaveBeenCalled();
  });

  it("rutea PreguntaActivada y PreguntaCerrada a sus handlers", async () => {
    const conn = fakeConnection();
    vi.mocked(crearSesionHub).mockReturnValue(conn as never);
    const onPreguntaActivada = vi.fn();
    const onPreguntaCerrada = vi.fn();

    renderHook(() => useSesionHub("p1", "tok", { onPreguntaActivada, onPreguntaCerrada }));
    await Promise.resolve();

    conn.handlers["PreguntaActivada"]({ partidaId: "p1", juegoId: "j1", preguntaId: "q1", orden: 1, fechaLimiteUtc: "2026-07-08T12:00:30Z" });
    expect(onPreguntaActivada).toHaveBeenCalledWith(
      expect.objectContaining({ preguntaId: "q1", fechaLimiteUtc: "2026-07-08T12:00:30Z" })
    );

    conn.handlers["PreguntaCerrada"]({ partidaId: "p1", juegoId: "j1", preguntaId: "q1" });
    expect(onPreguntaCerrada).toHaveBeenCalledWith(expect.objectContaining({ preguntaId: "q1" }));
  });

  it("rutea EtapaActivada/EtapaCerrada/EtapaGanada/UbicacionActualizada a sus handlers", async () => {
    const conn = fakeConnection();
    vi.mocked(crearSesionHub).mockReturnValue(conn as never);
    const onEtapaActivada = vi.fn();
    const onEtapaCerrada = vi.fn();
    const onEtapaGanada = vi.fn();
    const onUbicacionActualizada = vi.fn();

    renderHook(() => useSesionHub("p1", "tok", { onEtapaActivada, onEtapaCerrada, onEtapaGanada, onUbicacionActualizada }));
    await Promise.resolve();

    conn.handlers["EtapaActivada"]({ partidaId: "p1", juegoId: "j1", etapaId: "e1", orden: 1, fechaLimiteUtc: "2026-07-08T12:02:00Z" });
    expect(onEtapaActivada).toHaveBeenCalledWith(expect.objectContaining({ etapaId: "e1", fechaLimiteUtc: "2026-07-08T12:02:00Z" }));
    conn.handlers["EtapaCerrada"]({ partidaId: "p1", juegoId: "j1", etapaId: "e1" });
    expect(onEtapaCerrada).toHaveBeenCalledWith(expect.objectContaining({ etapaId: "e1" }));
    conn.handlers["EtapaGanada"]({ partidaId: "p1", juegoId: "j1", etapaId: "e1" });
    expect(onEtapaGanada).toHaveBeenCalledWith(expect.objectContaining({ etapaId: "e1" }));
    conn.handlers["UbicacionActualizada"]({ partidaId: "p1", participanteId: "u1", latitud: 10.5, longitud: -66.9, timestampUtc: "2026-07-08T12:00:00Z" });
    expect(onUbicacionActualizada).toHaveBeenCalledWith(expect.objectContaining({ participanteId: "u1", latitud: 10.5, longitud: -66.9 }));
  });
});
