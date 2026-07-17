// Rendimiento histórico de un equipo (HU-49/RF-44): posición y victoria por partida
// terminada. Se elige el equipo por nombre, o se llega ya resuelto vía ?equipoId= desde la
// vista de equipos (3b), que es el camino natural desde su columna "Ver rendimiento".
import { useEffect, useRef, useState } from "react";
import { useSearchParams } from "react-router-dom";
import {
  getRendimientoEquipo,
  PuntuacionesApiError,
  type RendimientoEquipoDto
} from "../../api/puntuacionesApi";
import { listAdminTeams, type AdminTeam } from "../../api/adminTeamsApi";
import { useNombresPartida } from "../shared/useNombresPartida";

const GUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

// El listado no filtra por estado a propósito: esta pantalla es historial, y un equipo
// eliminado o desactivado conserva el suyo. Se marca el estado para que el operador sepa
// qué está eligiendo, con el mismo vocabulario que la vista de equipos.
function etiquetaEquipo(equipo: AdminTeam): string {
  return equipo.estado === "Activo"
    ? equipo.nombreEquipo
    : `${equipo.nombreEquipo} (${equipo.estado.toLowerCase()})`;
}

type Estado =
  | { status: "inicial" }
  | { status: "cargando" }
  | { status: "ok"; rendimiento: RendimientoEquipoDto }
  | { status: "error"; message: string };

export function RendimientoEquipoPage({ accessToken }: { accessToken: string }) {
  const [searchParams] = useSearchParams();
  const [equipoId, setEquipoId] = useState("");
  const [formError, setFormError] = useState<string | null>(null);
  const [estado, setEstado] = useState<Estado>({ status: "inicial" });
  const [equipos, setEquipos] = useState<AdminTeam[] | null>(null);
  // Sin lista no hay desplegable: se cae al ID manual en vez de dejar la pantalla sin salida.
  const [listaCaida, setListaCaida] = useState(false);
  const nombrePartidaDe = useNombresPartida(accessToken);

  useEffect(() => {
    let active = true;
    listAdminTeams(accessToken)
      .then((lista) => {
        if (active) setEquipos(lista);
      })
      .catch(() => {
        if (active) setListaCaida(true);
      });
    return () => {
      active = false;
    };
  }, [accessToken]);

  async function consultar(id: string) {
    setFormError(null);
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

  // Deep-link desde la vista de equipos: precarga y consulta una sola vez al montar.
  const autoConsultado = useRef(false);
  useEffect(() => {
    if (autoConsultado.current) return;
    autoConsultado.current = true;
    const fromQuery = searchParams.get("equipoId")?.trim() ?? "";
    if (GUID_RE.test(fromQuery)) {
      setEquipoId(fromQuery);
      void consultar(fromQuery);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Un ?equipoId= puede apuntar a un equipo que el listado no trae (lista aún cargando, o id
  // ajeno al listado). Se le añade una opción con su GUID corto: el selector debe reflejar lo
  // que se está consultando en vez de aparentar que no se eligió nada.
  const listaEquipos = equipos ?? [];
  const opciones =
    equipoId && !listaEquipos.some((e) => e.equipoId === equipoId)
      ? [
          ...listaEquipos,
          { equipoId, nombreEquipo: equipoId.slice(0, 8), estado: "Activo", integrantes: [] } as AdminTeam
        ]
      : listaEquipos;

  async function onConsultar(e: React.FormEvent) {
    e.preventDefault();
    const id = equipoId.trim();
    if (!GUID_RE.test(id)) {
      setFormError("Ingresa un ID de equipo válido (GUID).");
      setEstado({ status: "inicial" });
      return;
    }
    await consultar(id);
  }

  return (
    <div className="page" data-testid="rendimiento-equipo">
      <div className="card stack">
        <h1>Rendimiento de equipo</h1>
        {listaCaida ? (
          <>
            <div className="notice error" role="alert">
              No se pudo cargar la lista de equipos. Puedes consultar por ID mientras tanto.
            </div>
            <form className="compact-actions" onSubmit={(e) => void onConsultar(e)}>
              <label>
                ID del equipo{" "}
                <input
                  value={equipoId}
                  aria-label="ID del equipo"
                  placeholder="00000000-0000-0000-0000-000000000000"
                  disabled={estado.status === "cargando"}
                  onChange={(e) => {
                    setEquipoId(e.target.value);
                    setFormError(null);
                    setEstado({ status: "inicial" });
                  }}
                />
              </label>
              <button type="submit" disabled={estado.status === "cargando"}>
                Consultar
              </button>
            </form>
          </>
        ) : (
          <div className="compact-actions">
            <label>
              Equipo{" "}
              <select
                value={equipoId}
                aria-label="Equipo"
                disabled={estado.status === "cargando" || equipos === null}
                onChange={(e) => {
                  const id = e.target.value;
                  setEquipoId(id);
                  if (id) {
                    void consultar(id);
                  } else {
                    setEstado({ status: "inicial" });
                  }
                }}
              >
                <option value="">{equipos === null ? "Cargando equipos…" : "— elige equipo —"}</option>
                {opciones.map((equipo) => (
                  <option key={equipo.equipoId} value={equipo.equipoId}>
                    {etiquetaEquipo(equipo)}
                  </option>
                ))}
              </select>
            </label>
          </div>
        )}
        {formError ? (
          <div className="notice error" role="alert">
            {formError}
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
                      <td>{nombrePartidaDe(p.partidaId)}</td>
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
