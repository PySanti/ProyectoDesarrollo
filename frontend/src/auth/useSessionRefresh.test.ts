import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { act, renderHook } from "@testing-library/react";
import { useSessionRefresh, REFRESH_INTERVAL_MS } from "./useSessionRefresh";
import { authProvider } from "./keycloak";

vi.mock("./keycloak", () => ({ authProvider: { refresh: vi.fn() } }));

beforeEach(() => vi.useFakeTimers());
afterEach(() => {
  vi.useRealTimers();
  vi.clearAllMocks();
});

describe("useSessionRefresh", () => {
  it("con actividad reciente, refresca en silencio y entrega el token nuevo", async () => {
    const usuarioRefrescado = {
      username: "op",
      roles: ["Operador"],
      permisos: ["GestionarPartidas"],
      token: "nuevo-token"
    };
    const refresh = vi.mocked(authProvider.refresh).mockResolvedValue(usuarioRefrescado);
    const onUsuario = vi.fn();
    const onExpired = vi.fn();

    const { result } = renderHook(() =>
      useSessionRefresh({ enabled: true, onUsuario, onExpired })
    );

    window.dispatchEvent(new Event("pointerdown"));

    await act(async () => {
      await vi.advanceTimersByTimeAsync(REFRESH_INTERVAL_MS);
    });

    expect(refresh).toHaveBeenCalledTimes(1);
    expect(onUsuario).toHaveBeenCalledWith(usuarioRefrescado);
    expect(onExpired).not.toHaveBeenCalled();
    expect(result.current.modalVisible).toBe(false);
  });

  /* El admin puede cambiar los privilegios de un rol en cualquier momento. Si el refresh sólo
     renovara el string del token, la sesión seguiría con los privilegios del login y el cambio no
     surtiría efecto hasta cerrar sesión. */
  it("propaga los privilegios nuevos del token refrescado", async () => {
    const usuarioRefrescado = {
      username: "admin",
      roles: ["Administrador"],
      permisos: ["GestionarPartidas"],
      token: "token-nuevo"
    };
    vi.mocked(authProvider.refresh).mockResolvedValue(usuarioRefrescado);
    const onUsuario = vi.fn();

    renderHook(() => useSessionRefresh({ enabled: true, onUsuario, onExpired: vi.fn() }));

    window.dispatchEvent(new Event("pointerdown"));

    await act(async () => {
      await vi.advanceTimersByTimeAsync(REFRESH_INTERVAL_MS);
    });

    expect(onUsuario).toHaveBeenCalledWith(usuarioRefrescado);
  });

  it("sin actividad, no refresca y muestra el modal", async () => {
    const refresh = vi.mocked(authProvider.refresh);
    const { result } = renderHook(() =>
      useSessionRefresh({ enabled: true, onUsuario: vi.fn(), onExpired: vi.fn() })
    );

    await act(async () => {
      await vi.advanceTimersByTimeAsync(REFRESH_INTERVAL_MS);
    });

    expect(refresh).not.toHaveBeenCalled();
    expect(result.current.modalVisible).toBe(true);
  });

  it("al desmontar limpia listeners e interval: no vuelve a refrescar", async () => {
    const refresh = vi.mocked(authProvider.refresh).mockResolvedValue({
      username: "op",
      roles: ["Operador"],
      permisos: ["GestionarPartidas"],
      token: "tok"
    });
    const { unmount } = renderHook(() =>
      useSessionRefresh({ enabled: true, onUsuario: vi.fn(), onExpired: vi.fn() })
    );

    unmount();

    await act(async () => {
      await vi.advanceTimersByTimeAsync(REFRESH_INTERVAL_MS * 2);
    });

    expect(refresh).not.toHaveBeenCalled();
  });

  it("con enabled:false no registra listeners ni interval", async () => {
    const refresh = vi.mocked(authProvider.refresh);
    const { result } = renderHook(() =>
      useSessionRefresh({ enabled: false, onUsuario: vi.fn(), onExpired: vi.fn() })
    );

    window.dispatchEvent(new Event("pointerdown"));

    await act(async () => {
      await vi.advanceTimersByTimeAsync(REFRESH_INTERVAL_MS);
    });

    expect(refresh).not.toHaveBeenCalled();
    expect(result.current.modalVisible).toBe(false);
  });
});
