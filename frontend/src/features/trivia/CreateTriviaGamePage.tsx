import { FormEvent, useEffect, useState } from "react";
import {
  TriviaApiError,
  TriviaFormListItem,
  TriviaModalidad,
  TriviaModoInicio,
  TriviaGameDetail,
  createTriviaGame,
  getTriviaForms
} from "../../api/triviaApi";

interface CreateTriviaGamePageProps {
  accessToken: string;
}

interface FormState {
  nombre: string;
  formularioId: string;
  modalidad: TriviaModalidad;
  modoInicio: TriviaModoInicio;
  tiempoInicio: string;
  minimoParticipantes: string;
  maximoJugadores: string;
  maximoEquipos: string;
  minimoJugadoresPorEquipo: string;
  maximoJugadoresPorEquipo: string;
}

const tomorrowIsoLocal = (): string => {
  const now = new Date();
  now.setDate(now.getDate() + 1);
  const pad = (n: number) => n.toString().padStart(2, "0");
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}T${pad(now.getHours())}:${pad(now.getMinutes())}`;
};

const initialForm: FormState = {
  nombre: "",
  formularioId: "",
  modalidad: "Individual",
  modoInicio: "Manual",
  tiempoInicio: tomorrowIsoLocal(),
  minimoParticipantes: "1",
  maximoJugadores: "10",
  maximoEquipos: "",
  minimoJugadoresPorEquipo: "",
  maximoJugadoresPorEquipo: ""
};

export function CreateTriviaGamePage({ accessToken }: CreateTriviaGamePageProps) {
  const [form, setForm] = useState<FormState>(initialForm);
  const [forms, setForms] = useState<TriviaFormListItem[]>([]);
  const [formsLoading, setFormsLoading] = useState(true);
  const [formsError, setFormsError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<TriviaGameDetail | null>(null);

  useEffect(() => {
    let active = true;

    getTriviaForms(accessToken)
      .then((items) => {
        if (active) {
          setForms(items.filter((f) => f.isComplete));
          if (items.filter((f) => f.isComplete).length > 0) {
            setForm((current) => ({ ...current, formularioId: items.filter((f) => f.isComplete)[0].id }));
          }
        }
      })
      .catch(() => {
        if (active) {
          setFormsError("No se pudieron cargar los formularios.");
        }
      })
      .finally(() => {
        if (active) {
          setFormsLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, [accessToken]);

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
      const created = await createTriviaGame(
        {
          nombre: form.nombre.trim(),
          modalidad: form.modalidad,
          modoInicio: form.modoInicio,
          formularioId: form.formularioId,
          tiempoInicio: new Date(form.tiempoInicio).toISOString(),
          minimoParticipantes: Number(form.minimoParticipantes),
          maximoJugadores: form.modalidad === "Individual" ? Number(form.maximoJugadores) : null,
          maximoEquipos: form.modalidad === "Equipo" ? Number(form.maximoEquipos) : null,
          minimoJugadoresPorEquipo:
            form.modalidad === "Equipo" ? Number(form.minimoJugadoresPorEquipo) : null,
          maximoJugadoresPorEquipo:
            form.modalidad === "Equipo" && form.maximoJugadoresPorEquipo !== ""
              ? Number(form.maximoJugadoresPorEquipo)
              : null
        },
        accessToken
      );
      setResult(created);
      setForm(initialForm);
    } catch (caught) {
      if (caught instanceof TriviaApiError) {
        setError(mapErrorMessage(caught.statusCode, caught.message));
      } else {
        setError("Error inesperado al crear la partida de Trivia.");
      }
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="page">
      <div className="card">
        <h1>Crear partida de Trivia</h1>
        <p>Configura y publica una partida de Trivia usando un formulario valido.</p>

        {error ? (
          <div role="alert" className="notice error">
            {error}
          </div>
        ) : null}

        {result ? (
          <div className="notice success" data-testid="trivia-create-success">
            Partida creada: <strong>{result.nombre}</strong> en estado{" "}
            <strong>{result.estado}</strong>.
          </div>
        ) : null}

        <form onSubmit={onSubmit} noValidate>
          <label htmlFor="trivia-nombre">
            Nombre
            <input
              id="trivia-nombre"
              value={form.nombre}
              onChange={(event) => setForm((current) => ({ ...current, nombre: event.target.value }))}
            />
          </label>

          <label htmlFor="trivia-formulario">
            Formulario
            {formsLoading ? (
              <select id="trivia-formulario" disabled>
                <option>Cargando formularios...</option>
              </select>
            ) : formsError ? (
              <select id="trivia-formulario" disabled>
                <option>{formsError}</option>
              </select>
            ) : forms.length === 0 ? (
              <select id="trivia-formulario" disabled>
                <option>No hay formularios completos disponibles</option>
              </select>
            ) : (
              <select
                id="trivia-formulario"
                value={form.formularioId}
                onChange={(event) => setForm((current) => ({ ...current, formularioId: event.target.value }))}
              >
                {forms.map((f) => (
                  <option key={f.id} value={f.id}>
                    {f.title} ({f.questionsCount} preguntas)
                  </option>
                ))}
              </select>
            )}
          </label>

          <div className="row">
            <label htmlFor="trivia-modalidad">
              Modalidad
              <select
                id="trivia-modalidad"
                value={form.modalidad}
                onChange={(event) =>
                  setForm((current) => ({ ...current, modalidad: event.target.value as TriviaModalidad }))
                }
              >
                <option value="Individual">Individual</option>
                <option value="Equipo">Equipo</option>
              </select>
            </label>

            <label htmlFor="trivia-modo-inicio">
              Modo de inicio
              <select
                id="trivia-modo-inicio"
                value={form.modoInicio}
                onChange={(event) =>
                  setForm((current) => ({ ...current, modoInicio: event.target.value as TriviaModoInicio }))
                }
              >
                <option value="Manual">Manual</option>
                <option value="Automatico">Automatico</option>
                <option value="ManualYAutomatico">Manual y automatico</option>
              </select>
            </label>
          </div>

          <div className="row">
            <label htmlFor="trivia-minimo">
              Minimo participantes
              <input
                id="trivia-minimo"
                type="number"
                min="1"
                value={form.minimoParticipantes}
                onChange={(event) => setForm((current) => ({ ...current, minimoParticipantes: event.target.value }))}
              />
            </label>

            {form.modalidad === "Individual" ? (
              <label htmlFor="trivia-maximo-jugadores">
                Maximo jugadores
                <input
                  id="trivia-maximo-jugadores"
                  type="number"
                  min="1"
                  value={form.maximoJugadores}
                  onChange={(event) => setForm((current) => ({ ...current, maximoJugadores: event.target.value }))}
                />
              </label>
            ) : (
              <>
                <label htmlFor="trivia-maximo-equipos">
                  Maximo equipos
                  <input
                    id="trivia-maximo-equipos"
                    type="number"
                    min="1"
                    value={form.maximoEquipos}
                    onChange={(event) => setForm((current) => ({ ...current, maximoEquipos: event.target.value }))}
                  />
                </label>
              </>
            )}
          </div>

          {form.modalidad === "Equipo" ? (
            <div className="row">
              <label htmlFor="trivia-minimo-jugadores-equipo">
                Minimo jugadores por equipo
                <input
                  id="trivia-minimo-jugadores-equipo"
                  type="number"
                  min="1"
                  value={form.minimoJugadoresPorEquipo}
                  onChange={(event) =>
                    setForm((current) => ({ ...current, minimoJugadoresPorEquipo: event.target.value }))
                  }
                />
              </label>

              <label htmlFor="trivia-maximo-jugadores-equipo">
                Maximo jugadores por equipo
                <input
                  id="trivia-maximo-jugadores-equipo"
                  type="number"
                  min="1"
                  value={form.maximoJugadoresPorEquipo}
                  onChange={(event) =>
                    setForm((current) => ({ ...current, maximoJugadoresPorEquipo: event.target.value }))
                  }
                />
              </label>
            </div>
          ) : null}

          <label htmlFor="trivia-tiempo-inicio">
            Tiempo de inicio
            <input
              id="trivia-tiempo-inicio"
              type="datetime-local"
              value={form.tiempoInicio}
              onChange={(event) => setForm((current) => ({ ...current, tiempoInicio: event.target.value }))}
            />
          </label>

          <button type="submit" disabled={loading}>
            {loading ? "Creando Trivia..." : "Crear partida de Trivia"}
          </button>
        </form>
      </div>
    </div>
  );
}

function validateForm(form: FormState): string | null {
  if (!form.nombre.trim()) {
    return "El nombre de la partida es obligatorio.";
  }

  if (!form.formularioId) {
    return "Debes seleccionar un formulario.";
  }

  if (!form.tiempoInicio) {
    return "El tiempo de inicio es obligatorio.";
  }

  const tiempoInicioMs = new Date(form.tiempoInicio).getTime();
  if (Number.isNaN(tiempoInicioMs)) {
    return "El tiempo de inicio no es una fecha valida.";
  }

  if (tiempoInicioMs <= Date.now()) {
    return "El tiempo de inicio debe ser una fecha futura.";
  }

  if (Number(form.minimoParticipantes) <= 0) {
    return "El minimo de participantes debe ser mayor que cero.";
  }

  if (form.modalidad === "Individual" && Number(form.maximoJugadores) <= 0) {
    return "El maximo de jugadores debe ser mayor que cero.";
  }

  if (form.modalidad === "Equipo") {
    if (Number(form.maximoEquipos) <= 0) {
      return "El maximo de equipos debe ser mayor que cero.";
    }

    if (Number(form.minimoJugadoresPorEquipo) <= 0) {
      return "El minimo de jugadores por equipo debe ser mayor que cero.";
    }
  }

  return null;
}

function mapErrorMessage(statusCode: number, fallbackMessage: string): string {
  switch (statusCode) {
    case 400:
      return "Solicitud invalida. Verifica la configuracion de la partida.";
    case 403:
      return "No autorizado. Debes tener rol Operador.";
    case 404:
      return "El formulario seleccionado no existe.";
    case 409:
      return "La configuracion de modalidad y limites no es valida.";
    case 500:
      return "Error de persistencia en Trivia Game Service.";
    default:
      return fallbackMessage;
  }
}
