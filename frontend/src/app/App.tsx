import { useEffect, useMemo, useState } from "react";
import { createBrowserRouter, Navigate, RouterProvider } from "react-router-dom";
import { authProvider, AuthUser } from "../auth/keycloak";
import { AppShell } from "../shell/AppShell";
import { landingPath } from "../shell/navConfig";
import {
  AuthErrorScreen,
  LoadingScreen,
  LoginScreen,
  NotFoundScreen,
  UnauthorizedScreen
} from "../shell/states";
import { CreateBdtGamePage } from "../features/bdt/CreateBdtGamePage";
import { PublishedBdtGamesPage } from "../features/bdt/PublishedBdtGamesPage";
import { CreateUserPage } from "../features/identity/CreateUserPage";
import { GovernancePage } from "../features/identity/GovernancePage";
import { UserManagementPage } from "../features/identity/UserManagementPage";
import { CreateTriviaFormPage } from "../features/trivia/CreateTriviaFormPage";
import { CreateTriviaGamePage } from "../features/trivia/CreateTriviaGamePage";
import { TriviaOperationsPage } from "../features/trivia/TriviaOperationsPage";

type AuthState =
  | { status: "loading" }
  | { status: "error"; message: string }
  | { status: "unauthenticated" }
  | { status: "ready"; user: AuthUser };

function RequireRole({
  roles,
  need,
  landing,
  children
}: {
  roles: string[];
  need: string;
  landing: string;
  children: JSX.Element;
}) {
  return roles.includes(need) ? children : <Navigate to={landing} replace />;
}

export function App() {
  const [authState, setAuthState] = useState<AuthState>({ status: "loading" });
  const [logoutError, setLogoutError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;

    authProvider
      .init()
      .then((user) => {
        if (active) {
          setAuthState(user ? { status: "ready", user } : { status: "unauthenticated" });
        }
      })
      .catch((error: unknown) => {
        if (!active) {
          return;
        }
        const message =
          error instanceof Error ? error.message : "No fue posible autenticar al usuario.";
        setAuthState({ status: "error", message });
      });

    return () => {
      active = false;
    };
  }, []);

  async function onLogout() {
    setLogoutError(null);
    try {
      await authProvider.logout();
    } catch {
      setLogoutError("No fue posible cerrar sesión. Inténtalo nuevamente.");
    }
  }

  const router = useMemo(() => {
    if (authState.status !== "ready") {
      return null;
    }

    const { user } = authState;
    const roles = user.roles;
    const token = user.token;
    const landing = landingPath(roles);

    return createBrowserRouter([
      {
        element: <AppShell roles={roles} userName={user.username} onLogout={onLogout} />,
        children: [
          { index: true, element: <Navigate to={landing} replace /> },
          {
            path: "identidad/usuarios",
            element: (
              <RequireRole roles={roles} need="Administrador" landing={landing}>
                <UserManagementPage accessToken={token} />
              </RequireRole>
            )
          },
          {
            path: "identidad/usuarios/nuevo",
            element: (
              <RequireRole roles={roles} need="Administrador" landing={landing}>
                <CreateUserPage accessToken={token} />
              </RequireRole>
            )
          },
          {
            path: "identidad/gobernanza",
            element: (
              <RequireRole roles={roles} need="Administrador" landing={landing}>
                <GovernancePage accessToken={token} />
              </RequireRole>
            )
          },
          {
            path: "trivia/formularios/nuevo",
            element: (
              <RequireRole roles={roles} need="Operador" landing={landing}>
                <CreateTriviaFormPage accessToken={token} />
              </RequireRole>
            )
          },
          {
            path: "trivia/crear",
            element: (
              <RequireRole roles={roles} need="Operador" landing={landing}>
                <CreateTriviaGamePage accessToken={token} />
              </RequireRole>
            )
          },
          {
            path: "trivia/operar",
            element: (
              <RequireRole roles={roles} need="Operador" landing={landing}>
                <TriviaOperationsPage accessToken={token} />
              </RequireRole>
            )
          },
          {
            path: "bdt/crear",
            element: (
              <RequireRole roles={roles} need="Operador" landing={landing}>
                <CreateBdtGamePage accessToken={token} />
              </RequireRole>
            )
          },
          {
            path: "bdt/partidas",
            element: (
              <RequireRole roles={roles} need="Operador" landing={landing}>
                <PublishedBdtGamesPage accessToken={token} />
              </RequireRole>
            )
          },
          { path: "*", element: <NotFoundScreen /> }
        ]
      }
    ]);
    // onLogout is stable enough (only calls stable setters / authProvider).
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [authState]);

  if (authState.status === "loading") {
    return <LoadingScreen />;
  }

  if (authState.status === "error") {
    return <AuthErrorScreen message={authState.message} onRetry={() => void authProvider.login()} />;
  }

  if (authState.status === "unauthenticated") {
    return <LoginScreen onLogin={() => void authProvider.login()} />;
  }

  const roles = authState.user.roles;
  if (!roles.includes("Administrador") && !roles.includes("Operador")) {
    return <UnauthorizedScreen username={authState.user.username} onLogout={onLogout} />;
  }

  return (
    <>
      <RouterProvider router={router!} />
      {logoutError ? (
        <div
          className="notice error"
          role="alert"
          style={{
            position: "fixed",
            right: "var(--sp-lg)",
            bottom: "var(--sp-lg)",
            zIndex: 60,
            maxWidth: "360px",
            boxShadow: "var(--shadow-overlay)"
          }}
        >
          {logoutError}
        </div>
      ) : null}
    </>
  );
}
