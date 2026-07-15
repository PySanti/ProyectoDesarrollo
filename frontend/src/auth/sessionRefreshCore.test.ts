import { describe, expect, it, vi } from "vitest";
import { crearSessionRefreshCore } from "./sessionRefreshCore";

function armar(refrescarOk = true) {
  const refrescar = vi.fn().mockResolvedValue(refrescarOk);
  const onModal = vi.fn();
  const onExpirada = vi.fn();
  const core = crearSessionRefreshCore({ refrescar, onModal, onExpirada });
  return { core, refrescar, onModal, onExpirada };
}

describe("sessionRefreshCore", () => {
  it("tick con actividad refresca en silencio y consume la actividad", async () => {
    const { core, refrescar, onModal } = armar();
    core.marcarActividad();
    await core.tick();
    expect(refrescar).toHaveBeenCalledTimes(1);
    expect(onModal).not.toHaveBeenCalled();
    // La actividad se consumió: el siguiente tick sin actividad nueva abre el modal.
    await core.tick();
    expect(onModal).toHaveBeenCalledWith(true);
    expect(refrescar).toHaveBeenCalledTimes(1);
  });

  it("tick sin actividad abre el modal y NO refresca", async () => {
    const { core, refrescar, onModal } = armar();
    await core.tick();
    expect(onModal).toHaveBeenCalledWith(true);
    expect(refrescar).not.toHaveBeenCalled();
  });

  it("con modal abierto los ticks se ignoran", async () => {
    const { core, refrescar, onModal } = armar();
    await core.tick(); // abre modal
    onModal.mockClear();
    core.marcarActividad(); // actividad posterior no cierra el modal sola
    await core.tick();
    await core.tick();
    expect(refrescar).not.toHaveBeenCalled();
    expect(onModal).not.toHaveBeenCalled();
  });

  it("continuar() refresca y cierra el modal si el refresh funciona", async () => {
    const { core, refrescar, onModal } = armar();
    await core.tick(); // abre modal
    await core.continuar();
    expect(refrescar).toHaveBeenCalledTimes(1);
    expect(onModal).toHaveBeenLastCalledWith(false);
  });

  it("continuar() sin modal abierto es no-op", async () => {
    const { core, refrescar } = armar();
    await core.continuar();
    expect(refrescar).not.toHaveBeenCalled();
  });

  it("refresh fallido en tick silencioso dispara onExpirada", async () => {
    const { core, refrescar, onExpirada } = armar(false);
    core.marcarActividad();
    await core.tick();
    expect(refrescar).toHaveBeenCalledTimes(1);
    expect(onExpirada).toHaveBeenCalledTimes(1);
  });

  it("refresh fallido desde continuar() dispara onExpirada y el modal queda abierto", async () => {
    const { core, onModal, onExpirada } = armar(false);
    await core.tick();
    onModal.mockClear();
    await core.continuar();
    expect(onExpirada).toHaveBeenCalledTimes(1);
    expect(onModal).not.toHaveBeenCalledWith(false);
  });
});
