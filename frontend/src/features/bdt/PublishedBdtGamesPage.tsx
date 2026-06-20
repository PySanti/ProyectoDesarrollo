import { useEffect, useState } from "react";
import {
  BdtApiError,
  getOperatorPublishedBdtGames,
  PublishedBdtGameItem,
  StartBdtGameResponse,
  startBdtGame
} from "../../api/bdtApi";
import { Flag, Play } from "../../shell/icons";

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
  const [selectedGame, setSelectedGame] = useState<PublishedBdtGameItem | null>(null);

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

  const games = state.status === "ready" ? state.games : [];

  return (
    <div className="page wide">
      <div className="card stack">
        <header className="create-head">
          <div>
            <h1>Partidas BDT publicadas</h1>
            <p className="muted">
              Consulta las partidas publicadas e inicia su operación cuando cumplan los mínimos.
            </p>
          </div>
          {state.status === "ready" && games.length > 0 ? (
            <span className="badge">{games.length} publicadas</span>
          ) : null}
        </header>

        {state.status === "loading" ? (
          <div className="notice info">Cargando partidas BDT publicadas...</div>
        ) : null}

        {state.status === "error" ? (
          <div role="alert" className="notice error">
            {state.message}
          </div>
        ) : null}

        {startState.status === "started" ? (
          <div role="status" className="notice success">
            {startState.response.mensaje} Estado: {startState.response.estado}. Etapa activa{" "}
            {startState.response.etapaActiva.orden} cierra {formatDateTime(startState.response.etapaActiva.cierraEnUtc)}.
          </div>
        ) : null}

        {startState.status === "error" ? (
          <div role="alert" className="notice error">
            {startState.message}
          </div>
        ) : null}

        {state.status === "ready" && games.length === 0 ? (
          <div className="empty-panel" data-testid="bdt-published-empty">
            <Flag />
            <p>No hay partidas BDT publicadas.</p>
            <p className="muted">
              Crea y publica una búsqueda del tesoro en <strong>Crear BDT</strong> para verla aquí.
            </p>
          </div>
        ) : null}

        {state.status === "ready" && games.length > 0 ? (
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
                {games.map((game) => {
                  const pill = estadoPill(game.estado);
                  const isStarting =
                    startState.status === "starting" && startState.partidaId === game.partidaId;
                  return (
                    <tr key={game.partidaId}>
                      <td>{game.nombre}</td>
                      <td>
                        <span className={`pill ${pill.cls}`}>
                          <span className="pill__dot" />
                          {pill.label}
                        </span>
                      </td>
                      <td>{game.modalidad}</td>
                      <td>{game.areaBusqueda}</td>
                      <td>{game.cantidadEtapas}</td>
                      <td>
                        <div className="actions compact-actions">
                          <button type="button" className="secondary-button" onClick={() => setSelectedGame(game)}>
                            Ver resumen
                          </button>
                          <button
                            type="button"
                            className="btn-icon"
                            onClick={() => void handleStart(game.partidaId)}
                            disabled={startState.status === "starting"}
                          >
                            <Play />
                            {isStarting ? "Iniciando..." : "Iniciar BDT"}
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        ) : null}

        {selectedGame ? (
          <GameSummaryModal game={selectedGame} onClose={() => setSelectedGame(null)} />
        ) : null}
      </div>
    </div>
  );
}

function GameSummaryModal({ game, onClose }: { game: PublishedBdtGameItem; onClose: () => void }) {
  const pill = estadoPill(game.estado);

  return (
    <div className="modal-backdrop" role="presentation">
      <section className="modal-card" role="dialog" aria-modal="true" aria-labelledby="bdt-summary-title">
        <div className="modal-header">
          <div>
            <span className="badge">Resumen operativo</span>
            <h2 id="bdt-summary-title">{game.nombre}</h2>
          </div>
          <button type="button" className="secondary-button" onClick={onClose}>
            Cerrar
          </button>
        </div>

        <dl className="detail-grid">
          <div>
            <dt>Estado</dt>
            <dd>
              <span className={`pill ${pill.cls}`}>
                <span className="pill__dot" />
                {pill.label}
              </span>
            </dd>
          </div>
          <div>
            <dt>Modalidad</dt>
            <dd>{game.modalidad}</dd>
          </div>
          <div>
            <dt>Etapas configuradas</dt>
            <dd>{game.cantidadEtapas}</dd>
          </div>
          <div>
            <dt>Identificador</dt>
            <dd>
              <span className="mono">{game.partidaId}</span>
            </dd>
          </div>
          <div className="detail-wide">
            <dt>Area de busqueda</dt>
            <dd>{game.areaBusqueda}</dd>
          </div>
        </dl>
      </section>
    </div>
  );
}

function estadoPill(estado: string): { cls: string; label: string } {
  if (estado === "Iniciada") {
    return { cls: "pill--live", label: "Iniciada" };
  }
  if (estado === "Lobby") {
    return { cls: "pill--lobby", label: "Lobby" };
  }
  return { cls: "pill--done", label: estado };
}

function formatDateTime(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return iso;
  }
  return date.toLocaleString("es", {
    day: "2-digit",
    month: "short",
    hour: "2-digit",
    minute: "2-digit"
  });
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
