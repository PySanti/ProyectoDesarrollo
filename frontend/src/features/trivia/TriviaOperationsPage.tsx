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
  questions: QuestionFormState[];
};

type QuestionFormState = {
  id: string;
  question: string;
  optionA: string;
  optionB: string;
  optionC: string;
  optionD: string;
  correctIndex: string;
  assignedScore: string;
  timeLimitSeconds: string;
};

let questionIdCounter = 0;

function createQuestionId(): string {
  questionIdCounter += 1;
  return `question-${questionIdCounter}`;
}

function createEmptyQuestion(): QuestionFormState {
  return {
    id: createQuestionId(),
    question: "",
    optionA: "",
    optionB: "",
    optionC: "",
    optionD: "",
    correctIndex: "0",
    assignedScore: "100",
    timeLimitSeconds: "30"
  };
}

function createInitialFormState(): FormState {
  return {
    title: "",
    questions: [createEmptyQuestion()]
  };
}

export function TriviaOperationsPage({ accessToken }: TriviaOperationsPageProps) {
  const [form, setForm] = useState<FormState>(createInitialFormState);
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
      const questions = form.questions.map((question, questionIndex) => {
        const options = [question.optionA, question.optionB, question.optionC, question.optionD].map((text, index) => ({
          text: text.trim(),
          isCorrect: index === Number(question.correctIndex)
        }));

        return {
          text: question.question.trim(),
          assignedScore: Number(question.assignedScore),
          timeLimitSeconds: Number(question.timeLimitSeconds),
          displayOrder: questionIndex + 1,
          options
        };
      });

      const created = await createTriviaForm(
        {
          title: form.title.trim(),
          questions
        },
        accessToken
      );
      const questionLabel = created.questions.length === 1 ? "pregunta" : "preguntas";
      setMessage(`Formulario creado: ${created.title} (${created.questions.length} ${questionLabel}).`);
      setForm(createInitialFormState());
    } catch (caught) {
      setError(mapTriviaError(caught, "No se pudo crear el formulario de Trivia."));
    } finally {
      setLoading(null);
    }
  }

  function updateQuestion(questionId: string, patch: Partial<QuestionFormState>) {
    setForm((current) => ({
      ...current,
      questions: current.questions.map((question) =>
        question.id === questionId ? { ...question, ...patch } : question
      )
    }));
  }

  function addQuestion() {
    setForm((current) => ({
      ...current,
      questions: [...current.questions, createEmptyQuestion()]
    }));
  }

  function removeQuestion(questionId: string) {
    setForm((current) => ({
      ...current,
      questions: current.questions.length === 1
        ? current.questions
        : current.questions.filter((question) => question.id !== questionId)
    }));
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
          <p>Crea formularios, supervisa lobbies, inicia partidas y consulta rankings de Trivia.</p>
        </div>

        {message ? <div className="notice success" role="status">{message}</div> : null}
        {error ? <div className="notice error" role="alert">{error}</div> : null}

        <form onSubmit={handleCreateForm} noValidate>
          <fieldset>
            <legend>Crear formulario</legend>
            <label htmlFor="form-title">Titulo del formulario
              <input id="form-title" value={form.title} onChange={(event) => setForm((current) => ({ ...current, title: event.target.value }))} />
            </label>
            <div className="question-list">
              {form.questions.map((question, index) => (
                <QuestionEditor
                  key={question.id}
                  index={index}
                  question={question}
                  canRemove={form.questions.length > 1}
                  onChange={(patch) => updateQuestion(question.id, patch)}
                  onRemove={() => removeQuestion(question.id)}
                />
              ))}
            </div>
            <button type="button" className="secondary-button" onClick={addQuestion}>
              Agregar pregunta
            </button>
            <button type="submit" disabled={loading === "form"}>{loading === "form" ? "Creando..." : "Crear formulario"}</button>
          </fieldset>
        </form>

        <fieldset>
          <legend>Supervisar partida</legend>
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

function QuestionEditor({
  index,
  question,
  canRemove,
  onChange,
  onRemove
}: {
  index: number;
  question: QuestionFormState;
  canRemove: boolean;
  onChange: (patch: Partial<QuestionFormState>) => void;
  onRemove: () => void;
}) {
  const number = index + 1;

  return (
    <section className="question-card" aria-label={`Pregunta ${number}`}>
      <div className="question-card-header">
        <h3>Pregunta {number}</h3>
        <button type="button" className="secondary-button" onClick={onRemove} disabled={!canRemove}>
          Eliminar pregunta
        </button>
      </div>
      <label htmlFor={`form-question-${number}`}>Texto de pregunta {number}
        <input id={`form-question-${number}`} value={question.question} onChange={(event) => onChange({ question: event.target.value })} />
      </label>
      <div className="row">
        <label htmlFor={`option-a-${number}`}>Opcion A pregunta {number}<input id={`option-a-${number}`} value={question.optionA} onChange={(event) => onChange({ optionA: event.target.value })} /></label>
        <label htmlFor={`option-b-${number}`}>Opcion B pregunta {number}<input id={`option-b-${number}`} value={question.optionB} onChange={(event) => onChange({ optionB: event.target.value })} /></label>
      </div>
      <div className="row">
        <label htmlFor={`option-c-${number}`}>Opcion C pregunta {number}<input id={`option-c-${number}`} value={question.optionC} onChange={(event) => onChange({ optionC: event.target.value })} /></label>
        <label htmlFor={`option-d-${number}`}>Opcion D pregunta {number}<input id={`option-d-${number}`} value={question.optionD} onChange={(event) => onChange({ optionD: event.target.value })} /></label>
      </div>
      <div className="row">
        <label htmlFor={`correct-index-${number}`}>Respuesta correcta pregunta {number}
          <select id={`correct-index-${number}`} value={question.correctIndex} onChange={(event) => onChange({ correctIndex: event.target.value })}>
            <option value="0">A</option>
            <option value="1">B</option>
            <option value="2">C</option>
            <option value="3">D</option>
          </select>
        </label>
        <label htmlFor={`assigned-score-${number}`}>Puntaje pregunta {number}<input id={`assigned-score-${number}`} type="number" min="1" value={question.assignedScore} onChange={(event) => onChange({ assignedScore: event.target.value })} /></label>
        <label htmlFor={`time-limit-${number}`}>Tiempo limite pregunta {number}<input id={`time-limit-${number}`} type="number" min="5" value={question.timeLimitSeconds} onChange={(event) => onChange({ timeLimitSeconds: event.target.value })} /></label>
      </div>
    </section>
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
  if (!form.title.trim()) return "El titulo es obligatorio.";
  if (form.questions.length === 0) return "Agrega al menos una pregunta.";

  for (const [index, question] of form.questions.entries()) {
    const questionNumber = index + 1;
    if (!question.question.trim()) return `La pregunta ${questionNumber} es obligatoria.`;
    if ([question.optionA, question.optionB, question.optionC, question.optionD].some((option) => !option.trim())) return `Las cuatro opciones de la pregunta ${questionNumber} son obligatorias.`;
    if (Number(question.assignedScore) <= 0) return `El puntaje de la pregunta ${questionNumber} debe ser mayor que cero.`;
    if (Number(question.timeLimitSeconds) < 5) return `El tiempo limite de la pregunta ${questionNumber} debe ser de al menos 5 segundos.`;
  }

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
