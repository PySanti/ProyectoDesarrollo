import { AlertTriangle, BrandMark, Compass, Lock } from "./icons";

export function LoginScreen({ onLogin }: { onLogin: () => void }) {
  return (
    <div className="sh-state">
      <div className="sh-state__card">
        <div style={{ display: "flex", alignItems: "center", gap: "var(--sp-sm)" }}>
          <BrandMark className="sh-state__icon" />
          <span
            style={{
              fontFamily: "var(--font-display)",
              fontWeight: 700,
              fontSize: "1.5rem",
              letterSpacing: "-0.01em"
            }}
          >
            UMBRAL
          </span>
        </div>
        <h1>Consola de administración y operación</h1>
        <p className="muted">
          Crea y supervisa partidas de Trivia y Búsqueda del Tesoro. Inicia sesión para continuar.
        </p>
        <button type="button" onClick={onLogin}>
          Iniciar sesión
        </button>
      </div>
    </div>
  );
}

export function LoadingScreen() {
  return (
    <div className="sh-state">
      <div className="sh-state__card" aria-busy="true">
        <BrandMark className="sh-state__icon" />
        <h1>Autenticando…</h1>
        <p className="muted">Conectando con Keycloak. Un momento.</p>
        <div className="sh-skel" style={{ height: 12, width: "70%" }} />
        <div className="sh-skel" style={{ height: 12, width: "90%" }} />
      </div>
    </div>
  );
}

export function AuthErrorScreen({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <div className="sh-state">
      <div className="sh-state__card" role="alert">
        <AlertTriangle className="sh-state__icon" style={{ color: "var(--danger)" }} />
        <h1>No pudimos autenticarte</h1>
        <p className="muted">{message}</p>
        {onRetry ? (
          <button type="button" onClick={onRetry}>
            Reintentar
          </button>
        ) : null}
      </div>
    </div>
  );
}

export function UnauthorizedScreen({
  username,
  onLogout
}: {
  username: string;
  onLogout?: () => void;
}) {
  return (
    <div className="sh-state">
      <div className="sh-state__card" role="alert">
        <Lock className="sh-state__icon" style={{ color: "var(--danger)" }} />
        <h1 style={{ color: "var(--danger)" }}>
          El panel web es exclusivo para administradores y operadores
        </h1>
        <p className="muted">
          La cuenta <strong>{username}</strong> no tiene rol Administrador u Operador. Usa la app
          móvil para participar en las partidas.
        </p>
        {onLogout ? (
          <button type="button" onClick={onLogout}>
            Cerrar sesión
          </button>
        ) : null}
      </div>
    </div>
  );
}

export function NotFoundScreen() {
  return (
    <div className="sh-state">
      <div className="sh-state__card">
        <Compass className="sh-state__icon" />
        <h1>No encontramos esa pantalla</h1>
        <p className="muted">La ruta no existe o no está disponible para tu rol.</p>
        <a href="/">Volver al inicio</a>
      </div>
    </div>
  );
}
