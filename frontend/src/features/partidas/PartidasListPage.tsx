// Indice de partidas: lectura de getPartidas + navegacion a crear/detalle.
import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { getPartidas, PartidasApiError, type PartidaSummary } from "../../api/partidasApi";
import { ListChecks } from "../../shell/icons";

interface PartidasListPageProps {
  accessToken: string;
  puedeOperar: boolean;
}

type LoadState =
  | { status: "loading" }
  | { status: "error"; message: string }
  | { status: "ready"; partidas: PartidaSummary[] };

export function PartidasListPage({ accessToken, puedeOperar }: PartidasListPageProps) {
  const navigate = useNavigate();
  const [state, setState] = useState<LoadState>({ status: "loading" });

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [accessToken]);

  async function load() {
    setState({ status: "loading" });
    try {
      const partidas = await getPartidas(accessToken);
      setState({ status: "ready", partidas });
    } catch (caught) {
      setState({
        status: "error",
        message:
          caught instanceof PartidasApiError
            ? mapErrorMessage(caught.statusCode, caught.message)
            : "Error inesperado al consultar partidas."
      });
    }
  }

  return (
    <div className="page" data-testid="lista-partidas">
      <div className="card stack">
        <header className="create-head">
          <div>
            <h1>Partidas</h1>
            <p className="muted">Consulta las partidas creadas y su estado de publicación.</p>
          </div>
          {puedeOperar ? (
            <button
              type="button"
              data-testid="btn-nueva-partida"
              onClick={() => navigate("/partidas/crear")}
            >
              Nueva partida
            </button>
          ) : null}
        </header>

        {state.status === "loading" ? <p className="muted">Cargando partidas…</p> : null}

        {state.status === "error" ? (
          <div className="notice error" role="alert">
            {state.message}
            <div className="row">
              <button type="button" className="secondary-button" onClick={() => void load()}>
                Reintentar
              </button>
            </div>
          </div>
        ) : null}

        {state.status === "ready" && state.partidas.length === 0 ? (
          <div className="empty-panel">
            <ListChecks />
            <p>No hay partidas creadas todavía.</p>
            <p className="muted">
              Crea la primera con <strong>Nueva partida</strong>.
            </p>
          </div>
        ) : null}

        {state.status === "ready" && state.partidas.length > 0 ? (
          <div className="table-wrap">
            <table aria-label="Partidas">
              <thead>
                <tr>
                  <th scope="col">Nombre</th>
                  <th scope="col">Modalidad</th>
                  <th scope="col">Modo de inicio</th>
                  <th scope="col">Juegos</th>
                  <th scope="col">Estado</th>
                </tr>
              </thead>
              <tbody>
                {state.partidas.map((partida) => {
                  const pill = estadoPill(partida.estado);
                  return (
                    <tr key={partida.partidaId} data-testid={`fila-partida-${partida.partidaId}`}>
                      <td>
                        <button
                          type="button"
                          className="row-link"
                          onClick={() => navigate(`/partidas/${partida.partidaId}`)}
                        >
                          {partida.nombrePartida}
                        </button>
                      </td>
                      <td>{partida.modalidad}</td>
                      <td>{partida.modoInicioPartida}</td>
                      <td>{partida.cantidadJuegos}</td>
                      <td>
                        <span className={`pill ${pill.cls}`}>
                          <span className="pill__dot" />
                          {pill.label}
                        </span>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        ) : null}
      </div>
    </div>
  );
}

function estadoPill(estado: string | null): { cls: string; label: string } {
  if (estado === null) {
    return { cls: "pill--warn", label: "Sin publicar" };
  }
  if (estado === "Iniciada") {
    return { cls: "pill--live", label: estado };
  }
  if (estado === "Lobby") {
    return { cls: "pill--lobby", label: estado };
  }
  return { cls: "pill--done", label: estado };
}

function mapErrorMessage(statusCode: number, fallbackMessage: string): string {
  switch (statusCode) {
    case 401:
      return "Sesión expirada o no autenticada. Inicia sesión nuevamente.";
    case 403:
      return "No autorizado. Debes tener rol Operador.";
    case 500:
      return "Error de persistencia al consultar Partidas Service.";
    default:
      return fallbackMessage || "Error inesperado al consultar partidas.";
  }
}
