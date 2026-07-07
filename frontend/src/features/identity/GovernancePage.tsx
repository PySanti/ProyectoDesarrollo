import { useEffect, useState } from "react";
import {
  getGovernanceRoles,
  IdentityApiError,
  PermisoFuncional,
  RolePermissions,
  updateRolePermissions
} from "../../api/identityApi";
import { Lock } from "../../shell/icons";

interface GovernancePageProps {
  accessToken: string;
}

const PERMISOS: { key: PermisoFuncional; label: string }[] = [
  { key: "GestionarPartidas", label: "Gestionar partidas" },
  { key: "GestionarEquipos", label: "Gestionar equipos" },
  { key: "ParticiparEnPartidas", label: "Participar en partidas" }
];

interface CardState {
  info: RolePermissions;
  /* Último set confirmado por el servidor; Guardar solo se habilita si marked difiere. */
  confirmed: PermisoFuncional[];
  marked: PermisoFuncional[];
  saving: boolean;
  error: string | null;
  saved: boolean;
}

function sameSet(a: PermisoFuncional[], b: PermisoFuncional[]): boolean {
  return a.length === b.length && a.every((permiso) => b.includes(permiso));
}

function mapGovernanceError(caught: unknown): string {
  if (caught instanceof IdentityApiError) {
    if (caught.statusCode === 502) {
      return "Keycloak no disponible. Reintenta: volver a guardar repara el estado.";
    }
    return caught.message || "Error inesperado en Identity Service.";
  }
  return "Error inesperado al guardar permisos.";
}

export function GovernancePage({ accessToken }: GovernancePageProps) {
  const [cards, setCards] = useState<CardState[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);

  useEffect(() => {
    void loadMatriz();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function loadMatriz() {
    setIsLoading(true);
    setLoadError(null);
    try {
      const response = await getGovernanceRoles(accessToken);
      setCards(
        response.roles.map((info) => ({
          info,
          confirmed: info.permisos,
          marked: info.permisos,
          saving: false,
          error: null,
          saved: false
        }))
      );
    } catch (caught) {
      setLoadError(
        caught instanceof IdentityApiError
          ? caught.message || "No fue posible cargar la matriz de permisos."
          : "No fue posible cargar la matriz de permisos."
      );
    } finally {
      setIsLoading(false);
    }
  }

  function toggle(rol: string, permiso: PermisoFuncional) {
    setCards((current) =>
      current.map((card) => {
        if (card.info.rol !== rol) {
          return card;
        }
        const marked = card.marked.includes(permiso)
          ? card.marked.filter((p) => p !== permiso)
          : [...card.marked, permiso];
        return { ...card, marked, error: null, saved: false };
      })
    );
  }

  async function save(rol: string) {
    const card = cards.find((c) => c.info.rol === rol);
    if (!card || card.saving) {
      return;
    }

    setCards((current) =>
      current.map((c) => (c.info.rol === rol ? { ...c, saving: true, error: null, saved: false } : c))
    );

    try {
      /* PUT set completo (E5): el backend hace el diff server-side. */
      const permisosOrdenados = PERMISOS.map((p) => p.key).filter((p) => card.marked.includes(p));
      const updated = await updateRolePermissions(rol, permisosOrdenados, accessToken);
      setCards((current) =>
        current.map((c) =>
          c.info.rol === rol
            ? {
                ...c,
                confirmed: updated.permisos,
                marked: updated.permisos,
                saving: false,
                saved: true
              }
            : c
        )
      );
    } catch (caught) {
      setCards((current) =>
        current.map((c) =>
          c.info.rol === rol ? { ...c, saving: false, error: mapGovernanceError(caught) } : c
        )
      );
    }
  }

  return (
    <div className="page">
      <div className="stack">
        <div>
          <h1>Gobernanza</h1>
          <p className="muted">
            Permisos funcionales por rol. Los cambios se aplican primero en Keycloak y luego se
            registran en UMBRAL; los usuarios los reciben en su próximo token.
          </p>
        </div>

        {loadError ? (
          <div className="notice error" role="alert" data-testid="gov-load-error">
            {loadError}{" "}
            <button type="button" className="secondary-button" onClick={loadMatriz}>
              Reintentar
            </button>
          </div>
        ) : null}

        {isLoading ? <p className="muted">Cargando matriz de permisos…</p> : null}

        {cards.map((card) => (
          <div className="card stack" key={card.info.rol} data-testid={`gov-card-${card.info.rol}`}>
            <div className="card-head">
              <h2 className="q-title">{card.info.rol}</h2>
              {card.info.privilegiosGobernanza ? (
                <span className="badge" data-testid="gov-badge-admin">
                  <Lock /> Privilegios de gobernanza — protegidos
                </span>
              ) : null}
            </div>

            {PERMISOS.map((permiso) => (
              <label key={permiso.key} className="check-row">
                <input
                  type="checkbox"
                  data-testid={`gov-check-${card.info.rol}-${permiso.key}`}
                  checked={card.marked.includes(permiso.key)}
                  disabled={card.saving}
                  onChange={() => toggle(card.info.rol, permiso.key)}
                />
                {permiso.label}
              </label>
            ))}

            {card.error ? (
              <div className="notice error" role="alert" data-testid={`gov-error-${card.info.rol}`}>
                {card.error}
              </div>
            ) : null}

            {card.saved ? (
              <p className="muted" role="status">
                Permisos de {card.info.rol} guardados.
              </p>
            ) : null}

            <div className="actions">
              <button
                type="button"
                data-testid={`gov-save-${card.info.rol}`}
                disabled={card.saving || sameSet(card.marked, card.confirmed)}
                onClick={() => save(card.info.rol)}
              >
                {card.saving ? "Guardando…" : "Guardar"}
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
