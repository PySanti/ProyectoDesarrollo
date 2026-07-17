// Ciclo RNF-24 en la web: interval de 270s + listeners de actividad + núcleo puro.
import { useEffect, useRef, useState } from "react";
import { authProvider } from "./keycloak";
import { crearSessionRefreshCore, type SessionRefreshCore } from "./sessionRefreshCore";

export const REFRESH_INTERVAL_MS = 270_000;

export function useSessionRefresh(opts: {
  enabled: boolean;
  onToken: (token: string) => void;
  onExpired: () => void;
}): { modalVisible: boolean; continuar: () => void } {
  const [modalVisible, setModalVisible] = useState(false);
  const onTokenRef = useRef(opts.onToken);
  onTokenRef.current = opts.onToken;
  const onExpiredRef = useRef(opts.onExpired);
  onExpiredRef.current = opts.onExpired;
  const coreRef = useRef<SessionRefreshCore | null>(null);

  useEffect(() => {
    if (!opts.enabled) return;

    const core = crearSessionRefreshCore({
      refrescar: () =>
        authProvider.refresh().then(
          (token) => {
            onTokenRef.current(token);
            return true;
          },
          () => false
        ),
      onModal: setModalVisible,
      onExpirada: () => onExpiredRef.current()
    });
    coreRef.current = core;

    const marcar = () => core.marcarActividad();
    // RNF-24: clicks/toques, teclado, scroll y navegación cuentan como actividad.
    window.addEventListener("pointerdown", marcar, { capture: true, passive: true });
    window.addEventListener("keydown", marcar, { capture: true });
    window.addEventListener("scroll", marcar, { capture: true, passive: true });
    window.addEventListener("popstate", marcar);
    const interval = window.setInterval(() => void core.tick(), REFRESH_INTERVAL_MS);

    return () => {
      window.removeEventListener("pointerdown", marcar, { capture: true });
      window.removeEventListener("keydown", marcar, { capture: true });
      window.removeEventListener("scroll", marcar, { capture: true });
      window.removeEventListener("popstate", marcar);
      window.clearInterval(interval);
      coreRef.current = null;
      setModalVisible(false);
    };
  }, [opts.enabled]);

  return { modalVisible, continuar: () => void coreRef.current?.continuar() };
}
