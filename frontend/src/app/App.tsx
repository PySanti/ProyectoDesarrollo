import { useEffect, useMemo, useState } from "react";
import { authProvider, AuthUser } from "../auth/keycloak";
import { CreateBdtGamePage } from "../features/bdt/CreateBdtGamePage";
import { PublishedBdtGamesPage } from "../features/bdt/PublishedBdtGamesPage";
import { CreateUserPage } from "../features/identity/CreateUserPage";
import { UserManagementPage } from "../features/identity/UserManagementPage";

type WebView = "hu01" | "hu02" | "hu34" | "hu37";

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
    <div className="page">
      <div className="card">
        <h1>UMBRAL Web - Administracion y Operacion</h1>
        <p>Selecciona un flujo activo del primer sprint.</p>
        <div className="row">
          {isAdmin ? (
            <>
              <button type="button" onClick={() => setView("hu01")}>HU-01 Crear usuario</button>
              <button type="button" onClick={() => setView("hu02")}>
                HU-02 Gestionar usuarios
              </button>
            </>
          ) : null}
          {isOperator ? (
            <>
              <button type="button" onClick={() => setView("hu34")}>HU-34 Crear BDT</button>
              <button type="button" onClick={() => setView("hu37")}>HU-37 Listar BDT</button>
            </>
          ) : null}
        </div>
      </div>

      <div style={{ marginTop: 16 }}>
        {view === "hu34" && isOperator ? (
          <CreateBdtGamePage accessToken={authState.user.token} />
        ) : view === "hu37" && isOperator ? (
          <PublishedBdtGamesPage accessToken={authState.user.token} />
        ) : view === "hu01" && isAdmin ? (
          <CreateUserPage accessToken={authState.user.token} />
        ) : view === "hu02" && isAdmin ? (
          <UserManagementPage accessToken={authState.user.token} />
        ) : (
          <CreateBdtGamePage accessToken={authState.user.token} />
        )}
      </div>
    </div>
  );
}
