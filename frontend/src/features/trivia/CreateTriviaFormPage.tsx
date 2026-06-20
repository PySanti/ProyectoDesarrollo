import { FormEvent, useState } from "react";
import { TriviaApiError, createTriviaForm } from "../../api/triviaApi";
import { Plus, X } from "../../shell/icons";

interface CreateTriviaFormPageProps {
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

export function CreateTriviaFormPage({ accessToken }: CreateTriviaFormPageProps) {
  const [form, setForm] = useState<FormState>(createInitialFormState);
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function handleCreateForm(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setMessage(null);

    const validationError = validateForm(form);
    if (validationError) {
      setError(validationError);
      return;
    }

    setLoading(true);
    try {
      const questions = form.questions.map((question, questionIndex) => {
        const options = [question.optionA, question.optionB, question.optionC, question.optionD].map(
          (text, index) => ({
            text: text.trim(),
            isCorrect: index === Number(question.correctIndex)
          })
        );

        return {
          text: question.question.trim(),
          assignedScore: Number(question.assignedScore),
          timeLimitSeconds: Number(question.timeLimitSeconds),
          displayOrder: questionIndex + 1,
          options
        };
      });

      const created = await createTriviaForm({ title: form.title.trim(), questions }, accessToken);
      const questionLabel = created.questions.length === 1 ? "pregunta" : "preguntas";
      setMessage(`Formulario creado: ${created.title} (${created.questions.length} ${questionLabel}).`);
      setForm(createInitialFormState());
    } catch (caught) {
      setError(mapTriviaError(caught, "No se pudo crear el formulario de Trivia."));
    } finally {
      setLoading(false);
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
      questions:
        current.questions.length === 1
          ? current.questions
          : current.questions.filter((question) => question.id !== questionId)
    }));
  }

  const questionCount = form.questions.length;

  return (
    <div className="page">
      <div className="card stack">
        <header className="create-head">
          <div>
            <h1>Crear formulario de Trivia</h1>
            <p className="muted">
              Arma el banco de preguntas. Un formulario completo se usa luego en{" "}
              <strong>Crear Trivia</strong> para publicar una partida.
            </p>
          </div>
          <span className="badge">
            {questionCount} {questionCount === 1 ? "pregunta" : "preguntas"}
          </span>
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

        <form onSubmit={handleCreateForm} noValidate>
          <label htmlFor="form-title">
            Titulo del formulario
            <input
              id="form-title"
              value={form.title}
              onChange={(event) =>
                setForm((current) => ({ ...current, title: event.target.value }))
              }
            />
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

          <div className="create-actions">
            <button type="button" className="secondary-button btn-icon" onClick={addQuestion}>
              <Plus />
              Agregar pregunta
            </button>
            <button type="submit" disabled={loading}>
              {loading ? "Creando..." : "Crear formulario"}
            </button>
          </div>
        </form>
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
        <h3 className="q-title">
          <span className="q-badge" aria-hidden="true">
            {number}
          </span>
          Pregunta {number}
        </h3>
        <button
          type="button"
          className="secondary-button btn-icon"
          onClick={onRemove}
          disabled={!canRemove}
          aria-label={`Eliminar pregunta ${number}`}
        >
          <X />
          Eliminar
        </button>
      </div>
      <label htmlFor={`form-question-${number}`}>
        Texto de pregunta {number}
        <input
          id={`form-question-${number}`}
          value={question.question}
          onChange={(event) => onChange({ question: event.target.value })}
        />
      </label>
      <div className="row">
        <label htmlFor={`option-a-${number}`}>
          Opcion A pregunta {number}
          <input
            id={`option-a-${number}`}
            value={question.optionA}
            onChange={(event) => onChange({ optionA: event.target.value })}
          />
        </label>
        <label htmlFor={`option-b-${number}`}>
          Opcion B pregunta {number}
          <input
            id={`option-b-${number}`}
            value={question.optionB}
            onChange={(event) => onChange({ optionB: event.target.value })}
          />
        </label>
      </div>
      <div className="row">
        <label htmlFor={`option-c-${number}`}>
          Opcion C pregunta {number}
          <input
            id={`option-c-${number}`}
            value={question.optionC}
            onChange={(event) => onChange({ optionC: event.target.value })}
          />
        </label>
        <label htmlFor={`option-d-${number}`}>
          Opcion D pregunta {number}
          <input
            id={`option-d-${number}`}
            value={question.optionD}
            onChange={(event) => onChange({ optionD: event.target.value })}
          />
        </label>
      </div>
      <div className="q-meta">
        <label htmlFor={`correct-index-${number}`}>
          Respuesta correcta pregunta {number}
          <select
            id={`correct-index-${number}`}
            value={question.correctIndex}
            onChange={(event) => onChange({ correctIndex: event.target.value })}
          >
            <option value="0">A</option>
            <option value="1">B</option>
            <option value="2">C</option>
            <option value="3">D</option>
          </select>
        </label>
        <label htmlFor={`assigned-score-${number}`}>
          Puntaje pregunta {number}
          <input
            id={`assigned-score-${number}`}
            type="number"
            min="1"
            value={question.assignedScore}
            onChange={(event) => onChange({ assignedScore: event.target.value })}
          />
        </label>
        <label htmlFor={`time-limit-${number}`}>
          Tiempo limite pregunta {number}
          <input
            id={`time-limit-${number}`}
            type="number"
            min="5"
            value={question.timeLimitSeconds}
            onChange={(event) => onChange({ timeLimitSeconds: event.target.value })}
          />
        </label>
      </div>
    </section>
  );
}

function validateForm(form: FormState): string | null {
  if (!form.title.trim()) return "El titulo es obligatorio.";
  if (form.questions.length === 0) return "Agrega al menos una pregunta.";

  for (const [index, question] of form.questions.entries()) {
    const questionNumber = index + 1;
    if (!question.question.trim()) return `La pregunta ${questionNumber} es obligatoria.`;
    if ([question.optionA, question.optionB, question.optionC, question.optionD].some((option) => !option.trim()))
      return `Las cuatro opciones de la pregunta ${questionNumber} son obligatorias.`;
    if (Number(question.assignedScore) <= 0)
      return `El puntaje de la pregunta ${questionNumber} debe ser mayor que cero.`;
    if (Number(question.timeLimitSeconds) < 5)
      return `El tiempo limite de la pregunta ${questionNumber} debe ser de al menos 5 segundos.`;
  }

  return null;
}

function mapTriviaError(caught: unknown, fallback: string): string {
  if (caught instanceof TriviaApiError) {
    if (caught.statusCode === 403) return "No autorizado. Debes tener rol Operador.";
    if (caught.statusCode === 404) return "Recurso Trivia no encontrado.";
    if (caught.statusCode === 409) return caught.message || "El formulario no cumple las reglas del flujo.";
    return caught.message;
  }

  return fallback;
}
