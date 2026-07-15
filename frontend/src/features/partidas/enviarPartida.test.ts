import { describe, expect, it, vi } from "vitest";
import { PartidasApiError } from "../../api/partidasApi";
import {
  initialDraft,
  newEtapa,
  newJuegoBdt,
  newJuegoTrivia,
  newPregunta,
  type CreatePartidaDraft,
  type HeaderDraft
} from "./createPartidaDraft";
import { enviarPartida, type EnvioJuego } from "./enviarPartida";

function validHeader(): HeaderDraft {
  return {
    nombrePartida: "Copa demo",
    modalidad: "Individual",
    modoInicioPartida: "Manual",
    tiempoInicio: "",
    minimosParticipacion: "1",
    maximosParticipacion: "10"
  };
}

function draftTriviaYBdt(): CreatePartidaDraft {
  const draft = initialDraft();
  draft.header = validHeader();
  const trivia = newJuegoTrivia();
  trivia.preguntas = [newPregunta()];
  const bdt = newJuegoBdt();
  bdt.areaBusqueda = "Parque central";
  bdt.etapas = [newEtapa()];
  draft.juegos = [trivia, bdt];
  return draft;
}

function draftTresTrivia(): CreatePartidaDraft {
  const draft = initialDraft();
  draft.header = validHeader();
  draft.juegos = [0, 1, 2].map(() => {
    const t = newJuegoTrivia();
    t.preguntas = [newPregunta()];
    return t;
  });
  return draft;
}

describe("enviarPartida", () => {
  it("flujo feliz: header + trivia + bdt en orden, completo true", async () => {
    const createPartidaMock = vi.fn().mockResolvedValue({ partidaId: "p-1" });
    const addJuegoTriviaMock = vi.fn().mockResolvedValue({ juegoId: "j-1" });
    const addJuegoBdtMock = vi.fn().mockResolvedValue({ juegoId: "j-2" });
    const onProgress = vi.fn();

    const resultado = await enviarPartida(draftTriviaYBdt(), "tok", null, onProgress, {
      createPartida: createPartidaMock,
      addJuegoTrivia: addJuegoTriviaMock,
      addJuegoBdt: addJuegoBdtMock
    });

    expect(createPartidaMock).toHaveBeenCalledTimes(1);
    expect(addJuegoTriviaMock).toHaveBeenCalledTimes(1);
    expect(addJuegoBdtMock).toHaveBeenCalledTimes(1);
    expect(addJuegoTriviaMock.mock.calls[0][0]).toBe("p-1");
    expect(addJuegoTriviaMock.mock.calls[0][1].orden).toBe(1);
    expect(addJuegoBdtMock.mock.calls[0][1].orden).toBe(2);
    expect(resultado).toEqual({
      partidaId: "p-1",
      estados: [{ estado: "ok" }, { estado: "ok" }],
      completo: true
    });
  });

  it("falla el header: errorHeader con mensaje, cero llamadas a juegos", async () => {
    const createPartidaMock = vi.fn().mockRejectedValue(new PartidasApiError("nombre invalido", 400));
    const addJuegoTriviaMock = vi.fn();
    const addJuegoBdtMock = vi.fn();
    const onProgress = vi.fn();

    const resultado = await enviarPartida(draftTriviaYBdt(), "tok", null, onProgress, {
      createPartida: createPartidaMock,
      addJuegoTrivia: addJuegoTriviaMock,
      addJuegoBdt: addJuegoBdtMock
    });

    expect(resultado.partidaId).toBeNull();
    expect(resultado.errorHeader).toBe("nombre invalido");
    expect(resultado.completo).toBe(false);
    expect(resultado.estados).toEqual([{ estado: "pendiente" }, { estado: "pendiente" }]);
    expect(addJuegoTriviaMock).not.toHaveBeenCalled();
    expect(addJuegoBdtMock).not.toHaveBeenCalled();
  });

  it("falla el juego 2 de 3: cadena detenida, juego 3 queda pendiente", async () => {
    const createPartidaMock = vi.fn().mockResolvedValue({ partidaId: "p-1" });
    const addJuegoTriviaMock = vi
      .fn()
      .mockResolvedValueOnce({ juegoId: "j-1" })
      .mockRejectedValueOnce(new PartidasApiError("orden duplicado", 409))
      .mockResolvedValueOnce({ juegoId: "j-3" });
    const onProgress = vi.fn();

    const resultado = await enviarPartida(draftTresTrivia(), "tok", null, onProgress, {
      createPartida: createPartidaMock,
      addJuegoTrivia: addJuegoTriviaMock,
      addJuegoBdt: vi.fn()
    });

    expect(addJuegoTriviaMock).toHaveBeenCalledTimes(2);
    expect(resultado.estados).toEqual([
      { estado: "ok" },
      { estado: "error", mensaje: "orden duplicado" },
      { estado: "pendiente" }
    ]);
    expect(resultado.completo).toBe(false);
  });

  it("reintento con previo: no re-postea header ni juego ya ok, envia los restantes", async () => {
    const createPartidaMock = vi.fn();
    const addJuegoTriviaMock = vi.fn().mockResolvedValue({ juegoId: "j-x" });
    const onProgress = vi.fn();

    const previo: { partidaId: string | null; estados: EnvioJuego[] } = {
      partidaId: "p-1",
      estados: [{ estado: "ok" }, { estado: "error", mensaje: "orden duplicado" }, { estado: "pendiente" }]
    };

    const resultado = await enviarPartida(draftTresTrivia(), "tok", previo, onProgress, {
      createPartida: createPartidaMock,
      addJuegoTrivia: addJuegoTriviaMock,
      addJuegoBdt: vi.fn()
    });

    expect(createPartidaMock).not.toHaveBeenCalled();
    expect(addJuegoTriviaMock).toHaveBeenCalledTimes(2);
    expect(addJuegoTriviaMock.mock.calls[0][1].orden).toBe(2);
    expect(addJuegoTriviaMock.mock.calls[1][1].orden).toBe(3);
    expect(resultado).toEqual({
      partidaId: "p-1",
      estados: [{ estado: "ok" }, { estado: "ok" }, { estado: "ok" }],
      completo: true
    });
  });

  it("fallo de red (TypeError de fetch) produce mensaje generico, no el texto crudo", async () => {
    const createPartidaMock = vi.fn().mockResolvedValue({ partidaId: "p-1" });
    const addJuegoTriviaMock = vi.fn().mockRejectedValue(new TypeError("Failed to fetch"));

    const resultado = await enviarPartida(draftTresTrivia(), "tok", null, vi.fn(), {
      createPartida: createPartidaMock,
      addJuegoTrivia: addJuegoTriviaMock,
      addJuegoBdt: vi.fn()
    });

    expect(resultado.estados[0]).toEqual({
      estado: "error",
      mensaje: "Error de red al enviar la partida."
    });
    expect(resultado.completo).toBe(false);
  });

  it("onProgress se llama con 'enviando' antes de cada POST de juego", async () => {
    const createPartidaMock = vi.fn().mockResolvedValue({ partidaId: "p-1" });
    const callOrder: string[] = [];
    const addJuegoTriviaMock = vi.fn().mockImplementation(async (_partidaId: string, payload: { orden: number }) => {
      callOrder.push(`post-${payload.orden}`);
      return { juegoId: `j-${payload.orden}` };
    });
    const onProgress = vi.fn((estados: EnvioJuego[]) => {
      const idx = estados.findIndex((e) => e.estado === "enviando");
      if (idx !== -1) callOrder.push(`enviando-${idx + 1}`);
    });

    await enviarPartida(draftTresTrivia(), "tok", null, onProgress, {
      createPartida: createPartidaMock,
      addJuegoTrivia: addJuegoTriviaMock,
      addJuegoBdt: vi.fn()
    });

    expect(callOrder).toEqual(["enviando-1", "post-1", "enviando-2", "post-2", "enviando-3", "post-3"]);
  });
});
