// Rendimiento histórico de un equipo (HU-49/RF-44): posición y victoria por partida
// terminada. El equipo se elige de la lista (GET /identity/teams); deep-link ?equipoId=
// desde la vista de equipos preselecciona y consulta al montar.
import { useEffect, useRef, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { getEquipos, IdentityApiError, type EquipoAdminItem } from "../../api/identityApi";
import {
  getRendimientoEquipo,
  PuntuacionesApiError,
  type RendimientoEquipoDto
} from "../../api/puntuacionesApi";

const GUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

type Estado =
  | { status: "inicial" }
  | { status: "cargando" }
  | { status: "ok"; rendimiento: RendimientoEquipoDto }
  | { status: "error"; message: string };

export function RendimientoEquipoPage({ accessToken }: { accessToken: string }) {
  const [searchParams] = useSearchParams();
  const [equipos, setEquipos] = useState<EquipoAdminItem[] | null>(null);
  const [equiposError, setEquiposError] = useState<string | null>(null);
  const [equipoId, setEquipoId] = useState("");
  const [estado, setEstado] = useState<Estado>({ status: "inicial" });

  async function consultar(id: string) {
    setEstado({ status: "cargando" });
    try {
      const rendimiento = await getRendimientoEquipo(id, accessToken);
      setEstado({ status: "ok", rendimiento });
    } catch (caught) {
      setEstado({
        status: "error",
        message:
          caught instanceof PuntuacionesApiError
            ? caught.message
            : "Error inesperado al consultar el rendimiento."
      });
    }
  }

  // Carga la lista y aplica el deep-link una sola vez al montar.
  const inicializado = useRef(false);
  useEffect(() => {
    if (inicializado.current) return;
    inicializado.current = true;
    void (async () => {
      try {
        setEquipos(await getEquipos(accessToken));
      } catch (caught) {
        setEquipos([]);
        setEquiposError(
          caught instanceof IdentityApiError
            ? caught.message
            : "No se pudieron cargar los equipos."
        );
      }
      const fromQuery = searchParams.get("equipoId")?.trim() ?? "";
      if (GUID_RE.test(fromQuery)) {
        setEquipoId(fromQuery);
        void consultar(fromQuery);
      }
    })();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function onSelect(id: string) {
    setEquipoId(id);
    if (id) {
      void consultar(id);
    } else {
      setEstado({ status: "inicial" });
    }
  }

  return (
    <div className="page" data-testid="rendimiento-equipo">
      <div className="card stack">
        <h1>Rendimiento de equipo</h1>
        <p className="muted">Panel para consulta de rendimiento de equipos</p>
        <label>
          Equipo{" "}
          <select
            value={equipoId}
            aria-label="Equipo"
            disabled={equipos === null || estado.status === "cargando"}
            onChange={(e) => onSelect(e.target.value)}
          >
            <option value="">Selecciona un equipo…</option>
            {(equipos ?? []).map((eq) => (
              <option key={eq.equipoId} value={eq.equipoId}>
                {eq.nombreEquipo}
              </option>
            ))}
          </select>
        </label>
        {equipos === null && !equiposError ? <p className="muted">Cargando equipos…</p> : null}
        {equiposError ? (
          <div className="notice error" role="alert">
            {equiposError}
          </div>
        ) : null}

        {estado.status === "cargando" ? <p className="muted">Consultando…</p> : null}
        {estado.status === "error" ? (
          <div className="notice error" role="alert">
            {estado.message}
          </div>
        ) : null}
        {estado.status === "ok" ? (
          estado.rendimiento.partidas.length === 0 ? (
            <p className="muted">El equipo no tiene participaciones en partidas terminadas.</p>
          ) : (
            <div className="table-wrap">
              <table aria-label="Rendimiento del equipo" data-testid="tabla-rendimiento">
                <thead>
                  <tr>
                    <th scope="col">Partida</th>
                    <th scope="col">Fecha fin</th>
                    <th scope="col">Posición</th>
                    <th scope="col">Ganó</th>
                  </tr>
                </thead>
                <tbody>
                  {estado.rendimiento.partidas.map((p) => (
                    <tr key={p.partidaId}>
                      <td>{p.partidaId.slice(0, 8)}</td>
                      <td>{new Date(p.fechaFin).toLocaleString()}</td>
                      <td>{p.posicion}</td>
                      <td aria-label={p.gano ? "Sí" : "No"}>{p.gano ? "✓" : "—"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )
        ) : null}
      </div>
    </div>
  );
}
