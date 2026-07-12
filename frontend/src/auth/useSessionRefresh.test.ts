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
    const refresh = vi.mocked(authProvider.refresh).mockResolvedValue("nuevo-token");
    const onToken = vi.fn();
    const onExpired = vi.fn();

    const { result } = renderHook(() => useSessionRefresh({ enabled: true, onToken, onExpired }));

    window.dispatchEvent(new Event("pointerdown"));

    await act(async () => {
      await vi.advanceTimersByTimeAsync(REFRESH_INTERVAL_MS);
    });

    expect(refresh).toHaveBeenCalledTimes(1);
    expect(onToken).toHaveBeenCalledWith("nuevo-token");
    expect(onExpired).not.toHaveBeenCalled();
    expect(result.current.modalVisible).toBe(false);
  });

  it("sin actividad, no refresca y muestra el modal", async () => {
    const refresh = vi.mocked(authProvider.refresh);
    const { result } = renderHook(() =>
      useSessionRefresh({ enabled: true, onToken: vi.fn(), onExpired: vi.fn() })
    );

    await act(async () => {
      await vi.advanceTimersByTimeAsync(REFRESH_INTERVAL_MS);
    });

    expect(refresh).not.toHaveBeenCalled();
    expect(result.current.modalVisible).toBe(true);
  });

  it("al desmontar limpia listeners e interval: no vuelve a refrescar", async () => {
    const refresh = vi.mocked(authProvider.refresh).mockResolvedValue("tok");
    const { unmount } = renderHook(() =>
      useSessionRefresh({ enabled: true, onToken: vi.fn(), onExpired: vi.fn() })
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
      useSessionRefresh({ enabled: false, onToken: vi.fn(), onExpired: vi.fn() })
    );

    window.dispatchEvent(new Event("pointerdown"));

    await act(async () => {
      await vi.advanceTimersByTimeAsync(REFRESH_INTERVAL_MS);
    });

    expect(refresh).not.toHaveBeenCalled();
    expect(result.current.modalVisible).toBe(false);
  });
});
