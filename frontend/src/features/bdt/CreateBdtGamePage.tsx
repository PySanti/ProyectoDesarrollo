import { FormEvent, useState } from "react";
import {
  BdtApiError,
  BdtModalidad,
  BdtModoInicio,
  createBdtGame,
  CreateBdtGameResponse,
  decodeBdtExpectedQrImage
} from "../../api/bdtApi";

interface CreateBdtGamePageProps {
  accessToken: string;
}

interface FormState {
  nombre: string;
  areaBusqueda: string;
  modalidad: BdtModalidad;
  minimoParticipantes: string;
  maximoParticipantes: string;
  maximoEquipos: string;
  minimoJugadoresPorEquipo: string;
  modoInicio: BdtModoInicio;
  etapas: StageFormState[];
}

interface StageFormState {
  codigoQrEsperado: string;
  tiempoLimiteSegundos: string;
}

const emptyStage: StageFormState = {
  codigoQrEsperado: "",
  tiempoLimiteSegundos: "300"
};

const initialForm: FormState = {
  nombre: "",
  areaBusqueda: "",
  modalidad: "Individual",
  minimoParticipantes: "1",
  maximoParticipantes: "10",
  maximoEquipos: "",
  minimoJugadoresPorEquipo: "",
  modoInicio: "Manual",
  etapas: [emptyStage]
};

export function CreateBdtGamePage({ accessToken }: CreateBdtGamePageProps) {
  const [form, setForm] = useState<FormState>(initialForm);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<CreateBdtGameResponse | null>(null);
  const [qrDecodeState, setQrDecodeState] = useState<Record<number, string>>({});

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
      const created = await createBdtGame(
        {
          nombre: form.nombre.trim(),
          areaBusqueda: form.areaBusqueda.trim(),
          modalidad: form.modalidad,
          minimoParticipantes: Number(form.minimoParticipantes),
          maximoParticipantes:
            form.modalidad === "Individual" ? Number(form.maximoParticipantes) : null,
          maximoEquipos: form.modalidad === "Equipo" ? Number(form.maximoEquipos) : null,
          minimoJugadoresPorEquipo:
            form.modalidad === "Equipo" ? Number(form.minimoJugadoresPorEquipo) : null,
          modoInicio: form.modoInicio,
          etapas: form.etapas.map((etapa, index) => ({
            orden: index + 1,
            codigoQrEsperado: etapa.codigoQrEsperado.trim(),
            tiempoLimiteSegundos: Number(etapa.tiempoLimiteSegundos)
          }))
        },
        accessToken
      );
      setResult(created);
      setForm(initialForm);
      setQrDecodeState({});
    } catch (caught) {
      if (caught instanceof BdtApiError) {
        setError(mapErrorMessage(caught.statusCode, caught.message));
      } else {
        setError("Error inesperado al crear la partida BDT.");
      }
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="page">
      <div className="card">
        <h1>Crear partida BDT</h1>
        <p>Configura una busqueda del tesoro con area textual, modalidad y etapas con QR esperado.</p>

        {error ? (
          <div role="alert" className="notice error">
            {error}
          </div>
        ) : null}

        {result ? (
          <div className="notice success" data-testid="bdt-create-success">
            Partida creada: <strong>{result.nombre}</strong> en estado {" "}
            <strong>{result.estado}</strong> con {result.cantidadEtapas} etapa(s).
          </div>
        ) : null}

        <form onSubmit={onSubmit} noValidate>
          <label htmlFor="bdt-nombre">
            Nombre
            <input
              id="bdt-nombre"
              value={form.nombre}
              onChange={(event) => setForm((current) => ({ ...current, nombre: event.target.value }))}
            />
          </label>

          <label htmlFor="bdt-area">
            Area de busqueda
            <textarea
              id="bdt-area"
              value={form.areaBusqueda}
              onChange={(event) => setForm((current) => ({ ...current, areaBusqueda: event.target.value }))}
            />
          </label>

          <div className="row">
            <label htmlFor="bdt-modalidad">
              Modalidad
              <select
                id="bdt-modalidad"
                value={form.modalidad}
                onChange={(event) =>
                  setForm((current) => ({ ...current, modalidad: event.target.value as BdtModalidad }))
                }
              >
                <option value="Individual">Individual</option>
                <option value="Equipo">Equipo</option>
              </select>
            </label>

            <label htmlFor="bdt-modo-inicio">
              Modo de inicio
              <select
                id="bdt-modo-inicio"
                value={form.modoInicio}
                onChange={(event) =>
                  setForm((current) => ({ ...current, modoInicio: event.target.value as BdtModoInicio }))
                }
              >
                <option value="Manual">Manual</option>
                <option value="Automatico">Automatico</option>
                <option value="ManualYAutomatico">Manual y automatico</option>
              </select>
            </label>
          </div>

          <div className="row">
            <label htmlFor="bdt-minimo">
              Minimo participantes
              <input
                id="bdt-minimo"
                type="number"
                min="1"
                value={form.minimoParticipantes}
                onChange={(event) => setForm((current) => ({ ...current, minimoParticipantes: event.target.value }))}
              />
            </label>

            {form.modalidad === "Individual" ? (
              <label htmlFor="bdt-maximo-participantes">
                Maximo jugadores
                <input
                  id="bdt-maximo-participantes"
                  type="number"
                  min="1"
                  value={form.maximoParticipantes}
                  onChange={(event) => setForm((current) => ({ ...current, maximoParticipantes: event.target.value }))}
                />
              </label>
            ) : (
              <>
                <label htmlFor="bdt-maximo-equipos">
                  Maximo equipos
                  <input
                    id="bdt-maximo-equipos"
                    type="number"
                    min="1"
                    value={form.maximoEquipos}
                    onChange={(event) => setForm((current) => ({ ...current, maximoEquipos: event.target.value }))}
                  />
                </label>

                <label htmlFor="bdt-minimo-jugadores-equipo">
                  Minimo jugadores por equipo
                  <input
                    id="bdt-minimo-jugadores-equipo"
                    type="number"
                    min="1"
                    value={form.minimoJugadoresPorEquipo}
                    onChange={(event) =>
                      setForm((current) => ({ ...current, minimoJugadoresPorEquipo: event.target.value }))
                    }
                  />
                </label>
              </>
            )}
          </div>

          <div className="row">
            <h2>Etapas</h2>
            <button type="button" onClick={() => setForm((current) => ({ ...current, etapas: [...current.etapas, emptyStage] }))}>
              Agregar etapa
            </button>
          </div>

          {form.etapas.map((etapa, index) => {
            const stageNumber = index + 1;
            const qrId = `bdt-qr-${stageNumber}`;
            const qrImageId = `bdt-qr-image-${stageNumber}`;
            const timeId = `bdt-tiempo-${stageNumber}`;
            const decodeStatus = qrDecodeState[index];

            return (
              <fieldset key={stageNumber}>
                <legend>Etapa {stageNumber}</legend>
                <div className="row">
                  <label htmlFor={qrId}>
                    QR esperado etapa {stageNumber}
                    <input
                      id={qrId}
                      value={etapa.codigoQrEsperado}
                      onChange={(event) => updateStage(form, setForm, index, { codigoQrEsperado: event.target.value })}
                    />
                  </label>

                  <label htmlFor={qrImageId}>
                    Imagen QR etapa {stageNumber}
                    <input
                      id={qrImageId}
                      type="file"
                      accept="image/png,image/jpeg"
                      onChange={(event) => {
                        const file = event.target.files?.[0];
                        if (file) {
                          void decodeExpectedQrImage(file, index, form, setForm, setQrDecodeState, setError, accessToken);
                        }
                        event.target.value = "";
                      }}
                    />
                  </label>

                  <label htmlFor={timeId}>
                    Tiempo limite segundos etapa {stageNumber}
                    <input
                      id={timeId}
                      type="number"
                      min="1"
                      value={etapa.tiempoLimiteSegundos}
                      onChange={(event) => updateStage(form, setForm, index, { tiempoLimiteSegundos: event.target.value })}
                    />
                  </label>
                </div>

                {decodeStatus ? <p className="form-help">{decodeStatus}</p> : null}

                {form.etapas.length > 1 ? (
                  <button type="button" onClick={() => removeStage(setForm, index)}>
                    Eliminar etapa {stageNumber}
                  </button>
                ) : null}
              </fieldset>
            );
          })}

          <button type="submit" disabled={loading}>
            {loading ? "Creando BDT..." : "Crear partida BDT"}
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

  if (!form.areaBusqueda.trim()) {
    return "El area de busqueda es obligatoria.";
  }

  if (Number(form.minimoParticipantes) <= 0) {
    return "El minimo de participantes debe ser mayor que cero.";
  }

  if (form.modalidad === "Individual" && Number(form.maximoParticipantes) <= 0) {
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

  if (form.etapas.length === 0) {
    return "Debe existir al menos una etapa.";
  }

  for (const [index, etapa] of form.etapas.entries()) {
    const stageNumber = index + 1;

    if (!etapa.codigoQrEsperado.trim()) {
      return `El QR esperado de la etapa ${stageNumber} es obligatorio.`;
    }

    if (Number(etapa.tiempoLimiteSegundos) <= 0) {
      return `El tiempo limite de la etapa ${stageNumber} debe ser mayor que cero.`;
    }
  }

  return null;
}

function updateStage(
  _form: FormState,
  setForm: (updater: (current: FormState) => FormState) => void,
  index: number,
  changes: Partial<StageFormState>
) {
  setForm((current) => ({
    ...current,
    etapas: current.etapas.map((etapa, currentIndex) =>
      currentIndex === index ? { ...etapa, ...changes } : etapa
    )
  }));
}

function removeStage(setForm: (updater: (current: FormState) => FormState) => void, index: number) {
  setForm((current) => ({
    ...current,
    etapas: current.etapas.filter((_, currentIndex) => currentIndex !== index)
  }));
}

async function decodeExpectedQrImage(
  image: File,
  index: number,
  form: FormState,
  setForm: (updater: (current: FormState) => FormState) => void,
  setQrDecodeState: (updater: (current: Record<number, string>) => Record<number, string>) => void,
  setError: (message: string | null) => void,
  accessToken: string
) {
  setError(null);
  setQrDecodeState((current) => ({ ...current, [index]: "Decodificando imagen QR..." }));

  try {
    const response = await decodeBdtExpectedQrImage(image, accessToken);
    if (response.estadoProcesamiento === "Decodificado" && response.qrDecodificado) {
      updateStage(form, setForm, index, { codigoQrEsperado: response.qrDecodificado });
      setQrDecodeState((current) => ({ ...current, [index]: "QR decodificado correctamente." }));
      return;
    }

    setQrDecodeState((current) => ({ ...current, [index]: response.mensaje }));
  } catch (caught) {
    if (caught instanceof BdtApiError) {
      setQrDecodeState((current) => ({ ...current, [index]: mapQrDecodeError(caught.statusCode, caught.message) }));
    } else {
      setQrDecodeState((current) => ({ ...current, [index]: "Error inesperado al decodificar el QR." }));
    }
  }
}

function mapErrorMessage(statusCode: number, fallbackMessage: string): string {
  switch (statusCode) {
    case 400:
      return "Solicitud invalida. Verifica la configuracion BDT.";
    case 403:
      return "No autorizado. Debes tener rol Operador.";
    case 409:
      return "La configuracion de modalidad y limites no es valida.";
    case 500:
      return "Error de persistencia en BDT Game Service.";
    default:
      return fallbackMessage;
  }
}

function mapQrDecodeError(statusCode: number, fallbackMessage: string): string {
  switch (statusCode) {
    case 400:
      return "Selecciona una imagen QR valida.";
    case 403:
      return "No autorizado. Debes tener rol Operador.";
    case 413:
      return "La imagen QR no puede superar 5 MB.";
    case 415:
      return "Solo se aceptan imagenes JPEG o PNG.";
    case 500:
      return "Error del servicio al decodificar el QR.";
    default:
      return fallbackMessage;
  }
}
