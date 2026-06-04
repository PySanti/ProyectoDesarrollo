import { FormEvent, useState } from "react";
import {
  createTriviaForm,
  CreateTriviaFormResponse,
  TriviaApiError
} from "../../api/triviaApi";

interface CreateTriviaFormPageProps {
  accessToken: string;
}

interface OptionFormState {
  text: string;
  isCorrect: boolean;
}

interface QuestionFormState {
  text: string;
  assignedScore: string;
  timeLimitSeconds: string;
  options: OptionFormState[];
}

interface FormState {
  title: string;
  questions: QuestionFormState[];
}

const emptyOption: OptionFormState = { text: "", isCorrect: false };
const emptyQuestion: QuestionFormState = {
  text: "",
  assignedScore: "100",
  timeLimitSeconds: "30",
  options: [
    { ...emptyOption },
    { ...emptyOption },
    { ...emptyOption },
    { ...emptyOption }
  ]
};

const initialForm: FormState = {
  title: "",
  questions: [emptyQuestion]
};

export function CreateTriviaFormPage({ accessToken }: CreateTriviaFormPageProps) {
  const [form, setForm] = useState<FormState>(initialForm);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<CreateTriviaFormResponse | null>(null);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setResult(null);

    const validationError = validateForm(form);
    if (validationError) {
      setError(validationError);
      return;
    }

    setLoading(true);
    try {
      const created = await createTriviaForm(
        {
          title: form.title.trim(),
          questions: form.questions.map((question, qi) => ({
            text: question.text.trim(),
            assignedScore: Number(question.assignedScore),
            timeLimitSeconds: Number(question.timeLimitSeconds),
            displayOrder: qi + 1,
            options: question.options.map((option) => ({
              text: option.text.trim(),
              isCorrect: option.isCorrect
            }))
          }))
        },
        accessToken
      );
      setResult(created);
      setForm(initialForm);
    } catch (caught) {
      if (caught instanceof TriviaApiError) {
        setError(mapErrorMessage(caught.statusCode, caught.message));
      } else {
        setError("Error inesperado al crear el formulario.");
      }
    } finally {
      setLoading(false);
    }
  }

  function updateQuestion(
    index: number,
    changes: Partial<QuestionFormState>
  ) {
    setForm((current) => ({
      ...current,
      questions: current.questions.map((q, i) =>
        i === index ? { ...q, ...changes } : q
      )
    }));
  }

  function updateOption(
    questionIndex: number,
    optionIndex: number,
    changes: Partial<OptionFormState>
  ) {
    setForm((current) => ({
      ...current,
      questions: current.questions.map((q, qi) =>
        qi === questionIndex
          ? {
              ...q,
              options: q.options.map((o, oi) =>
                oi === optionIndex ? { ...o, ...changes } : o
              )
            }
          : q
      )
    }));
  }

  function removeQuestion(index: number) {
    setForm((current) => ({
      ...current,
      questions: current.questions.filter((_, i) => i !== index)
    }));
  }

  function addQuestion() {
    setForm((current) => ({
      ...current,
      questions: [...current.questions, { ...emptyQuestion, options: emptyQuestion.options.map(() => ({ ...emptyOption })) }]
    }));
  }

  return (
    <div className="page">
      <div className="card">
        <h1>Crear formulario de Trivia</h1>
        <p>Flujo HU-15 para operadores usando Trivia Game Service.</p>

        {error ? (
          <div role="alert" className="notice error">
            {error}
          </div>
        ) : null}

        {result ? (
          <div className="notice success" data-testid="trivia-form-create-success">
            Formulario creado: <strong>{result.title}</strong> con{" "}
            {result.questions.length} pregunta(s).
          </div>
        ) : null}

        <form onSubmit={onSubmit} noValidate>
          <label htmlFor="form-title">
            Titulo del formulario
            <input
              id="form-title"
              value={form.title}
              onChange={(event) => setForm((current) => ({ ...current, title: event.target.value }))}
            />
          </label>

          <div className="row">
            <h2>Preguntas</h2>
            <button type="button" onClick={addQuestion}>
              Agregar pregunta
            </button>
          </div>

          {form.questions.map((question, qi) => {
            const questionNumber = qi + 1;
            const textId = `q-text-${questionNumber}`;
            const scoreId = `q-score-${questionNumber}`;
            const timeId = `q-time-${questionNumber}`;

            return (
              <fieldset key={questionNumber}>
                <legend>Pregunta {questionNumber}</legend>

                <label htmlFor={textId}>
                  Texto de la pregunta
                  <input
                    id={textId}
                    value={question.text}
                    onChange={(event) => updateQuestion(qi, { text: event.target.value })}
                  />
                </label>

                <div className="row">
                  <label htmlFor={scoreId}>
                    Puntaje (1-1000)
                    <input
                      id={scoreId}
                      type="number"
                      min="1"
                      max="1000"
                      value={question.assignedScore}
                      onChange={(event) => updateQuestion(qi, { assignedScore: event.target.value })}
                    />
                  </label>

                  <label htmlFor={timeId}>
                    Tiempo limite (5-300 seg)
                    <input
                      id={timeId}
                      type="number"
                      min="5"
                      max="300"
                      value={question.timeLimitSeconds}
                      onChange={(event) => updateQuestion(qi, { timeLimitSeconds: event.target.value })}
                    />
                  </label>
                </div>

                <h3>Opciones</h3>
                {question.options.map((option, oi) => {
                  const optionId = `q${questionNumber}-opt-${oi + 1}`;
                  const correctId = `q${questionNumber}-opt-${oi + 1}-correct`;

                  return (
                    <div className="row" key={oi}>
                      <label htmlFor={optionId}>
                        Opcion {oi + 1}
                        <input
                          id={optionId}
                          value={option.text}
                          onChange={(event) => updateOption(qi, oi, { text: event.target.value })}
                        />
                      </label>

                      <label htmlFor={correctId}>
                        Correcta
                        <input
                          id={correctId}
                          type="checkbox"
                          checked={option.isCorrect}
                          onChange={(event) =>
                            updateOption(qi, oi, { isCorrect: event.target.checked })
                          }
                        />
                      </label>
                    </div>
                  );
                })}

                {form.questions.length > 1 ? (
                  <button type="button" onClick={() => removeQuestion(qi)}>
                    Eliminar pregunta {questionNumber}
                  </button>
                ) : null}
              </fieldset>
            );
          })}

          <button type="submit" disabled={loading}>
            {loading ? "Creando formulario..." : "Crear formulario"}
          </button>
        </form>
      </div>
    </div>
  );
}

function validateForm(form: FormState): string | null {
  if (!form.title.trim()) {
    return "El titulo del formulario es obligatorio.";
  }

  if (form.questions.length === 0) {
    return "Debe existir al menos una pregunta.";
  }

  for (const [qi, question] of form.questions.entries()) {
    const questionNumber = qi + 1;

    if (!question.text.trim()) {
      return `El texto de la pregunta ${questionNumber} es obligatorio.`;
    }

    const score = Number(question.assignedScore);
    if (score < 1 || score > 1000) {
      return `El puntaje de la pregunta ${questionNumber} debe estar entre 1 y 1000.`;
    }

    const timeLimit = Number(question.timeLimitSeconds);
    if (timeLimit < 5 || timeLimit > 300) {
      return `El tiempo limite de la pregunta ${questionNumber} debe estar entre 5 y 300 segundos.`;
    }

    if (question.options.length !== 4) {
      return `La pregunta ${questionNumber} debe tener exactamente 4 opciones.`;
    }

    const correctCount = question.options.filter((o) => o.isCorrect).length;
    if (correctCount !== 1) {
      return `La pregunta ${questionNumber} debe tener exactamente una opcion correcta (tiene ${correctCount}).`;
    }

    for (const [oi, option] of question.options.entries()) {
      if (!option.text.trim()) {
        return `El texto de la opcion ${oi + 1} de la pregunta ${questionNumber} es obligatorio.`;
      }
    }
  }

  return null;
}

function mapErrorMessage(statusCode: number, fallbackMessage: string): string {
  switch (statusCode) {
    case 400:
      return "Datos invalidos o regla de negocio violada. Verifica el formulario.";
    case 403:
      return "No autorizado. Debes tener rol Operador.";
    case 500:
      return "Error de persistencia en Trivia Game Service.";
    default:
      return fallbackMessage;
  }
}
