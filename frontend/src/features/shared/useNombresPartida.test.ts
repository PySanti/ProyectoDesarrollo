import { renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { useNombresPartida } from "./useNombresPartida";
import * as partidasApi from "../../api/partidasApi";

const P1 = "aaaaaaaa-0000-0000-0000-000000000000";
const P2 = "bbbbbbbb-0000-0000-0000-000000000000";

const resumen = (partidaId: string, nombrePartida: string) =>
  ({
    partidaId,
    nombrePartida,
    modalidad: "Individual",
    modoInicioPartida: "Manual",
    tiempoInicio: null,
    minimosParticipacion: 1,
    maximosParticipacion: 10,
    estado: null,
    cantidadJuegos: 1
  }) as unknown as partidasApi.PartidaSummary;

beforeEach(() => vi.restoreAllMocks());

describe("useNombresPartida", () => {
  it("resuelve el nombre de una partida conocida", async () => {
    vi.spyOn(partidasApi, "getPartidas").mockResolvedValue([resumen(P1, "Copa UCAB")]);

    const { result } = renderHook(() => useNombresPartida("tok"));

    await waitFor(() => expect(result.current(P1)).toBe("Copa UCAB"));
  });

  it("cae al GUID corto para una partida que no está en la lista", async () => {
    vi.spyOn(partidasApi, "getPartidas").mockResolvedValue([resumen(P1, "Copa UCAB")]);

    const { result } = renderHook(() => useNombresPartida("tok"));

    await waitFor(() => expect(partidasApi.getPartidas).toHaveBeenCalled());
    expect(result.current(P2)).toBe("bbbbbbbb");
  });

  it("cae al GUID corto cuando la llamada falla, sin lanzar", async () => {
    vi.spyOn(partidasApi, "getPartidas").mockRejectedValue(new Error("caido"));

    const { result } = renderHook(() => useNombresPartida("tok"));

    await waitFor(() => expect(partidasApi.getPartidas).toHaveBeenCalled());
    expect(result.current(P1)).toBe("aaaaaaaa");
  });

  it("pide la lista una sola vez por montaje", async () => {
    const spy = vi.spyOn(partidasApi, "getPartidas").mockResolvedValue([resumen(P1, "Copa UCAB")]);

    const { rerender } = renderHook(() => useNombresPartida("tok"));
    rerender();

    await waitFor(() => expect(spy).toHaveBeenCalledTimes(1));
  });
});
