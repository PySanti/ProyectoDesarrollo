import { FormEvent, useState } from "react";
import {
  TriviaApiError,
  TriviaGameLobby,
  TriviaRankingEntry,
  TriviaTeamLobbyItem,
  createTriviaForm,
  getTriviaParticipants,
  getTriviaRanking,
  getTriviaTeams,
  startTriviaGame
} from "../../api/triviaApi";

interface TriviaOperationsPageProps {
  accessToken: string;
}

type FormState = {
  title: string;
  question: string;
  optionA: string;
  optionB: string;
  optionC: string;
  optionD: string;
  correctIndex: string;
  assignedScore: string;
  timeLimitSeconds: string;
};

const initialFormState: FormState = {
  title: "",
  question: "",
  optionA: "",
  optionB: "",
  optionC: "",
  optionD: "",
  correctIndex: "0",
  assignedScore: "100",
  timeLimitSeconds: "30"
};

export function TriviaOperationsPage({ accessToken }: TriviaOperationsPageProps) {
  const [form, setForm] = useState<FormState>(initialFormState);
  const [partidaId, setPartidaId] = useState("");
  const [loading, setLoading] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [participants, setParticipants] = useState<TriviaGameLobby | null>(null);
  const [teams, setTeams] = useState<TriviaTeamLobbyItem[] | null>(null);
  const [ranking, setRanking] = useState<TriviaRankingEntry[] | null>(null);

  async function handleCreateForm(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setMessage(null);

    const validationError = validateForm(form);
    if (validationError) {
      setError(validationError);
      return;
    }

    setLoading("form");
    try {
      const options = [form.optionA, form.optionB, form.optionC, form.optionD].map((text, index) => ({
        text: text.trim(),
        isCorrect: index === Number(form.correctIndex)
      }));
      const created = await createTriviaForm(
        {
          title: form.title.trim(),
          questions: [
            {
              text: form.question.trim(),
              assignedScore: Number(form.assignedScore),
              timeLimitSeconds: Number(form.timeLimitSeconds),
              displayOrder: 1,
              options
            }
          ]
        },
        accessToken
      );
      setMessage(`Formulario creado: ${created.title} (${created.questions.length} pregunta).`);
      setForm(initialFormState);
    } catch (caught) {
      setError(mapTriviaError(caught, "No se pudo crear el formulario de Trivia."));
    } finally {
      setLoading(null);
    }
  }

  async function handleLoadParticipants() {
    if (!partidaId.trim()) {
      setError("Indica el ID de la partida Trivia.");
      return;
    }

    setLoading("participants");
    setError(null);
    setMessage(null);
    try {
      const data = await getTriviaParticipants(partidaId.trim(), accessToken);
      setParticipants(data);
      setTeams(null);
      setRanking(null);
    } catch (caught) {
      setError(mapTriviaError(caught, "No se pudo consultar el lobby de Trivia."));
    } finally {
      setLoading(null);
    }
  }

  async function handleLoadTeams() {
    if (!partidaId.trim()) {
      setError("Indica el ID de la partida Trivia.");
      return;
    }

    setLoading("teams");
    setError(null);
    setMessage(null);
    try {
      const data = await getTriviaTeams(partidaId.trim(), accessToken);
      setTeams(data);
      setParticipants(null);
      setRanking(null);
    } catch (caught) {
      setError(mapTriviaError(caught, "No se pudieron consultar equipos en lobby."));
    } finally {
      setLoading(null);
    }
  }

  async function handleStart() {
    if (!partidaId.trim()) {
      setError("Indica el ID de la partida Trivia.");
      return;
    }

    setLoading("start");
    setError(null);
    setMessage(null);
    try {
      const started = await startTriviaGame(partidaId.trim(), accessToken);
      setMessage(`Partida iniciada: ${started.nombre}. Estado ${started.estado}.`);
    } catch (caught) {
      setError(mapTriviaError(caught, "No se pudo iniciar la partida Trivia."));
    } finally {
      setLoading(null);
    }
  }

  async function handleLoadRanking() {
    if (!partidaId.trim()) {
      setError("Indica el ID de la partida Trivia.");
      return;
    }

    setLoading("ranking");
    setError(null);
    setMessage(null);
    try {
      const data = await getTriviaRanking(partidaId.trim(), accessToken);
      setRanking(data);
      setParticipants(null);
      setTeams(null);
    } catch (caught) {
      setError(mapTriviaError(caught, "No se pudo consultar el ranking de Trivia."));
    } finally {
      setLoading(null);
    }
  }

  return (
    <div className="page">
      <div className="card stack">
        <div>
          <h1>Operacion Trivia</h1>
          <p>Flujos HU-15, HU-22, HU-23, HU-24 y HU-30 para operadores.</p>
        </div>

        {message ? <div className="notice success" role="status">{message}</div> : null}
        {error ? <div className="notice error" role="alert">{error}</div> : null}

        <form onSubmit={handleCreateForm} noValidate>
          <fieldset>
            <legend>HU-15 Crear formulario</legend>
            <label htmlFor="form-title">Titulo del formulario
              <input id="form-title" value={form.title} onChange={(event) => setForm((current) => ({ ...current, title: event.target.value }))} />
            </label>
            <label htmlFor="form-question">Pregunta
              <input id="form-question" value={form.question} onChange={(event) => setForm((current) => ({ ...current, question: event.target.value }))} />
            </label>
            <div className="row">
              <label htmlFor="option-a">Opcion A<input id="option-a" value={form.optionA} onChange={(event) => setForm((current) => ({ ...current, optionA: event.target.value }))} /></label>
              <label htmlFor="option-b">Opcion B<input id="option-b" value={form.optionB} onChange={(event) => setForm((current) => ({ ...current, optionB: event.target.value }))} /></label>
            </div>
            <div className="row">
              <label htmlFor="option-c">Opcion C<input id="option-c" value={form.optionC} onChange={(event) => setForm((current) => ({ ...current, optionC: event.target.value }))} /></label>
              <label htmlFor="option-d">Opcion D<input id="option-d" value={form.optionD} onChange={(event) => setForm((current) => ({ ...current, optionD: event.target.value }))} /></label>
            </div>
            <div className="row">
              <label htmlFor="correct-index">Respuesta correcta
                <select id="correct-index" value={form.correctIndex} onChange={(event) => setForm((current) => ({ ...current, correctIndex: event.target.value }))}>
                  <option value="0">A</option>
                  <option value="1">B</option>
                  <option value="2">C</option>
                  <option value="3">D</option>
                </select>
              </label>
              <label htmlFor="assigned-score">Puntaje<input id="assigned-score" type="number" min="1" value={form.assignedScore} onChange={(event) => setForm((current) => ({ ...current, assignedScore: event.target.value }))} /></label>
              <label htmlFor="time-limit">Tiempo limite<input id="time-limit" type="number" min="5" value={form.timeLimitSeconds} onChange={(event) => setForm((current) => ({ ...current, timeLimitSeconds: event.target.value }))} /></label>
            </div>
            <button type="submit" disabled={loading === "form"}>{loading === "form" ? "Creando..." : "Crear formulario"}</button>
          </fieldset>
        </form>

        <fieldset>
          <legend>HU-22/HU-23/HU-24/HU-30 Supervisar partida</legend>
          <label htmlFor="partida-id">ID de partida Trivia
            <input id="partida-id" value={partidaId} onChange={(event) => setPartidaId(event.target.value)} placeholder="uuid" />
          </label>
          <div className="actions">
            <button type="button" onClick={() => void handleLoadParticipants()} disabled={loading === "participants"}>Ver participantes</button>
            <button type="button" onClick={() => void handleLoadTeams()} disabled={loading === "teams"}>Ver equipos</button>
            <button type="button" onClick={() => void handleStart()} disabled={loading === "start"}>Iniciar Trivia</button>
            <button type="button" onClick={() => void handleLoadRanking()} disabled={loading === "ranking"}>Ver ranking</button>
          </div>
        </fieldset>

        {participants ? <ParticipantsTable data={participants} /> : null}
        {teams ? <TeamsTable teams={teams} /> : null}
        {ranking ? <RankingTable ranking={ranking} /> : null}
      </div>
    </div>
  );
}

function ParticipantsTable({ data }: { data: TriviaGameLobby }) {
  if (data.participantes.length === 0) {
    return (
      <div className="table-wrap">
        <h2>Participantes en lobby</h2>
        <p className="muted">{data.nombre} - sin participantes inscritos.</p>
      </div>
    );
  }

  return (
    <div className="table-wrap">
      <h2>Participantes en lobby</h2>
      <p className="muted">{data.nombre} - {data.participantesActual} participantes</p>
      <table aria-label="Participantes unidos a Trivia publicada">
        <thead><tr><th>Usuario</th><th>Inscripcion</th><th>Fecha</th></tr></thead>
        <tbody>{data.participantes.map((item) => <tr key={item.inscripcionId}><td>{item.usuarioId}</td><td>{item.inscripcionId}</td><td>{item.fechaInscripcion}</td></tr>)}</tbody>
      </table>
    </div>
  );
}

function TeamsTable({ teams }: { teams: TriviaTeamLobbyItem[] }) {
  if (teams.length === 0) {
    return (
      <div className="table-wrap">
        <h2>Equipos en lobby</h2>
        <p className="muted">No hay equipos inscritos en esta Trivia.</p>
      </div>
    );
  }

  return (
    <div className="table-wrap">
      <h2>Equipos en lobby</h2>
      <table aria-label="Equipos unidos a Trivia publicada">
        <thead><tr><th>Equipo</th><th>Fecha inscripcion</th></tr></thead>
        <tbody>{teams.map((item) => <tr key={item.equipoId}><td>{item.equipoId}</td><td>{item.fechaInscripcion}</td></tr>)}</tbody>
      </table>
    </div>
  );
}

function RankingTable({ ranking }: { ranking: TriviaRankingEntry[] }) {
  if (ranking.length === 0) {
    return (
      <div className="table-wrap">
        <h2>Ranking Trivia</h2>
        <p className="muted">Todavia no hay posiciones de ranking para esta partida.</p>
      </div>
    );
  }

  return (
    <div className="table-wrap">
      <h2>Ranking Trivia</h2>
      <table aria-label="Ranking durante Trivia">
        <thead><tr><th>Posicion</th><th>Usuario</th><th>Puntaje</th><th>Tiempo acumulado</th><th>Correctas</th></tr></thead>
        <tbody>{ranking.map((item) => <tr key={`${item.posicion}-${item.usuarioId}`}><td>{item.posicion}</td><td>{item.usuarioId}</td><td>{item.puntajeAcumulado}</td><td>{item.tiempoAcumuladoSegundos}s</td><td>{item.respuestasCorrectas}/{item.totalRespuestas}</td></tr>)}</tbody>
      </table>
    </div>
  );
}

function validateForm(form: FormState): string | null {
  if (!form.title.trim() || !form.question.trim()) return "Titulo y pregunta son obligatorios.";
  if ([form.optionA, form.optionB, form.optionC, form.optionD].some((option) => !option.trim())) return "Las cuatro opciones son obligatorias.";
  if (Number(form.assignedScore) <= 0) return "El puntaje debe ser mayor que cero.";
  if (Number(form.timeLimitSeconds) < 5) return "El tiempo limite debe ser de al menos 5 segundos.";
  return null;
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
