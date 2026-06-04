import { useEffect, useState } from "react";
import {
  BdtApiError,
  getOperatorPublishedBdtGames,
  PublishedBdtGameItem,
  StartBdtGameResponse,
  startBdtGame
} from "../../api/bdtApi";

interface PublishedBdtGamesPageProps {
  accessToken: string;
}

type LoadState =
  | { status: "loading" }
  | { status: "error"; message: string }
  | { status: "ready"; games: PublishedBdtGameItem[] };

type StartState =
  | { status: "idle" }
  | { status: "starting"; partidaId: string }
  | { status: "started"; response: StartBdtGameResponse }
  | { status: "error"; partidaId: string; message: string };

export function PublishedBdtGamesPage({ accessToken }: PublishedBdtGamesPageProps) {
  const [state, setState] = useState<LoadState>({ status: "loading" });
  const [startState, setStartState] = useState<StartState>({ status: "idle" });

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

  async function handleStart(partidaId: string) {
    setStartState({ status: "starting", partidaId });

    try {
      const response = await startBdtGame(partidaId, accessToken);
      setStartState({ status: "started", response });
    } catch (caught) {
      if (caught instanceof BdtApiError) {
        setStartState({ status: "error", partidaId, message: mapStartErrorMessage(caught.statusCode, caught.message) });
      } else {
        setStartState({ status: "error", partidaId, message: "Error inesperado al iniciar la partida BDT." });
      }
    }
  }

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

        {startState.status === "started" ? (
          <div role="status" className="notice success">
            {startState.response.mensaje} Estado: {startState.response.estado}. Etapa activa {startState.response.etapaActiva.orden} cierra en {startState.response.etapaActiva.cierraEnUtc}.
          </div>
        ) : null}

        {startState.status === "error" ? (
          <div role="alert" className="notice error">
            {startState.message}
          </div>
        ) : null}

        {state.status === "ready" && state.games.length > 0 ? (
          <div className="table-wrap">
            <table aria-label="Partidas BDT publicadas para operador">
              <thead>
                <tr>
                  <th>Nombre</th>
                  <th>Estado</th>
                  <th>Modalidad</th>
                  <th>Area</th>
                  <th>Etapas</th>
                  <th>Accion</th>
                </tr>
              </thead>
              <tbody>
                {state.games.map((game) => (
                  <tr key={game.partidaId}>
                    <td>{game.nombre}</td>
                    <td><span className="badge">{game.estado}</span></td>
                    <td>{game.modalidad}</td>
                    <td>{game.areaBusqueda}</td>
                    <td>{game.cantidadEtapas}</td>
                    <td>
                      <button
                        type="button"
                        onClick={() => void handleStart(game.partidaId)}
                        disabled={startState.status === "starting"}
                      >
                        {startState.status === "starting" && startState.partidaId === game.partidaId
                          ? "Iniciando..."
                          : "Iniciar BDT"}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : null}
      </div>
    </div>
  );
}

function mapStartErrorMessage(statusCode: number, fallbackMessage: string): string {
  switch (statusCode) {
    case 401:
      return "Sesion expirada o no autenticada. Inicia sesion nuevamente.";
    case 403:
      return "No autorizado. Debes tener rol Operador para iniciar BDT.";
    case 404:
      return "La partida BDT no existe.";
    case 409:
      return fallbackMessage || "La partida BDT no cumple las reglas para iniciar.";
    case 500:
      return "Error de persistencia al iniciar BDT Game Service.";
    default:
      return fallbackMessage;
  }
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
