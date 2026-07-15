import { useEffect, useMemo, useState } from "react";
import { createBrowserRouter, Navigate, RouterProvider } from "react-router-dom";
import { authProvider, AuthUser } from "../auth/keycloak";
import { useSessionRefresh } from "../auth/useSessionRefresh";
import { SessionExpiryModal } from "../auth/SessionExpiryModal";
import { AppShell } from "../shell/AppShell";
import { areasForRoles, landingPath } from "../shell/navConfig";
import {
  AuthErrorScreen,
  LoadingScreen,
  LoginScreen,
  NotFoundScreen,
  UnauthorizedScreen
} from "../shell/states";
import { CreateUserPage } from "../features/identity/CreateUserPage";
import { EquiposPage } from "../features/identity/EquiposPage";
import { GovernancePage } from "../features/identity/GovernancePage";
import { TeamsAdminPage } from "../features/identity/TeamsAdminPage";
import { UserManagementPage } from "../features/identity/UserManagementPage";
import { CreatePartidaPage } from "../features/partidas/CreatePartidaPage";
import { HistorialPartidaPage } from "../features/partidas/HistorialPartidaPage";
import { PartidaDetailPage } from "../features/partidas/PartidaDetailPage";
import { PartidasListPage } from "../features/partidas/PartidasListPage";
import { SesionOperadorPage } from "../features/partidas/SesionOperadorPage";
import { RendimientoEquipoPage } from "../features/puntuaciones/RendimientoEquipoPage";

type AuthState =
  | { status: "loading" }
  | { status: "error"; message: string }
  | { status: "unauthenticated" }
  | { status: "ready"; user: AuthUser };

/* Guardia de ruta: `have` son las credenciales del usuario (roles base o privilegios funcionales) y
   `need` las que la ruta exige. Basta una coincidencia. */
function Require({
  have,
  need,
  landing,
  children
}: {
  have: string[];
  need: string | readonly string[];
  landing: string;
  children: JSX.Element;
}) {
  const allowed = typeof need === "string" ? [need] : need;
  return have.some((credencial) => allowed.includes(credencial)) ? (
    children
  ) : (
    <Navigate to={landing} replace />
  );
}

export function App() {
  const [authState, setAuthState] = useState<AuthState>({ status: "loading" });
  const [logoutError, setLogoutError] = useState<string | null>(null);

  const sesionExpiradaKey = "umbral.sesion.expirada";
  const { modalVisible, continuar } = useSessionRefresh({
    enabled: authState.status === "ready",
    onUsuario: (user) => setAuthState({ status: "ready", user }),
    onExpired: () => {
      // El logout de Keycloak recarga la página: el aviso sobrevive en sessionStorage.
      window.sessionStorage.setItem(sesionExpiradaKey, "1");
      void authProvider.logout();
    }
  });

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

    const { roles, permisos, token, username } = authState.user;
    const landing = landingPath(roles, permisos) ?? "/";
    /* El área Partidas exige el privilegio, así que dentro de ella siempre se puede operar. */
    const puedeOperar = permisos.includes("GestionarPartidas");

    return createBrowserRouter([
      {
        element: <AppShell roles={roles} permisos={permisos} userName={username} onLogout={onLogout} />,
        children: [
          { index: true, element: <Navigate to={landing} replace /> },
          {
            path: "identidad/usuarios",
            element: (
              <Require have={roles} need="Administrador" landing={landing}>
                <UserManagementPage accessToken={token} />
              </Require>
            )
          },
          {
            path: "identidad/usuarios/nuevo",
            element: (
              <Require have={roles} need="Administrador" landing={landing}>
                <CreateUserPage accessToken={token} />
              </Require>
            )
          },
          {
            path: "identidad/gobernanza",
            element: (
              <Require have={roles} need="Administrador" landing={landing}>
                <GovernancePage accessToken={token} />
              </Require>
            )
          },
          {
            // Sólo el privilegio: el rol no veta. Igual que «equipos» y «puntuaciones/equipos»
            // más abajo (D6, gobernanza).
            path: "identidad/equipos",
            element: (
              <Require have={permisos} need="GestionarEquipos" landing={landing}>
                <TeamsAdminPage accessToken={token} />
              </Require>
            )
          },
          {
            path: "partidas",
            element: (
              <Require have={permisos} need="GestionarPartidas" landing={landing}>
                <PartidasListPage accessToken={token} puedeOperar={puedeOperar} />
              </Require>
            )
          },
          {
            path: "partidas/crear",
            element: (
              <Require have={permisos} need="GestionarPartidas" landing={landing}>
                <CreatePartidaPage accessToken={token} />
              </Require>
            )
          },
          {
            path: "partidas/:partidaId",
            element: (
              <Require have={permisos} need="GestionarPartidas" landing={landing}>
                <PartidaDetailPage accessToken={token} puedeOperar={puedeOperar} />
              </Require>
            )
          },
          {
            path: "partidas/:partidaId/sesion",
            element: (
              <Require have={permisos} need="GestionarPartidas" landing={landing}>
                <SesionOperadorPage accessToken={token} puedeOperar={puedeOperar} />
              </Require>
            )
          },
          {
            path: "partidas/:partidaId/historial",
            element: (
              <Require have={permisos} need="GestionarPartidas" landing={landing}>
                <HistorialPartidaPage accessToken={token} />
              </Require>
            )
          },
          {
            path: "puntuaciones/equipos",
            element: (
              <Require have={permisos} need="GestionarEquipos" landing={landing}>
                <RendimientoEquipoPage accessToken={token} />
              </Require>
            )
          },
          {
            path: "equipos",
            element: (
              <Require have={permisos} need="GestionarEquipos" landing={landing}>
                <EquiposPage accessToken={token} />
              </Require>
            )
          },
          { path: "*", element: <NotFoundScreen /> }
        ]
      }
    ]);
    // onLogout is stable enough (only calls stable setters / authProvider).
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [authState]);

  // El router se recrea cuando authState rota el token (RNF-24, cada 270s);
  // RouterProvider no hace dispose del anterior, así que lo hacemos aquí para
  // no acumular listeners de history huérfanos.
  useEffect(() => {
    return () => {
      router?.dispose();
    };
  }, [router]);

  if (authState.status === "loading") {
    return <LoadingScreen />;
  }

  if (authState.status === "error") {
    return <AuthErrorScreen message={authState.message} onRetry={() => void authProvider.login()} />;
  }

  if (authState.status === "unauthenticated") {
    const expirada = window.sessionStorage.getItem(sesionExpiradaKey) === "1";
    if (expirada) {
      window.sessionStorage.removeItem(sesionExpiradaKey);
    }
    return (
      <LoginScreen
        onLogin={() => void authProvider.login()}
        notice={expirada ? "Tu sesión expiró. Inicia sesión de nuevo." : null}
      />
    );
  }

  const { roles, permisos } = authState.user;
  /* Sin ningún área no hay dónde aterrizar: cubre al participante que entra a la web (ningún área
     es suya) y al operador al que le retiraron los privilegios. Sin esto, el landing sería null y
     el index redirigiría a la nada. */
  if (areasForRoles(roles, permisos).length === 0) {
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
      <SessionExpiryModal
        visible={modalVisible}
        onContinuar={continuar}
        onSalir={() => void onLogout()}
      />
    </>
  );
}
