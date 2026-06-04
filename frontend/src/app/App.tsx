import { useEffect, useMemo, useState } from "react";
import { authProvider, AuthUser } from "../auth/keycloak";
import { CreateBdtGamePage } from "../features/bdt/CreateBdtGamePage";
import { PublishedBdtGamesPage } from "../features/bdt/PublishedBdtGamesPage";
import { CreateUserPage } from "../features/identity/CreateUserPage";
import { UserManagementPage } from "../features/identity/UserManagementPage";
import { CreateTriviaGamePage } from "../features/trivia/CreateTriviaGamePage";
import { TriviaOperationsPage } from "../features/trivia/TriviaOperationsPage";

type WebView = "hu01" | "hu02" | "hu15" | "hu17" | "hu34" | "hu37";

type AuthState =
  | { status: "loading" }
  | { status: "error"; message: string }
  | { status: "ready"; user: AuthUser };

export function App() {
  const [authState, setAuthState] = useState<AuthState>({ status: "loading" });
  const [view, setView] = useState<WebView>("hu01");

  useEffect(() => {
    let active = true;

    authProvider
      .init()
      .then((user) => {
        if (active) {
          setAuthState({ status: "ready", user });
        }
      })
      .catch((error: unknown) => {
        if (!active) {
          return;
        }

        const message =
          error instanceof Error
            ? error.message
            : "No fue posible autenticar al usuario.";
        setAuthState({ status: "error", message });
      });

    return () => {
      active = false;
    };
  }, []);

  const isAdmin = useMemo(() => {
    if (authState.status !== "ready") {
      return false;
    }

    return authState.user.roles.includes("Administrador");
  }, [authState]);

  const isOperator = useMemo(() => {
    if (authState.status !== "ready") {
      return false;
    }

    return authState.user.roles.includes("Operador");
  }, [authState]);

  useEffect(() => {
    if (authState.status !== "ready") {
      return;
    }

    const canSeeAdminView = isAdmin && (view === "hu01" || view === "hu02");
    const canSeeOperatorView = isOperator && ["hu15", "hu17", "hu34", "hu37"].includes(view);

    if (!canSeeAdminView && !canSeeOperatorView) {
      setView(isOperator ? "hu17" : "hu01");
    }
  }, [authState.status, isAdmin, isOperator, view]);

  if (authState.status === "loading") {
    return (
      <div className="page">
        <div className="card">Autenticando con Keycloak...</div>
      </div>
    );
  }

  if (authState.status === "error") {
    return (
      <div className="page">
        <div className="card">
          <div className="notice error" role="alert">
            {authState.message}
          </div>
        </div>
      </div>
    );
  }

  if (!isAdmin && !isOperator) {
    return (
      <div className="page">
        <div className="card">
          <h1>Acceso restringido</h1>
          <p>
            El usuario autenticado ({authState.user.username}) no tiene rol
            Administrador u Operador.
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="page wide app-shell">
      <div className="card app-header">
        <p className="eyebrow">Primer sprint</p>
        <h1>UMBRAL Web - Administracion y Operacion</h1>
        <p className="muted">Selecciona un flujo activo del primer sprint.</p>
        <div className="nav-grid" aria-label="Flujos web disponibles">
          {isAdmin ? (
            <>
              <button className={`nav-button ${view === "hu01" ? "active" : ""}`} type="button" onClick={() => setView("hu01")}>HU-01 Crear usuario</button>
              <button className={`nav-button ${view === "hu02" ? "active" : ""}`} type="button" onClick={() => setView("hu02")}>
                HU-02 Gestionar usuarios
              </button>
            </>
          ) : null}
          {isOperator ? (
            <>
              <button className={`nav-button ${view === "hu17" ? "active" : ""}`} type="button" onClick={() => setView("hu17")}>HU-17 Crear Trivia</button>
              <button className={`nav-button ${view === "hu15" ? "active" : ""}`} type="button" onClick={() => setView("hu15")}>HU-15/22/23/24/30 Operar Trivia</button>
              <button className={`nav-button ${view === "hu34" ? "active" : ""}`} type="button" onClick={() => setView("hu34")}>HU-34 Crear BDT</button>
              <button className={`nav-button ${view === "hu37" ? "active" : ""}`} type="button" onClick={() => setView("hu37")}>HU-37 Listar BDT</button>
            </>
          ) : null}
        </div>
      </div>

      <div className="stack">
        {view === "hu17" && isOperator ? (
          <CreateTriviaGamePage accessToken={authState.user.token} />
        ) : view === "hu15" && isOperator ? (
          <TriviaOperationsPage accessToken={authState.user.token} />
        ) : view === "hu34" && isOperator ? (
          <CreateBdtGamePage accessToken={authState.user.token} />
        ) : view === "hu37" && isOperator ? (
          <PublishedBdtGamesPage accessToken={authState.user.token} />
        ) : view === "hu01" && isAdmin ? (
          <CreateUserPage accessToken={authState.user.token} />
        ) : view === "hu02" && isAdmin ? (
          <UserManagementPage accessToken={authState.user.token} />
        ) : isOperator ? (
          <CreateTriviaGamePage accessToken={authState.user.token} />
        ) : (
          <CreateUserPage accessToken={authState.user.token} />
        )}
      </div>
    </div>
  );
}
