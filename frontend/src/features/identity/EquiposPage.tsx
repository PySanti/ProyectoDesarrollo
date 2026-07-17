// Vista de solo lectura de todos los equipos (admin/operador), con enlace
// directo al rendimiento histórico de cada equipo (bloque 3b).
import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getEquipos, IdentityApiError, type EquipoAdminItem } from "../../api/identityApi";

type Estado =
  | { status: "cargando" }
  | { status: "ok"; equipos: EquipoAdminItem[] }
  | { status: "error"; message: string };

function estadoPill(estado: string): { cls: string; label: string } {
  if (estado === "Activo") {
    return { cls: "pill--ok", label: estado };
  }
  if (estado === "Eliminado") {
    return { cls: "pill--cancel", label: estado };
  }
  return { cls: "pill--warn", label: estado };
}

export function EquiposPage({ accessToken }: { accessToken: string }) {
  const [estado, setEstado] = useState<Estado>({ status: "cargando" });

  const cargar = useCallback(async () => {
    setEstado({ status: "cargando" });
    try {
      const equipos = await getEquipos(accessToken);
      setEstado({ status: "ok", equipos });
    } catch (caught) {
      setEstado({
        status: "error",
        message:
          caught instanceof IdentityApiError
            ? caught.message
            : "Error inesperado al cargar los equipos."
      });
    }
  }, [accessToken]);

  useEffect(() => {
    void cargar();
  }, [cargar]);

  return (
    <div className="page" data-testid="equipos">
      <div className="card stack">
        <h1>Equipos</h1>
        {estado.status === "cargando" ? <p className="muted">Cargando…</p> : null}
        {estado.status === "error" ? (
          <>
            <div className="notice error" role="alert">
              {estado.message}
            </div>
            <button type="button" onClick={() => void cargar()}>
              Reintentar
            </button>
          </>
        ) : null}
        {estado.status === "ok" ? (
          estado.equipos.length === 0 ? (
            <p className="muted">No hay equipos registrados.</p>
          ) : (
            <div className="table-wrap">
              <table aria-label="Equipos" data-testid="tabla-equipos">
                <thead>
                  <tr>
                    <th scope="col">Nombre</th>
                    <th scope="col">Estado</th>
                    <th scope="col">Miembros</th>
                    <th scope="col">Rendimiento</th>
                  </tr>
                </thead>
                <tbody>
                  {estado.equipos.map((e) => {
                    const pill = estadoPill(e.estado);
                    return (
                      <tr key={e.equipoId}>
                        <td>{e.nombreEquipo}</td>
                        <td>
                          <span className={`pill ${pill.cls}`}>
                            <span className="pill__dot" />
                            {pill.label}
                          </span>
                        </td>
                        <td>
                          <ul>
                            {e.participantes.map((p) => (
                              <li key={p.usuarioId}>
                                {p.esLider ? `${p.nombre} (líder)` : p.nombre}
                              </li>
                            ))}
                          </ul>
                        </td>
                        <td>
                          <Link to={`/puntuaciones/equipos?equipoId=${e.equipoId}`}>
                            Ver rendimiento
                          </Link>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )
        ) : null}
      </div>
    </div>
  );
}
