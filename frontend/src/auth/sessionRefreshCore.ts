// Núcleo puro del ciclo RNF-24: decide refresh silencioso vs modal de continuación.
// Sin timers ni red: el caller posee el interval (270s) y llama tick(); el refresh
// real se inyecta como callback que resuelve true (token renovado) o false (fallo).
export interface SessionRefreshCallbacks {
  refrescar: () => Promise<boolean>;
  onModal: (visible: boolean) => void;
  onExpirada: () => void;
}

export interface SessionRefreshCore {
  marcarActividad(): void;
  tick(): Promise<void>;
  continuar(): Promise<void>;
}

export function crearSessionRefreshCore(cb: SessionRefreshCallbacks): SessionRefreshCore {
  let activo = false;
  let modalPendiente = false;
  let refrescando = false;

  async function ejecutarRefresh(): Promise<void> {
    if (refrescando) return;
    refrescando = true;
    try {
      const ok = await cb.refrescar();
      if (ok) {
        if (modalPendiente) {
          modalPendiente = false;
          cb.onModal(false);
        }
      } else {
        cb.onExpirada();
      }
    } finally {
      refrescando = false;
    }
  }

  return {
    marcarActividad() {
      activo = true;
    },
    async tick() {
      if (modalPendiente || refrescando) return;
      if (activo) {
        // Se consume ANTES del await: actividad ocurrida durante el refresh cuenta
        // para la ventana siguiente en vez de perderse.
        activo = false;
        await ejecutarRefresh();
      } else {
        modalPendiente = true;
        cb.onModal(true);
      }
    },
    async continuar() {
      if (!modalPendiente) return;
      await ejecutarRefresh();
    },
  };
}
