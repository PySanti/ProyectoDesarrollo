import { renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { resetNombresCache, useNombres } from "./useNombres";
import * as directoryApi from "../../api/directoryApi";

const A = "aaaaaaaa-0000-0000-0000-000000000000";
const B = "bbbbbbbb-0000-0000-0000-000000000000";
const EQ = "eeeeeeee-0000-0000-0000-000000000000";

beforeEach(() => {
  // La caché es de módulo: sin reset, los tests se contaminan entre sí.
  resetNombresCache();
  vi.restoreAllMocks();
});

describe("useNombres", () => {
  it("resuelve nombres de participantes y equipos", async () => {
    vi.spyOn(directoryApi, "resolverNombres").mockResolvedValue({
      participantes: [{ participanteId: A, nombre: "María González" }],
      equipos: [{ equipoId: EQ, nombreEquipo: "Los Cazadores" }]
    });

    const { result } = renderHook(() => useNombres({ participanteIds: [A], equipoIds: [EQ] }, "tok"));

    await waitFor(() => expect(result.current(A)).toBe("María González"));
    expect(result.current(EQ)).toBe("Los Cazadores");
  });

  it("cae al GUID corto cuando el directorio falla, sin lanzar", async () => {
    vi.spyOn(directoryApi, "resolverNombres").mockRejectedValue(new Error("red caída"));

    const { result } = renderHook(() => useNombres({ participanteIds: [A], equipoIds: [] }, "tok"));

    await waitFor(() => expect(directoryApi.resolverNombres).toHaveBeenCalled());
    expect(result.current(A)).toBe("aaaaaaaa");
  });

  it("cae al GUID corto para un id que el directorio omite", async () => {
    vi.spyOn(directoryApi, "resolverNombres").mockResolvedValue({ participantes: [], equipos: [] });

    const { result } = renderHook(() => useNombres({ participanteIds: [A], equipoIds: [] }, "tok"));

    await waitFor(() => expect(directoryApi.resolverNombres).toHaveBeenCalled());
    expect(result.current(A)).toBe("aaaaaaaa");
  });

  it("no repide ids ya cacheados y pide solo los faltantes cuando llega uno nuevo", async () => {
    const spy = vi.spyOn(directoryApi, "resolverNombres")
      .mockResolvedValueOnce({ participantes: [{ participanteId: A, nombre: "Ana" }], equipos: [] })
      .mockResolvedValueOnce({ participantes: [{ participanteId: B, nombre: "Bruno" }], equipos: [] });

    // Primer render: solo A. Simula el estado inicial del ranking.
    const { result, rerender } = renderHook(
      ({ ids }) => useNombres(ids, "tok"),
      { initialProps: { ids: { participanteIds: [A], equipoIds: [] as string[] } } }
    );
    await waitFor(() => expect(result.current(A)).toBe("Ana"));

    // Segundo render: llega B por push de SignalR. A ya está cacheado.
    rerender({ ids: { participanteIds: [A, B], equipoIds: [] as string[] } });
    await waitFor(() => expect(result.current(B)).toBe("Bruno"));

    expect(spy).toHaveBeenCalledTimes(2);
    expect(spy.mock.calls[1][0]).toEqual({ participanteIds: [B], equipoIds: [] });
    expect(result.current(A)).toBe("Ana");
  });

  it("no llama al directorio cuando no hay ids que resolver", async () => {
    const spy = vi.spyOn(directoryApi, "resolverNombres");

    renderHook(() => useNombres({ participanteIds: [], equipoIds: [] }, "tok"));

    await waitFor(() => expect(spy).not.toHaveBeenCalled());
  });

  it("trocea en lotes de 200 contando ambas listas sumadas", async () => {
    const muchos = Array.from({ length: 250 }, (_, i) => `${String(i).padStart(8, "0")}-0000-0000-0000-000000000000`);
    const spy = vi.spyOn(directoryApi, "resolverNombres").mockResolvedValue({ participantes: [], equipos: [] });

    renderHook(() => useNombres({ participanteIds: muchos, equipoIds: [EQ] }, "tok"));

    await waitFor(() => expect(spy).toHaveBeenCalledTimes(2));
    expect(spy.mock.calls[0][0].participanteIds).toHaveLength(200);
    expect(spy.mock.calls[0][0].equipoIds).toHaveLength(0);
    expect(spy.mock.calls[1][0].participanteIds).toHaveLength(50);
    expect(spy.mock.calls[1][0].equipoIds).toEqual([EQ]);
  });
});
