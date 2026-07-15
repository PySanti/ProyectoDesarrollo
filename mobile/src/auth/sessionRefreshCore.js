// Núcleo puro del ciclo RNF-24 (espejo del core web): decide refresh silencioso
// vs modal. Sin timers ni red; el caller posee el interval de 270s.
export function crearSessionRefreshCore({ refrescar, onModal, onExpirada }) {
  let activo = false;
  let modalPendiente = false;
  let refrescando = false;

  async function ejecutarRefresh() {
    if (refrescando) return;
    refrescando = true;
    try {
      const ok = await refrescar();
      if (ok) {
        if (modalPendiente) {
          modalPendiente = false;
          onModal(false);
        }
      } else {
        onExpirada();
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
        activo = false;
        await ejecutarRefresh();
      } else {
        modalPendiente = true;
        onModal(true);
      }
    },
    async continuar() {
      if (!modalPendiente) return;
      await ejecutarRefresh();
    },
  };
}
