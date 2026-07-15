import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  getRankingConsolidado,
  getRankingJuego,
  PuntuacionesApiError,
  getHistorialPartida,
  getRendimientoEquipo
} from "./puntuacionesApi";

const okJson = (body: unknown, status = 200) =>
  vi.fn().mockResolvedValue(new Response(JSON.stringify(body), { status }));

describe("puntuacionesApi", () => {
  beforeEach(() => vi.stubEnv("VITE_GATEWAY_BASE_URL", "https://gw.example.test/"));
  afterEach(() => vi.unstubAllEnvs());

  it("getRankingJuego hace GET autenticado al ranking del juego", async () => {
    const f = okJson({
      juegoId: "j1",
      tipoJuego: "Trivia",
      generadoEn: "2026-07-08T12:00:00Z",
      entradas: [
        {
          posicion: 1,
          competidorId: "c1",
          tipoCompetidor: "Participante",
          puntos: 30,
          tiempoAcumuladoMs: 12345,
          unidadesGanadas: 3
        }
      ]
    });
    const r = await getRankingJuego("p1", "j1", "tok", f);
    expect(r.entradas[0].puntos).toBe(30);
    expect(f.mock.calls[0][0]).toBe(
      "https://gw.example.test/puntuaciones/partidas/p1/juegos/j1/ranking"
    );
    expect((f.mock.calls[0][1].headers as Record<string, string>).Authorization).toBe("Bearer tok");
  });

  it("404 de proyeccion lanza PuntuacionesApiError con statusCode", async () => {
    const f = okJson({ message: "no proyectado" }, 404);
    await expect(getRankingJuego("p1", "j1", "tok", f)).rejects.toMatchObject({ statusCode: 404 });
    await expect(getRankingJuego("p1", "j1", "tok", f)).rejects.toBeInstanceOf(PuntuacionesApiError);
  });

  it("getRankingConsolidado hace GET autenticado al consolidado de la partida", async () => {
    const f = okJson({
      partidaId: "p1",
      generadoEn: "2026-07-08T12:00:00Z",
      entradas: [
        {
          posicion: 1,
          competidorId: "c1",
          tipoCompetidor: "Participante",
          juegosGanados: 2,
          puntosTotales: 45,
          tiempoTotalMs: 23456
        }
      ]
    });
    const r = await getRankingConsolidado("p1", "tok", f);
    expect(r.entradas[0].puntosTotales).toBe(45);
    expect(r.entradas[0].juegosGanados).toBe(2);
    expect(f.mock.calls[0][0]).toBe(
      "https://gw.example.test/puntuaciones/partidas/p1/ranking-consolidado"
    );
    expect((f.mock.calls[0][1].headers as Record<string, string>).Authorization).toBe("Bearer tok");
  });

  it("409 (partida no terminada) lanza PuntuacionesApiError con statusCode", async () => {
    const f = okJson({ message: "no terminada" }, 409);
    await expect(getRankingConsolidado("p1", "tok", f)).rejects.toMatchObject({ statusCode: 409 });
    await expect(getRankingConsolidado("p1", "tok", f)).rejects.toBeInstanceOf(PuntuacionesApiError);
  });

  it("consolidado 200 con entradas vacías es válido (terminada sin marcadores)", async () => {
    const f = okJson({ partidaId: "p1", generadoEn: "2026-07-08T12:00:00Z", entradas: [] });
    const r = await getRankingConsolidado("p1", "tok", f);
    expect(r.entradas).toEqual([]);
  });

  it("getHistorialPartida arma query solo con los opts presentes", async () => {
    const f = okJson({ partidaId: "p1", total: 2, entradas: [] });
    await getHistorialPartida("p1", "tok", { limit: 50, tipo: "PistaEnviada" }, f);
    expect(f.mock.calls[0][0]).toBe(
      "https://gw.example.test/puntuaciones/partidas/p1/historial?limit=50&tipo=PistaEnviada"
    );
    await getHistorialPartida("p1", "tok", {}, f);
    expect(f.mock.calls[1][0]).toBe("https://gw.example.test/puntuaciones/partidas/p1/historial");
    expect((f.mock.calls[0][1].headers as Record<string, string>).Authorization).toBe("Bearer tok");
  });

  it("getHistorialPartida 403 de participante lanza PuntuacionesApiError", async () => {
    const f = okJson({ message: "solo operador/administrador" }, 403);
    await expect(getHistorialPartida("p1", "tok", {}, f)).rejects.toMatchObject({
      statusCode: 403
    });
  });

  it("getRendimientoEquipo hace GET autenticado y devuelve partidas", async () => {
    const f = okJson({
      equipoId: "e1",
      partidas: [{ partidaId: "p1", fechaFin: "2026-07-10T12:00:00Z", posicion: 1, gano: true }]
    });
    const r = await getRendimientoEquipo("e1", "tok", f);
    expect(r.partidas[0].gano).toBe(true);
    expect(f.mock.calls[0][0]).toBe("https://gw.example.test/puntuaciones/equipos/e1/rendimiento");
  });

  it("getRendimientoEquipo mapea un non-success a PuntuacionesApiError con status", async () => {
    const f = okJson({ message: "servicio no disponible" }, 503);

    await expect(getRendimientoEquipo("e1", "tok", f)).rejects.toMatchObject({
      message: "servicio no disponible",
      statusCode: 503
    });
    await expect(getRendimientoEquipo("e1", "tok", f)).rejects.toBeInstanceOf(
      PuntuacionesApiError
    );
  });
});
