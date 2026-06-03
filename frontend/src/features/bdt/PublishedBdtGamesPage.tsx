import { useEffect, useState } from "react";
import {
  BdtApiError,
  getOperatorPublishedBdtGames,
  PublishedBdtGameItem
} from "../../api/bdtApi";

interface PublishedBdtGamesPageProps {
  accessToken: string;
}

type LoadState =
  | { status: "loading" }
  | { status: "error"; message: string }
  | { status: "ready"; games: PublishedBdtGameItem[] };

export function PublishedBdtGamesPage({ accessToken }: PublishedBdtGamesPageProps) {
  const [state, setState] = useState<LoadState>({ status: "loading" });

  useEffect(() => {
    let active = true;

    getOperatorPublishedBdtGames(accessToken)
      .then((games) => {
        if (active) {
          setState({ status: "ready", games });
        }
      })
      .catch((caught) => {
        if (!active) {
          return;
        }

        if (caught instanceof BdtApiError) {
          setState({ status: "error", message: mapErrorMessage(caught.statusCode, caught.message) });
        } else {
          setState({ status: "error", message: "Error inesperado al consultar partidas BDT publicadas." });
        }
      });

    return () => {
      active = false;
    };
  }, [accessToken]);

  return (
    <div className="page">
      <div className="card">
        <h1>Partidas BDT publicadas</h1>
        <p>Flujo HU-37 para operadores usando BDT Game Service.</p>

        {state.status === "loading" ? <div className="notice">Cargando partidas BDT publicadas...</div> : null}

        {state.status === "error" ? (
          <div role="alert" className="notice error">
            {state.message}
          </div>
        ) : null}

        {state.status === "ready" && state.games.length === 0 ? (
          <div className="notice" data-testid="bdt-published-empty">
            No hay partidas BDT publicadas.
          </div>
        ) : null}

        {state.status === "ready" && state.games.length > 0 ? (
          <table aria-label="Partidas BDT publicadas para operador">
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Estado</th>
                <th>Modalidad</th>
                <th>Area</th>
                <th>Etapas</th>
              </tr>
            </thead>
            <tbody>
              {state.games.map((game) => (
                <tr key={game.partidaId}>
                  <td>{game.nombre}</td>
                  <td>{game.estado}</td>
                  <td>{game.modalidad}</td>
                  <td>{game.areaBusqueda}</td>
                  <td>{game.cantidadEtapas}</td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : null}
      </div>
    </div>
  );
}

function mapErrorMessage(statusCode: number, fallbackMessage: string): string {
  switch (statusCode) {
    case 401:
      return "Sesion expirada o no autenticada. Inicia sesion nuevamente.";
    case 403:
      return "No autorizado. Debes tener rol Operador.";
    case 500:
      return "Error de persistencia al consultar BDT Game Service.";
    default:
      return fallbackMessage;
  }
}
