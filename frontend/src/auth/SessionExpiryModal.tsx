// Modal de continuación de sesión (RNF-24): aparece cuando el tick de 270s
// encuentra al usuario inactivo. Sin countdown: Keycloak decide la expiración real.
export function SessionExpiryModal({
  visible,
  onContinuar,
  onSalir
}: {
  visible: boolean;
  onContinuar: () => void;
  onSalir: () => void;
}) {
  if (!visible) {
    return null;
  }
  return (
    <div className="modal-backdrop" role="dialog" aria-modal="true" aria-label="Sesión por expirar">
      <div className="modal-card stack" data-testid="session-expiry-modal">
        <h2>¿Sigues ahí?</h2>
        <p className="muted">Tu sesión está por expirar.</p>
        <div className="compact-actions">
          <button type="button" onClick={onContinuar}>
            Continuar sesión
          </button>
          <button type="button" className="secondary-button" onClick={onSalir}>
            Salir
          </button>
        </div>
      </div>
    </div>
  );
}
