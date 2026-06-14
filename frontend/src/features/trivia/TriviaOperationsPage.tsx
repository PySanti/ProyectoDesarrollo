import { ReactNode, useEffect, useState } from "react";
import {
  TriviaApiError,
  TriviaGameListItem,
  TriviaGameLobby,
  TriviaRankingEntry,
  TriviaTeamLobbyItem,
  getOperatorTriviaGamesForSupervision,
  getTriviaParticipants,
  getTriviaRanking,
  getTriviaTeams,
  startTriviaGame
} from "../../api/triviaApi";
import { Activity, Compass, Play, RefreshCw, Trophy, Users } from "../../shell/icons";

interface TriviaOperationsPageProps {
  accessToken: string;
}

export function TriviaOperationsPage({ accessToken }: TriviaOperationsPageProps) {
  const [supervisableGames, setSupervisableGames] = useState<TriviaGameListItem[]>([]);
  const [selectedGameId, setSelectedGameId] = useState("");
  const [loading, setLoading] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [participants, setParticipants] = useState<TriviaGameLobby | null>(null);
  const [teams, setTeams] = useState<TriviaTeamLobbyItem[] | null>(null);
  const [ranking, setRanking] = useState<TriviaRankingEntry[] | null>(null);
  const selectedGame = supervisableGames.find((game) => game.id === selectedGameId) ?? null;

  useEffect(() => {
    void handleLoadSupervisableGames();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function handleLoadSupervisableGames(preferredSelectedGameId = selectedGameId) {
    setLoading("supervision-list");
    setError(null);
    try {
      const games = await getOperatorTriviaGamesForSupervision(accessToken);
      setSupervisableGames(games);

      if (preferredSelectedGameId && games.some((game) => game.id === preferredSelectedGameId)) {
        setSelectedGameId(preferredSelectedGameId);
      } else if (preferredSelectedGameId) {
        setSelectedGameId("");
        clearSupervisionDetail();
      }
    } catch (caught) {
      setError(mapTriviaError(caught, "No se pudieron cargar las partidas de Trivia."));
    } finally {
      setLoading(null);
    }
  }

  async function handleSelectGame(gameId: string) {
    if (gameId === selectedGameId) {
      return;
    }
    setSelectedGameId(gameId);
    clearSupervisionDetail();
    setMessage(null);

    if (!gameId) {
      return;
    }

    await handleLoadSelectedGameDetail(gameId);
  }

  async function handleLoadSelectedGameDetail(gameId = selectedGameId) {
    if (!gameId.trim()) {
      setError("Selecciona una partida Trivia para supervisar.");
      return;
    }

    setLoading("detail");
    setError(null);
    try {
      const [participantsData, teamsData, rankingData] = await Promise.all([
        getTriviaParticipants(gameId.trim(), accessToken),
        getTriviaTeams(gameId.trim(), accessToken),
        getTriviaRanking(gameId.trim(), accessToken)
      ]);

      setParticipants(participantsData);
      setTeams(teamsData);
      setRanking(rankingData);
    } catch (caught) {
      setError(mapTriviaError(caught, "No se pudo consultar el detalle de la partida Trivia."));
    } finally {
      setLoading(null);
    }
  }

  function clearSupervisionDetail() {
    setParticipants(null);
    setTeams(null);
    setRanking(null);
  }

  async function handleStart() {
    if (!selectedGameId.trim()) {
      setError("Selecciona una partida Trivia para iniciar.");
      return;
    }

    setLoading("start");
    setError(null);
    setMessage(null);
    try {
      const started = await startTriviaGame(selectedGameId.trim(), accessToken);
      setSupervisableGames((current) =>
        current.map((game) => (game.id === started.id ? { ...game, estado: started.estado } : game))
      );
      await handleLoadSelectedGameDetail(started.id);
      await handleLoadSupervisableGames(started.id);
      setMessage(`Partida iniciada: ${started.nombre}. Estado ${started.estado}.`);
    } catch (caught) {
      setError(mapTriviaError(caught, "No se pudo iniciar la partida Trivia."));
    } finally {
      setLoading(null);
    }
  }

  const isLoadingList = loading === "supervision-list";
  const isLoadingDetail = loading === "detail";
  const showSkeleton = isLoadingDetail && !participants && !teams && !ranking;

  return (
    <div className="page wide">
      <header className="ops-head">
        <h1>Operación Trivia</h1>
        <p className="muted">
          Supervisa los lobbies en vivo, inicia partidas y consulta participantes, equipos y ranking
          en tiempo real.
        </p>
      </header>

      {message ? (
        <div className="notice success" role="status">
          {message}
        </div>
      ) : null}
      {error ? (
        <div className="notice error" role="alert">
          {error}
        </div>
      ) : null}

      <div className="ops-grid">
        <aside className="ops-master" aria-label="Partidas supervisables">
          <div className="ops-master__head">
            <span className="ops-master__title">
              <Activity />
              Partidas
            </span>
            <button
              type="button"
              className="ops-icon-btn"
              onClick={() => void handleLoadSupervisableGames()}
              disabled={isLoadingList}
              aria-label="Actualizar partidas"
            >
              <RefreshCw className={isLoadingList ? "ops-spin" : undefined} />
            </button>
          </div>

          {supervisableGames.length === 0 ? (
            <p className="ops-master__empty">
              {isLoadingList
                ? "Cargando partidas…"
                : "No hay partidas Trivia en lobby o iniciadas para supervisar."}
            </p>
          ) : (
            <div className="ops-list">
              {supervisableGames.map((game) => {
                const state = stateLabel(game.estado);
                const isActive = game.id === selectedGameId;
                return (
                  <button
                    key={game.id}
                    type="button"
                    className={isActive ? "ops-row is-active" : "ops-row"}
                    aria-pressed={isActive}
                    onClick={() => void handleSelectGame(game.id)}
                  >
                    <span className="ops-row__top">
                      <span className="ops-row__name">{game.nombre}</span>
                      <span className={`pill ${state.cls}`}>
                        <span className="pill__dot" />
                        {state.label}
                      </span>
                    </span>
                    <span className="ops-row__meta">Modalidad {game.modalidad}</span>
                  </button>
                );
              })}
            </div>
          )}
        </aside>

        <section className="ops-detail" aria-label="Detalle de la partida">
          {!selectedGame ? (
            <div className="empty-panel">
              <Compass />
              <p className="muted">
                Selecciona una partida de la lista para ver su lobby, equipos y ranking.
              </p>
            </div>
          ) : (
            <>
              <SelectedGameCard
                game={selectedGame}
                onStart={() => void handleStart()}
                onRefreshDetail={() => void handleLoadSelectedGameDetail()}
                starting={loading === "start"}
                refreshing={isLoadingDetail}
              />

              {showSkeleton ? (
                <SkeletonPanels />
              ) : (
                <>
                  {participants ? <ParticipantsPanel data={participants} /> : null}
                  {teams ? <TeamsPanel teams={teams} /> : null}
                  {ranking ? <RankingPanel ranking={ranking} /> : null}
                </>
              )}
            </>
          )}
        </section>
      </div>
    </div>
  );
}

function SelectedGameCard({
  game,
  onStart,
  onRefreshDetail,
  starting,
  refreshing
}: {
  game: TriviaGameListItem;
  onStart: () => void;
  onRefreshDetail: () => void;
  starting: boolean;
  refreshing: boolean;
}) {
  const state = stateLabel(game.estado);
  const alreadyStarted = game.estado === "Iniciada";

  return (
    <div className="ops-detail__card">
      <div className="ops-detail__head">
        <div className="ops-detail__title">
          <span className={`pill ${state.cls}`}>
            <span className="pill__dot" />
            {state.label}
          </span>
          <h2>{game.nombre}</h2>
        </div>
        <div className="ops-detail__actions">
          <button type="button" className="secondary-button" onClick={onRefreshDetail} disabled={refreshing}>
            {refreshing ? "Actualizando…" : "Actualizar detalle"}
          </button>
          <button type="button" className="btn-icon" onClick={onStart} disabled={alreadyStarted || starting}>
            <Play />
            {starting ? "Iniciando…" : "Iniciar Trivia"}
          </button>
        </div>
      </div>

      <dl className="ops-stats">
        <div className="ops-stat">
          <dt>Modalidad</dt>
          <dd>{game.modalidad}</dd>
        </div>
        <div className="ops-stat">
          <dt>Inicio</dt>
          <dd className="is-mono">{formatDateTime(game.tiempoInicio)}</dd>
        </div>
        <div className="ops-stat">
          <dt>Mínimo participantes</dt>
          <dd>{game.minimoParticipantes}</dd>
        </div>
        <div className="ops-stat">
          <dt>Máximo jugadores</dt>
          <dd>{game.maximoJugadores ?? "No aplica"}</dd>
        </div>
        <div className="ops-stat">
          <dt>Máximo equipos</dt>
          <dd>{game.maximoEquipos ?? "No aplica"}</dd>
        </div>
      </dl>
    </div>
  );
}

function Panel({
  title,
  icon,
  count,
  children
}: {
  title: string;
  icon: JSX.Element;
  count?: number;
  children: ReactNode;
}) {
  return (
    <section className="ops-panel" aria-label={title}>
      <div className="ops-panel__head">
        {icon}
        <h3>{title}</h3>
        {typeof count === "number" ? <span className="badge ops-panel__count">{count}</span> : null}
      </div>
      <div className="ops-panel__body">{children}</div>
    </section>
  );
}

function ParticipantsPanel({ data }: { data: TriviaGameLobby }) {
  return (
    <Panel title="Participantes" icon={<Users />} count={data.participantesActual}>
      {data.participantes.length === 0 ? (
        <p className="ops-panel__empty">
          {data.nombre} aún no tiene participantes inscritos en el lobby.
        </p>
      ) : (
        <div className="table-wrap">
          <table aria-label="Participantes unidos a Trivia publicada">
            <thead>
              <tr>
                <th>Usuario</th>
                <th>Inscripción</th>
                <th>Fecha</th>
              </tr>
            </thead>
            <tbody>
              {data.participantes.map((item) => (
                <tr key={item.inscripcionId}>
                  <td>{item.usuarioId}</td>
                  <td>{item.inscripcionId}</td>
                  <td>{formatDateTime(item.fechaInscripcion)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </Panel>
  );
}

function TeamsPanel({ teams }: { teams: TriviaTeamLobbyItem[] }) {
  return (
    <Panel title="Equipos" icon={<Users />} count={teams.length}>
      {teams.length === 0 ? (
        <p className="ops-panel__empty">No hay equipos inscritos en esta Trivia.</p>
      ) : (
        <div className="table-wrap">
          <table aria-label="Equipos unidos a Trivia publicada">
            <thead>
              <tr>
                <th>Equipo</th>
                <th>Fecha inscripción</th>
              </tr>
            </thead>
            <tbody>
              {teams.map((item) => (
                <tr key={item.equipoId}>
                  <td>{item.equipoId}</td>
                  <td>{formatDateTime(item.fechaInscripcion)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </Panel>
  );
}

function RankingPanel({ ranking }: { ranking: TriviaRankingEntry[] }) {
  return (
    <Panel title="Ranking" icon={<Trophy />} count={ranking.length}>
      {ranking.length === 0 ? (
        <p className="ops-panel__empty">
          Todavia no hay posiciones de ranking para esta partida. Aparecerán cuando inicie y se
          respondan preguntas.
        </p>
      ) : (
        <div className="table-wrap">
          <table aria-label="Ranking durante Trivia">
            <thead>
              <tr>
                <th>Posición</th>
                <th>Usuario</th>
                <th>Puntaje</th>
                <th>Tiempo acumulado</th>
                <th>Correctas</th>
              </tr>
            </thead>
            <tbody>
              {ranking.map((item) => (
                <tr key={`${item.posicion}-${item.usuarioId}`}>
                  <td>{item.posicion}</td>
                  <td>{item.usuarioId}</td>
                  <td>{item.puntajeAcumulado}</td>
                  <td>{item.tiempoAcumuladoSegundos}s</td>
                  <td>
                    {item.respuestasCorrectas}/{item.totalRespuestas}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </Panel>
  );
}

function SkeletonPanels() {
  return (
    <div className="ops-panel" aria-busy="true" aria-label="Cargando detalle">
      <div className="ops-skel">
        <span style={{ width: "40%" }} />
        <span style={{ width: "90%" }} />
        <span style={{ width: "75%" }} />
        <span style={{ width: "85%" }} />
      </div>
    </div>
  );
}

function stateLabel(estado: string): { cls: string; label: string } {
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
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit"
  });
}

function mapTriviaError(caught: unknown, fallback: string): string {
  if (caught instanceof TriviaApiError) {
    if (caught.statusCode === 403) return "No autorizado. Debes tener rol Operador.";
    if (caught.statusCode === 404) return "Recurso Trivia no encontrado.";
    if (caught.statusCode === 409) return caught.message || "La partida no cumple las reglas del flujo.";
    return caught.message;
  }

  return fallback;
}
