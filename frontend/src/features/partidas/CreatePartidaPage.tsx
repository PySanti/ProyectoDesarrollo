// Wizard de 3 pasos para crear una partida multi-juego (Trivia / BDT).
// Reducer fino: acciones gruesas sobre el draft (Task 2) + estado de envio (Task 3).
// Nunca se llama a enviarPartida sin pasar antes por validateDraft (ver nota en enviarPartida.ts).
import { useReducer, useState } from "react";
import { useNavigate } from "react-router-dom";
import type { Modalidad, ModoInicioPartida } from "../../api/partidasApi";
import { Plus, X } from "../../shell/icons";
import {
  initialDraft,
  newEtapa,
  newJuegoBdt,
  newJuegoTrivia,
  newPregunta,
  validateDraft,
  validateHeader,
  validateJuego,
  type CreatePartidaDraft,
  type EtapaDraft,
  type HeaderDraft,
  type JuegoBdtDraft,
  type JuegoDraft,
  type JuegoTriviaDraft,
  type PreguntaDraft
} from "./createPartidaDraft";
import { enviarPartida, type EnvioJuego, type EstadoEnvio, type ResultadoEnvio } from "./enviarPartida";
import { generarCodigoTesoro, nombreArchivoQr, renderizarQrDataUrl } from "./qrTesoro";

interface EnvioState {
  partidaId: string | null;
  estados: EnvioJuego[];
  errorHeader?: string;
}

type WizardState = CreatePartidaDraft & {
  envio: EnvioState | null;
  enviando: boolean;
};

type Action =
  | { type: "patchHeader"; patch: Partial<HeaderDraft> }
  | { type: "setJuegos"; juegos: JuegoDraft[] }
  | { type: "setStep"; step: 1 | 2 | 3 }
  | { type: "envioProgreso"; estados: EnvioJuego[]; partidaId: string | null }
  | { type: "envioResultado"; resultado: ResultadoEnvio }
  | { type: "enviando" };

function initState(): WizardState {
  return { ...initialDraft(), envio: null, enviando: false };
}

function reducer(state: WizardState, action: Action): WizardState {
  switch (action.type) {
    case "patchHeader":
      // Editar el header tras un envio parcial invalida el progreso: reusar
      // partidaId sin re-postear el header dejaria el envio desincronizado.
      return { ...state, header: { ...state.header, ...action.patch }, envio: null };
    case "setJuegos":
      // Reordenar/agregar/quitar juegos tras un envio parcial desincroniza
      // envio.estados (mapeado por indice) de la identidad real de cada juego.
      return { ...state, juegos: action.juegos, envio: null };
    case "setStep":
      return { ...state, step: action.step };
    case "enviando":
      return { ...state, enviando: true };
    case "envioProgreso":
      return {
        ...state,
        envio: { partidaId: action.partidaId, estados: action.estados, errorHeader: undefined }
      };
    case "envioResultado":
      return {
        ...state,
        enviando: false,
        envio: {
          partidaId: action.resultado.partidaId,
          estados: action.resultado.estados,
          errorHeader: action.resultado.errorHeader
        }
      };
    default:
      return state;
  }
}

export function CreatePartidaPage({ accessToken }: { accessToken: string }) {
  const navigate = useNavigate();
  const [state, dispatch] = useReducer(reducer, undefined, initState);
  const [headerErrors, setHeaderErrors] = useState<string[]>([]);
  const [juegoErrors, setJuegoErrors] = useState<string[]>([]);

  function onSiguienteHeader() {
    const errors = validateHeader(state.header);
    setHeaderErrors(errors);
    if (errors.length === 0) {
      dispatch({ type: "setStep", step: 2 });
    }
  }

  function onSiguienteJuegos() {
    const errors =
      state.juegos.length === 0
        ? ["La partida debe tener al menos un juego."]
        : state.juegos.flatMap((juego) => validateJuego(juego));
    setJuegoErrors(errors);
    if (errors.length === 0) {
      dispatch({ type: "setStep", step: 3 });
    }
  }

  function onAtras() {
    dispatch({ type: "setStep", step: Math.max(1, state.step - 1) as 1 | 2 | 3 });
  }

  async function submit(previo: { partidaId: string | null; estados: EnvioJuego[] } | null) {
    // Guardia del T3: jamas enviar un draft sin pasar por validateDraft.
    const errors = validateDraft(state);
    if (errors.length > 0) {
      setJuegoErrors(errors);
      dispatch({ type: "setStep", step: 2 });
      return;
    }

    dispatch({ type: "enviando" });
    const resultado = await enviarPartida(state, accessToken, previo, (estados, partidaId) =>
      dispatch({ type: "envioProgreso", estados, partidaId })
    );
    dispatch({ type: "envioResultado", resultado });

    if (resultado.completo && resultado.partidaId) {
      navigate(`/partidas/${resultado.partidaId}`);
    }
  }

  return (
    <div className="page">
      <div className="card stack">
        <header className="create-head">
          <div>
            <h1>Crear partida</h1>
            <p className="muted">
              Configura los datos generales, agrega uno o más juegos (Trivia o Búsqueda del Tesoro) y
              confirma la creación.
            </p>
          </div>
          <span className="badge">Paso {state.step} de 3</span>
        </header>

        {state.step === 1 ? (
          <PasoHeader
            header={state.header}
            errors={headerErrors}
            onPatch={(patch) => dispatch({ type: "patchHeader", patch })}
            onSiguiente={onSiguienteHeader}
          />
        ) : null}

        {state.step === 2 ? (
          <PasoJuegos
            juegos={state.juegos}
            errors={juegoErrors}
            onAdd={(tipo) =>
              dispatch({
                type: "setJuegos",
                juegos: [...state.juegos, tipo === "Trivia" ? newJuegoTrivia() : newJuegoBdt()]
              })
            }
            onRemove={(index) =>
              dispatch({ type: "setJuegos", juegos: state.juegos.filter((_, i) => i !== index) })
            }
            onMove={(index, delta) => {
              const target = index + delta;
              if (target < 0 || target >= state.juegos.length) return;
              const next = [...state.juegos];
              [next[index], next[target]] = [next[target], next[index]];
              dispatch({ type: "setJuegos", juegos: next });
            }}
            onUpdateJuego={(index, next) =>
              dispatch({
                type: "setJuegos",
                juegos: state.juegos.map((juego, i) => (i === index ? next : juego))
              })
            }
            onSiguiente={onSiguienteJuegos}
            onAtras={onAtras}
          />
        ) : null}

        {state.step === 3 ? (
          <PasoResumen
            draft={state}
            envio={state.envio}
            enviando={state.enviando}
            onCrear={() => void submit(null)}
            onReintentar={() =>
              void submit(
                state.envio ? { partidaId: state.envio.partidaId, estados: state.envio.estados } : null
              )
            }
            onAtras={onAtras}
          />
        ) : null}
      </div>
    </div>
  );
}

function PasoHeader({
  header,
  errors,
  onPatch,
  onSiguiente
}: {
  header: HeaderDraft;
  errors: string[];
  onPatch: (patch: Partial<HeaderDraft>) => void;
  onSiguiente: () => void;
}) {
  return (
    <section className="form-section" data-testid="paso-1">
      <div className="form-section__head">
        <h2 className="form-section__title">Datos de la partida</h2>
      </div>

      <ErrorList errors={errors} />

      <label htmlFor="nombrePartida">
        Nombre de la partida
        <input
          id="nombrePartida"
          value={header.nombrePartida}
          onChange={(event) => onPatch({ nombrePartida: event.target.value })}
        />
      </label>

      <div className="row">
        <label htmlFor="modalidad">
          Modalidad
          <select
            id="modalidad"
            value={header.modalidad}
            onChange={(event) => onPatch({ modalidad: event.target.value as Modalidad })}
          >
            <option value="Individual">Individual</option>
            <option value="Equipo">Equipo</option>
          </select>
        </label>

        <label htmlFor="modoInicioPartida">
          Modo de inicio
          <select
            id="modoInicioPartida"
            value={header.modoInicioPartida}
            onChange={(event) => onPatch({ modoInicioPartida: event.target.value as ModoInicioPartida })}
          >
            <option value="Manual">Manual</option>
            <option value="Automatico">Automatico</option>
            <option value="ManualYAutomatico">Manual y automatico</option>
          </select>
        </label>
      </div>

      {header.modoInicioPartida !== "Manual" ? (
        <label htmlFor="tiempoInicio">
          Tiempo de inicio
          <input
            id="tiempoInicio"
            type="datetime-local"
            value={header.tiempoInicio}
            onChange={(event) => onPatch({ tiempoInicio: event.target.value })}
          />
        </label>
      ) : null}

      <div className="row">
        <label htmlFor="minimosParticipacion">
          Minimo de participacion
          <input
            id="minimosParticipacion"
            type="number"
            min="1"
            value={header.minimosParticipacion}
            onChange={(event) => onPatch({ minimosParticipacion: event.target.value })}
          />
        </label>
        <label htmlFor="maximosParticipacion">
          Maximo de participacion
          <input
            id="maximosParticipacion"
            type="number"
            min="1"
            value={header.maximosParticipacion}
            onChange={(event) => onPatch({ maximosParticipacion: event.target.value })}
          />
        </label>
      </div>

      <div className="create-actions">
        <span />
        <button type="button" data-testid="btn-siguiente" onClick={onSiguiente}>
          Siguiente
        </button>
      </div>
    </section>
  );
}

function PasoJuegos({
  juegos,
  errors,
  onAdd,
  onRemove,
  onMove,
  onUpdateJuego,
  onSiguiente,
  onAtras
}: {
  juegos: JuegoDraft[];
  errors: string[];
  onAdd: (tipo: "Trivia" | "BusquedaDelTesoro") => void;
  onRemove: (index: number) => void;
  onMove: (index: number, delta: -1 | 1) => void;
  onUpdateJuego: (index: number, next: JuegoDraft) => void;
  onSiguiente: () => void;
  onAtras: () => void;
}) {
  return (
    <section className="form-section" data-testid="paso-2">
      <div className="form-section__head">
        <h2 className="form-section__title">
          Juegos <span className="badge">{juegos.length}</span>
        </h2>
        <p className="form-section__hint">
          Agrega uno o más juegos. El orden de la lista es el orden de juego dentro de la partida.
        </p>
      </div>

      <ErrorList errors={errors} />

      <div className="create-actions">
        <div className="actions">
          <button
            type="button"
            className="secondary-button btn-icon"
            data-testid="btn-agregar-trivia"
            onClick={() => onAdd("Trivia")}
          >
            <Plus />
            Agregar Trivia
          </button>
          <button
            type="button"
            className="secondary-button btn-icon"
            data-testid="btn-agregar-bdt"
            onClick={() => onAdd("BusquedaDelTesoro")}
          >
            <Plus />
            Agregar BDT
          </button>
        </div>
      </div>

      <div className="question-list">
        {juegos.map((juego, index) => (
          <JuegoCard
            key={juego.localId}
            juego={juego}
            index={index}
            total={juegos.length}
            onRemove={() => onRemove(index)}
            onMoveUp={() => onMove(index, -1)}
            onMoveDown={() => onMove(index, 1)}
            onUpdate={(next) => onUpdateJuego(index, next)}
          />
        ))}
      </div>

      <div className="create-actions">
        <button type="button" className="ghost-button" data-testid="btn-atras" onClick={onAtras}>
          Atras
        </button>
        <button type="button" data-testid="btn-siguiente" onClick={onSiguiente}>
          Siguiente
        </button>
      </div>
    </section>
  );
}

function JuegoCard({
  juego,
  index,
  total,
  onRemove,
  onMoveUp,
  onMoveDown,
  onUpdate
}: {
  juego: JuegoDraft;
  index: number;
  total: number;
  onRemove: () => void;
  onMoveUp: () => void;
  onMoveDown: () => void;
  onUpdate: (next: JuegoDraft) => void;
}) {
  const numero = index + 1;
  const tipoLabel = tipoJuegoLabel(juego.tipo);

  return (
    <section className="question-card" aria-label={`Juego ${numero}`}>
      <div className="question-card-header">
        <h3 className="q-title">
          <span className="q-badge" aria-hidden="true">
            {numero}
          </span>
          Juego {numero} — {tipoLabel}
        </h3>
        <div className="actions">
          <button
            type="button"
            className="ghost-button"
            onClick={onMoveUp}
            disabled={index === 0}
            aria-label={`Subir juego ${numero}`}
          >
            ↑
          </button>
          <button
            type="button"
            className="ghost-button"
            onClick={onMoveDown}
            disabled={index === total - 1}
            aria-label={`Bajar juego ${numero}`}
          >
            ↓
          </button>
          <button
            type="button"
            className="secondary-button btn-icon"
            onClick={onRemove}
            aria-label={`Eliminar juego ${numero}`}
          >
            <X />
            Eliminar
          </button>
        </div>
      </div>

      {juego.tipo === "Trivia" ? (
        <TriviaEditor juego={juego} juegoIndex={index} onUpdate={onUpdate} />
      ) : (
        <BdtEditor juego={juego} juegoIndex={index} onUpdate={onUpdate} />
      )}
    </section>
  );
}

function TriviaEditor({
  juego,
  juegoIndex,
  onUpdate
}: {
  juego: JuegoTriviaDraft;
  juegoIndex: number;
  onUpdate: (next: JuegoDraft) => void;
}) {
  function patchPregunta(pIndex: number, patch: Partial<PreguntaDraft>) {
    onUpdate({
      ...juego,
      preguntas: juego.preguntas.map((p, i) => (i === pIndex ? { ...p, ...patch } : p))
    });
  }

  return (
    <div className="question-list">
      {juego.preguntas.map((pregunta, pIndex) => (
        <PreguntaEditor
          key={pIndex}
          pregunta={pregunta}
          juegoIndex={juegoIndex}
          preguntaIndex={pIndex}
          onPatch={(patch) => patchPregunta(pIndex, patch)}
          onRemove={() =>
            onUpdate({ ...juego, preguntas: juego.preguntas.filter((_, i) => i !== pIndex) })
          }
        />
      ))}
      <div className="create-actions">
        <button
          type="button"
          className="ghost-button"
          onClick={() => onUpdate({ ...juego, preguntas: [...juego.preguntas, newPregunta()] })}
        >
          + Agregar pregunta
        </button>
        <span />
      </div>
    </div>
  );
}

function PreguntaEditor({
  pregunta,
  juegoIndex,
  preguntaIndex,
  onPatch,
  onRemove
}: {
  pregunta: PreguntaDraft;
  juegoIndex: number;
  preguntaIndex: number;
  onPatch: (patch: Partial<PreguntaDraft>) => void;
  onRemove: () => void;
}) {
  const n = preguntaIndex + 1;
  const baseId = `pregunta-${juegoIndex}-${preguntaIndex}`;
  const radioName = `correcta-${juegoIndex}-${preguntaIndex}`;

  function patchOpcion(oIndex: number, patch: { texto?: string; esCorrecta?: boolean }) {
    onPatch({
      opciones: pregunta.opciones.map((o, i) => {
        if (patch.esCorrecta !== undefined) {
          return { ...o, esCorrecta: i === oIndex };
        }
        return i === oIndex ? { ...o, ...patch } : o;
      })
    });
  }

  return (
    <section className="question-card" aria-label={`Pregunta ${n} del juego ${juegoIndex + 1}`}>
      <div className="question-card-header">
        <h4 className="q-title">
          <span className="q-badge" aria-hidden="true">
            {n}
          </span>
          Pregunta {n}
        </h4>
        <button
          type="button"
          className="secondary-button btn-icon"
          onClick={onRemove}
          aria-label={`Eliminar pregunta ${n} del juego ${juegoIndex + 1}`}
        >
          <X />
          Eliminar
        </button>
      </div>

      <label htmlFor={`${baseId}-texto`}>
        Texto de la pregunta {n}
        <input
          id={`${baseId}-texto`}
          value={pregunta.texto}
          onChange={(event) => onPatch({ texto: event.target.value })}
        />
      </label>

      <div className="stack">
        {pregunta.opciones.map((opcion, oIndex) => (
          <div className="row" key={oIndex}>
            <label htmlFor={`${baseId}-opcion-${oIndex}`}>
              Opcion {oIndex + 1} pregunta {n}
              <input
                id={`${baseId}-opcion-${oIndex}`}
                value={opcion.texto}
                onChange={(event) => patchOpcion(oIndex, { texto: event.target.value })}
              />
            </label>
            <div className="check-row">
              <input
                type="radio"
                name={radioName}
                id={`${baseId}-correcta-${oIndex}`}
                checked={opcion.esCorrecta}
                onChange={() => patchOpcion(oIndex, { esCorrecta: true })}
              />
              <label htmlFor={`${baseId}-correcta-${oIndex}`}>Correcta</label>
              <button
                type="button"
                className="ghost-button"
                onClick={() =>
                  onPatch({ opciones: pregunta.opciones.filter((_, i) => i !== oIndex) })
                }
                disabled={pregunta.opciones.length <= 1}
                aria-label={`Eliminar opcion ${oIndex + 1} pregunta ${n}`}
              >
                <X />
              </button>
            </div>
          </div>
        ))}
        <button
          type="button"
          className="ghost-button"
          onClick={() => onPatch({ opciones: [...pregunta.opciones, { texto: "", esCorrecta: false }] })}
        >
          + Agregar opcion
        </button>
      </div>

      <div className="q-meta">
        <label htmlFor={`${baseId}-puntaje`}>
          Puntaje
          <input
            id={`${baseId}-puntaje`}
            type="number"
            min="1"
            value={pregunta.puntaje}
            onChange={(event) => onPatch({ puntaje: event.target.value })}
          />
        </label>
        <label htmlFor={`${baseId}-tiempo`}>
          Tiempo limite (segundos)
          <input
            id={`${baseId}-tiempo`}
            type="number"
            min="1"
            value={pregunta.tiempoLimiteSegundos}
            onChange={(event) => onPatch({ tiempoLimiteSegundos: event.target.value })}
          />
        </label>
      </div>
    </section>
  );
}

function BdtEditor({
  juego,
  juegoIndex,
  onUpdate
}: {
  juego: JuegoBdtDraft;
  juegoIndex: number;
  onUpdate: (next: JuegoDraft) => void;
}) {
  const areaId = `area-busqueda-${juegoIndex}`;
  // Indexado por codigoQREsperado (no por eIndex): asi no hace falta re-renderizar el QR
  // en cada tecla que el operador escribe en otros campos de la etapa.
  const [qrDataUrls, setQrDataUrls] = useState<Record<string, string>>({});
  // Indexado por eIndex: si renderizarQrDataUrl falla, la etapa no debe quedar con un
  // codigo "valido" sin QR asociado (ver nota en el onClick de mas abajo).
  const [qrErrors, setQrErrors] = useState<Record<number, string>>({});

  function patchEtapa(eIndex: number, patch: Partial<EtapaDraft>) {
    onUpdate({
      ...juego,
      etapas: juego.etapas.map((e, i) => (i === eIndex ? { ...e, ...patch } : e))
    });
  }

  return (
    <div className="stack">
      <label htmlFor={areaId}>
        Area de busqueda
        <textarea
          id={areaId}
          value={juego.areaBusqueda}
          onChange={(event) => onUpdate({ ...juego, areaBusqueda: event.target.value })}
        />
      </label>

      <div className="question-list">
        {juego.etapas.map((etapa, eIndex) => {
          const n = eIndex + 1;
          const baseId = `etapa-${juegoIndex}-${eIndex}`;
          return (
            <section
              className="question-card"
              key={eIndex}
              aria-label={`Etapa ${n} del juego ${juegoIndex + 1}`}
            >
              <div className="question-card-header">
                <h4 className="q-title">
                  <span className="q-badge" aria-hidden="true">
                    {n}
                  </span>
                  Etapa {n}
                </h4>
                <button
                  type="button"
                  className="secondary-button btn-icon"
                  onClick={() =>
                    onUpdate({ ...juego, etapas: juego.etapas.filter((_, i) => i !== eIndex) })
                  }
                  aria-label={`Eliminar etapa ${n} del juego ${juegoIndex + 1}`}
                >
                  <X />
                  Eliminar
                </button>
              </div>

              <div className="stack">
                <button
                  type="button"
                  className="ghost-button"
                  onClick={async () => {
                    // El codigo solo se confirma en el draft si renderizarQrDataUrl tiene
                    // exito: si patcheamos antes y el render falla, la etapa quedaria con un
                    // codigoQREsperado "valido" (pasa validateEtapa y el backend lo aceptaria
                    // como UUID unico) pero sin QR ni descarga — el operador crearia una
                    // partida con un tesoro que nadie puede fotografiar, sin ningun aviso.
                    setQrErrors((prev) => {
                      const { [eIndex]: _omitida, ...resto } = prev;
                      return resto;
                    });
                    const codigo = generarCodigoTesoro();
                    try {
                      const dataUrl = await renderizarQrDataUrl(codigo);
                      patchEtapa(eIndex, { codigoQREsperado: codigo });
                      setQrDataUrls((prev) => ({ ...prev, [codigo]: dataUrl }));
                    } catch {
                      setQrErrors((prev) => ({
                        ...prev,
                        [eIndex]: `No se pudo generar el QR de la etapa ${n}. Intenta de nuevo.`
                      }));
                    }
                  }}
                >
                  {etapa.codigoQREsperado ? `Regenerar QR etapa ${n}` : `Generar QR etapa ${n}`}
                </button>

                {qrErrors[eIndex] ? <p className="notice error">{qrErrors[eIndex]}</p> : null}

                {etapa.codigoQREsperado && qrDataUrls[etapa.codigoQREsperado] ? (
                  <>
                    <img
                      src={qrDataUrls[etapa.codigoQREsperado]}
                      alt={`QR del tesoro de la etapa ${n}`}
                      width={160}
                      height={160}
                    />
                    <a href={qrDataUrls[etapa.codigoQREsperado]} download={nombreArchivoQr(n)}>
                      Descargar QR etapa {n}
                    </a>
                  </>
                ) : null}
              </div>

              <div className="q-meta">
                <label htmlFor={`${baseId}-puntaje`}>
                  Puntaje
                  <input
                    id={`${baseId}-puntaje`}
                    type="number"
                    min="1"
                    value={etapa.puntaje}
                    onChange={(event) => patchEtapa(eIndex, { puntaje: event.target.value })}
                  />
                </label>
                <label htmlFor={`${baseId}-tiempo`}>
                  Tiempo limite (segundos)
                  <input
                    id={`${baseId}-tiempo`}
                    type="number"
                    min="1"
                    value={etapa.tiempoLimiteSegundos}
                    onChange={(event) => patchEtapa(eIndex, { tiempoLimiteSegundos: event.target.value })}
                  />
                </label>
              </div>
            </section>
          );
        })}
      </div>

      <button
        type="button"
        className="ghost-button"
        onClick={() => onUpdate({ ...juego, etapas: [...juego.etapas, newEtapa()] })}
      >
        + Agregar etapa
      </button>
    </div>
  );
}

function PasoResumen({
  draft,
  envio,
  enviando,
  onCrear,
  onReintentar,
  onAtras
}: {
  draft: CreatePartidaDraft;
  envio: EnvioState | null;
  enviando: boolean;
  onCrear: () => void;
  onReintentar: () => void;
  onAtras: () => void;
}) {
  const fallo =
    envio !== null &&
    !enviando &&
    (envio.errorHeader !== undefined || envio.estados.some((e) => e.estado === "error"));

  return (
    <section className="form-section" data-testid="paso-3">
      <div className="form-section__head">
        <h2 className="form-section__title">Resumen</h2>
        <p className="form-section__hint">Revisa los datos antes de crear la partida.</p>
      </div>

      <dl className="detail-grid">
        <div>
          <dt>Nombre</dt>
          <dd>{draft.header.nombrePartida}</dd>
        </div>
        <div>
          <dt>Modalidad</dt>
          <dd>{draft.header.modalidad}</dd>
        </div>
        <div>
          <dt>Modo de inicio</dt>
          <dd>{draft.header.modoInicioPartida}</dd>
        </div>
        {draft.header.modoInicioPartida !== "Manual" ? (
          <div>
            <dt>Tiempo de inicio</dt>
            <dd>{draft.header.tiempoInicio}</dd>
          </div>
        ) : null}
        <div>
          <dt>Minimo de participacion</dt>
          <dd>{draft.header.minimosParticipacion}</dd>
        </div>
        <div>
          <dt>Maximo de participacion</dt>
          <dd>{draft.header.maximosParticipacion}</dd>
        </div>
      </dl>

      <div className="question-list">
        {draft.juegos.map((juego, i) => {
          const estado: EstadoEnvio = envio?.estados[i]?.estado ?? "pendiente";
          const mensaje = envio?.estados[i]?.mensaje;
          return (
            <section className="question-card" key={juego.localId}>
              <div className="question-card-header">
                <h3 className="q-title">
                  <span className="q-badge" aria-hidden="true">
                    {i + 1}
                  </span>
                  Juego {i + 1} — {tipoJuegoLabel(juego.tipo)}
                </h3>
                <div data-testid={`envio-juego-${i}`}>
                  <span className={`pill ${pillClassFor(estado)}`}>
                    <span className="pill__dot" />
                    {estado}
                  </span>
                </div>
              </div>
              {juego.tipo === "Trivia" ? (
                <p className="muted">{juego.preguntas.length} pregunta(s).</p>
              ) : (
                <p className="muted">
                  {juego.areaBusqueda} — {juego.etapas.length} etapa(s).
                </p>
              )}
              {mensaje ? <p className="notice error">{mensaje}</p> : null}
            </section>
          );
        })}
      </div>

      {envio?.errorHeader ? (
        <div className="notice error" role="alert">
          {envio.errorHeader}
        </div>
      ) : null}

      <div className="create-actions">
        <button
          type="button"
          className="ghost-button"
          data-testid="btn-atras"
          onClick={onAtras}
          disabled={enviando}
        >
          Atras
        </button>
        {fallo ? (
          <button type="button" data-testid="btn-reintentar" onClick={onReintentar} disabled={enviando}>
            Reintentar restantes
          </button>
        ) : (
          <button type="button" data-testid="btn-crear-partida" onClick={onCrear} disabled={enviando}>
            {enviando ? "Creando..." : "Crear partida"}
          </button>
        )}
      </div>
    </section>
  );
}

function ErrorList({ errors }: { errors: string[] }) {
  if (errors.length === 0) return null;
  return (
    <div className="notice error" role="alert">
      <ul>
        {errors.map((message, index) => (
          <li key={index}>{message}</li>
        ))}
      </ul>
    </div>
  );
}

function tipoJuegoLabel(tipo: JuegoDraft["tipo"]): string {
  return tipo === "Trivia" ? "Trivia" : "Búsqueda del Tesoro";
}

function pillClassFor(estado: EstadoEnvio): string {
  switch (estado) {
    case "ok":
      return "pill--ok";
    case "error":
      return "pill--cancel";
    case "enviando":
      return "pill--live";
    default:
      return "pill--done";
  }
}
