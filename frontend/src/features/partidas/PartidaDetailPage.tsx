// Detalle de solo lectura de una partida: header + juegos (Trivia/BDT) via getPartida.
import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  getPartida,
  PartidasApiError,
  type EtapaDetail,
  type JuegoDetail,
  type PartidaDetail,
  type PreguntaDetail
} from "../../api/partidasApi";
import { publicarPartida, OperacionesApiError } from "../../api/operacionesApi";
import { nombreArchivoQr, renderizarQrDataUrl } from "./qrTesoro";

interface PartidaDetailPageProps {
  accessToken: string;
  puedeOperar: boolean;
}

type LoadState =
  | { status: "loading" }
  | { status: "error"; message: string }
  | { status: "ready"; partida: PartidaDetail };

export function PartidaDetailPage({ accessToken, puedeOperar }: PartidaDetailPageProps) {
  const { partidaId } = useParams<{ partidaId: string }>();
  const navigate = useNavigate();
  const [state, setState] = useState<LoadState>({ status: "loading" });

  useEffect(() => {
    if (!partidaId) {
      setState({ status: "error", message: "Partida no encontrada" });
      return;
    }

    let active = true;
    setState({ status: "loading" });
    getPartida(partidaId, accessToken)
      .then((partida) => {
        if (active) setState({ status: "ready", partida });
      })
      .catch((caught) => {
        if (!active) return;
        setState({
          status: "error",
          message:
            caught instanceof PartidasApiError
              ? mapErrorMessage(caught.statusCode)
              : "Error inesperado al consultar la partida."
        });
      });

    return () => {
      active = false;
    };
  }, [partidaId, accessToken]);

  return (
    <div className="page" data-testid="detalle-partida">
      {state.status === "loading" ? <p className="muted">Cargando partida…</p> : null}

      {state.status === "error" ? (
        <div className="card stack">
          <div className="notice error" role="alert">
            {state.message}
          </div>
          <button type="button" className="secondary-button" onClick={() => navigate("/partidas")}>
            Volver a partidas
          </button>
        </div>
      ) : null}

      {state.status === "ready" ? (
        <PartidaDetailContent
          partida={state.partida}
          accessToken={accessToken}
          puedeOperar={puedeOperar}
        />
      ) : null}
    </div>
  );
}

function PartidaDetailContent({
  partida,
  accessToken,
  puedeOperar
}: {
  partida: PartidaDetail;
  accessToken: string;
  puedeOperar: boolean;
}) {
  const navigate = useNavigate();
  const [publicando, setPublicando] = useState(false);
  const [pubError, setPubError] = useState<string | null>(null);
  const pillEstado = estadoPill(partida.estado);
  const juegos = [...partida.juegos].sort((a, b) => a.orden - b.orden);

  async function onPublicar() {
    setPublicando(true);
    setPubError(null);
    try {
      await publicarPartida(partida.partidaId, accessToken);
      navigate(`/partidas/${partida.partidaId}/sesion`);
    } catch (caught) {
      if (caught instanceof OperacionesApiError && caught.statusCode === 409) {
        navigate(`/partidas/${partida.partidaId}/sesion`);
        return;
      }
      setPubError(caught instanceof Error ? caught.message : "No se pudo publicar la partida.");
    } finally {
      setPublicando(false);
    }
  }

  return (
    <div className="card stack">
      <header className="create-head">
        <div>
          <h1>{partida.nombrePartida}</h1>
          <div className="compact-actions">
            <Pill cls="pill--done" label={partida.modalidad} />
            <Pill cls="pill--done" label={partida.modoInicioPartida} />
            <Pill cls={pillEstado.cls} label={pillEstado.label} />
            <Pill
              cls="pill--done"
              label={`Min ${partida.minimosParticipacion} · Max ${partida.maximosParticipacion}`}
            />
          </div>
          <button
            type="button"
            className="secondary-button"
            onClick={() => navigate(`/partidas/${partida.partidaId}/historial`)}
          >
            Historial de eventos
          </button>
        </div>
        {puedeOperar ? (
          <button
            type="button"
            data-testid="btn-publicar-operar"
            disabled={publicando}
            onClick={() => void onPublicar()}
          >
            Publicar y operar
          </button>
        ) : null}
      </header>

      {pubError ? (
        <div className="notice error" role="alert">
          {pubError}
        </div>
      ) : null}

      <div className="question-list">
        {juegos.map((juego) => (
          <JuegoCard key={juego.juegoId} juego={juego} />
        ))}
      </div>
    </div>
  );
}

function JuegoCard({ juego }: { juego: JuegoDetail }) {
  const tipoLabel = juego.tipoJuego === "Trivia" ? "Trivia" : "Búsqueda del Tesoro";

  return (
    <section className="question-card" data-testid={`juego-${juego.orden}`}>
      <div className="question-card-header">
        <h3 className="q-title">
          <span className="q-badge" aria-hidden="true">
            {juego.orden}
          </span>
          Juego {juego.orden} — {tipoLabel}
        </h3>
      </div>

      {juego.trivia ? <TriviaView preguntas={juego.trivia.preguntas} /> : null}
      {juego.bdt ? (
        <BdtView areaBusqueda={juego.bdt.areaBusqueda} etapas={juego.bdt.etapas} juegoOrden={juego.orden} />
      ) : null}
    </section>
  );
}

function TriviaView({ preguntas }: { preguntas: PreguntaDetail[] }) {
  return (
    <div className="question-list">
      {preguntas.map((pregunta) => (
        <div className="stack" key={pregunta.preguntaId}>
          <p className="q-title">{pregunta.texto}</p>
          <ul>
            {pregunta.opciones.map((opcion) => (
              <li key={opcion.opcionId}>
                <span>{opcion.texto}</span>
                {opcion.esCorrecta ? <span className="badge">Correcta</span> : null}
              </li>
            ))}
          </ul>
          <div className="q-meta">
            <span>Puntaje: {pregunta.puntajeAsignado}</span>
            <span>Tiempo límite: {pregunta.tiempoLimiteSegundos}s</span>
          </div>
        </div>
      ))}
    </div>
  );
}

function BdtView({
  areaBusqueda,
  etapas,
  juegoOrden
}: {
  areaBusqueda: string;
  etapas: EtapaDetail[];
  juegoOrden: number;
}) {
  const [qrDataUrls, setQrDataUrls] = useState<Record<string, string>>({});

  useEffect(() => {
    let vigente = true;
    Promise.allSettled(
      etapas.map(async (e) => [e.etapaBDTId, await renderizarQrDataUrl(e.codigoQREsperado)] as const)
    ).then((resultados) => {
      if (!vigente) return;
      // No dejar que el fallo de una etapa oculte el QR de las demas: cada etapa se
      // resuelve por separado y solo se descarta la que rechazo.
      const pares = resultados
        .filter((resultado): resultado is PromiseFulfilledResult<readonly [string, string]> =>
          resultado.status === "fulfilled"
        )
        .map((resultado) => resultado.value);
      setQrDataUrls(Object.fromEntries(pares));
    });
    return () => {
      vigente = false;
    };
  }, [etapas]);

  return (
    <div className="stack">
      <p>
        <strong>Área de búsqueda:</strong> <span>{areaBusqueda}</span>
      </p>
      <div className="table-wrap">
        <table aria-label="Etapas">
          <thead>
            <tr>
              <th scope="col">Orden</th>
              <th scope="col">QR esperado</th>
              <th scope="col">QR</th>
              <th scope="col">Puntaje</th>
              <th scope="col">Tiempo límite</th>
            </tr>
          </thead>
          <tbody>
            {etapas.map((etapa) => (
              <tr key={etapa.etapaBDTId}>
                <td>{etapa.orden}</td>
                <td className="mono">{etapa.codigoQREsperado}</td>
                <td>
                  {qrDataUrls[etapa.etapaBDTId] ? (
                    <details>
                      <summary>Mostrar QR</summary>
                      <img
                        src={qrDataUrls[etapa.etapaBDTId]}
                        alt={`QR del tesoro del juego ${juegoOrden}, etapa ${etapa.orden}`}
                        width={96}
                        height={96}
                      />
                      <a
                        href={qrDataUrls[etapa.etapaBDTId]}
                        download={nombreArchivoQr(juegoOrden, etapa.orden, etapa.codigoQREsperado)}
                      >
                        Descargar QR etapa {etapa.orden}
                      </a>
                    </details>
                  ) : null}
                </td>
                <td>{etapa.puntajeAsignado}</td>
                <td>{etapa.tiempoLimiteSegundos}s</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function Pill({ cls, label }: { cls: string; label: string }) {
  return (
    <span className={`pill ${cls}`}>
      <span className="pill__dot" />
      {label}
    </span>
  );
}

function estadoPill(estado: string | null): { cls: string; label: string } {
  if (estado === null) {
    return { cls: "pill--warn", label: "Sin publicar" };
  }
  if (estado === "Iniciada") {
    return { cls: "pill--live", label: estado };
  }
  if (estado === "Lobby") {
    return { cls: "pill--lobby", label: estado };
  }
  return { cls: "pill--done", label: estado };
}

function mapErrorMessage(statusCode: number): string {
  switch (statusCode) {
    case 404:
      return "Partida no encontrada";
    case 401:
      return "Sesión expirada o no autenticada. Inicia sesión nuevamente.";
    case 403:
      return "No autorizado. Debes tener rol Operador.";
    case 500:
      return "Error de persistencia al consultar Partidas Service.";
    default:
      return "Error inesperado al consultar la partida.";
  }
}
