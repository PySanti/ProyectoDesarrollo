import { ChangeEvent, FormEvent, useState } from "react";
import {
  BdtApiError,
  BdtModalidad,
  BdtModoInicio,
  createBdtGame,
  CreateBdtGameResponse,
  decodeExpectedBdtQrImage
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
  qrImageName: string;
  qrDecodeStatus: "idle" | "decoding" | "decoded" | "error";
  qrDecodeError: string | null;
  tiempoLimiteSegundos: string;
}

function createEmptyStage(): StageFormState {
  return {
  codigoQrEsperado: "",
  qrImageName: "",
  qrDecodeStatus: "idle",
  qrDecodeError: null,
  tiempoLimiteSegundos: "300"
};
}

const initialForm: FormState = {
  nombre: "",
  areaBusqueda: "",
  modalidad: "Individual",
  minimoParticipantes: "1",
  maximoParticipantes: "10",
  maximoEquipos: "",
  minimoJugadoresPorEquipo: "",
  modoInicio: "Manual",
  etapas: [createEmptyStage()]
};

export function CreateBdtGamePage({ accessToken }: CreateBdtGamePageProps) {
  const [form, setForm] = useState<FormState>(initialForm);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<CreateBdtGameResponse | null>(null);

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

  async function onQrImageSelected(index: number, event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = "";
    if (!file) {
      return;
    }

    updateStage(setForm, index, {
      codigoQrEsperado: "",
      qrImageName: file.name,
      qrDecodeStatus: "decoding",
      qrDecodeError: null
    });
    setError(null);
    setResult(null);

    try {
      const response = await decodeExpectedBdtQrImage(file, accessToken);
      updateStage(setForm, index, {
        codigoQrEsperado: response.qrDecodificado,
        qrDecodeStatus: "decoded",
        qrDecodeError: null
      });
    } catch (caught) {
      const message = caught instanceof BdtApiError
        ? mapQrDecodeErrorMessage(caught.statusCode, caught.message)
        : "No se pudo decodificar el QR de la imagen.";
      updateStage(setForm, index, {
        codigoQrEsperado: "",
        qrDecodeStatus: "error",
        qrDecodeError: message
      });
    }
  }

  return (
    <div className="page">
      <div className="card">
        <h1>Crear partida BDT</h1>
        <p>Configura una busqueda del tesoro con area textual, modalidad y etapas con una imagen QR esperada por etapa.</p>

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
            <button type="button" onClick={() => setForm((current) => ({ ...current, etapas: [...current.etapas, createEmptyStage()] }))}>
              Agregar etapa
            </button>
          </div>

          {form.etapas.map((etapa, index) => {
            const stageNumber = index + 1;
            const qrId = `bdt-qr-${stageNumber}`;
            const timeId = `bdt-tiempo-${stageNumber}`;
            const qrStatusId = `bdt-qr-status-${stageNumber}`;

            return (
              <fieldset key={stageNumber}>
                <legend>Etapa {stageNumber}</legend>
                <div className="row">
                  <label htmlFor={qrId}>
                    Imagen QR esperada etapa {stageNumber}
                    <input
                      id={qrId}
                      type="file"
                      accept="image/png,image/jpeg"
                      onChange={(event) => void onQrImageSelected(index, event)}
                    />
                  </label>

                  <label htmlFor={timeId}>
                    Tiempo limite segundos etapa {stageNumber}
                    <input
                      id={timeId}
                      type="number"
                      min="1"
                      value={etapa.tiempoLimiteSegundos}
                      onChange={(event) => updateStage(setForm, index, { tiempoLimiteSegundos: event.target.value })}
                    />
                  </label>
                </div>

                <p id={qrStatusId} className={etapa.qrDecodeStatus === "error" ? "notice error" : "notice"}>
                  {formatQrDecodeStatus(etapa)}
                </p>

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

    if (etapa.qrDecodeStatus === "decoding") {
      return `Espera a que termine la decodificacion del QR de la etapa ${stageNumber}.`;
    }

    if (!etapa.codigoQrEsperado.trim()) {
      return `Debes subir una imagen QR valida para la etapa ${stageNumber}.`;
    }

    if (Number(etapa.tiempoLimiteSegundos) <= 0) {
      return `El tiempo limite de la etapa ${stageNumber} debe ser mayor que cero.`;
    }
  }

  return null;
}

function updateStage(
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

function formatQrDecodeStatus(etapa: StageFormState): string {
  if (etapa.qrDecodeStatus === "decoding") {
    return `Decodificando ${etapa.qrImageName}...`;
  }

  if (etapa.qrDecodeStatus === "decoded") {
    return `QR detectado correctamente desde ${etapa.qrImageName}.`;
  }

  if (etapa.qrDecodeStatus === "error") {
    return etapa.qrDecodeError ?? "No se pudo leer un QR en la imagen.";
  }

  return "Sube una imagen PNG o JPEG que contenga el QR esperado de esta etapa.";
}

function removeStage(setForm: (updater: (current: FormState) => FormState) => void, index: number) {
  setForm((current) => ({
    ...current,
    etapas: current.etapas.filter((_, currentIndex) => currentIndex !== index)
  }));
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

function mapQrDecodeErrorMessage(statusCode: number, fallbackMessage: string): string {
  switch (statusCode) {
    case 413:
      return "La imagen QR no puede superar 5 MB.";
    case 415:
      return "Solo se aceptan imagenes QR JPEG o PNG.";
    case 422:
      return "No se pudo leer un QR en la imagen seleccionada.";
    case 403:
      return "No autorizado. Debes tener rol Operador para decodificar QR.";
    default:
      return fallbackMessage;
  }
}
