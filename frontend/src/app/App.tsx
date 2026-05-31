import { useEffect, useMemo, useState } from "react";
import { authProvider, AuthUser } from "../auth/keycloak";
import { CreateUserPage } from "../features/identity/CreateUserPage";

type AuthState =
  | { status: "loading" }
  | { status: "error"; message: string }
  | { status: "ready"; user: AuthUser };

export function App() {
  const [authState, setAuthState] = useState<AuthState>({ status: "loading" });

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

  if (!isAdmin) {
    return (
      <div className="page">
        <div className="card">
          <h1>Acceso restringido</h1>
          <p>
            El usuario autenticado ({authState.user.username}) no tiene rol
            Administrador.
          </p>
        </div>
      </div>
    );
  }

  return <CreateUserPage accessToken={authState.user.token} />;
}
