// Runtime Trivia del operador: pregunta activa + avance + finalizar + ranking del juego.
import { useCallback, useEffect, useState } from "react";
import {
  avanzarPregunta,
  finalizarJuegoActual,
  getPreguntaActual,
  OperacionesApiError,
  type PreguntaActualDto
} from "../../api/operacionesApi";
import { getRankingJuego, type RankingJuegoDto } from "../../api/puntuacionesApi";
import type { PreguntaDetail } from "../../api/partidasApi";
import { Countdown, RankingView } from "./runtimeShared";

export interface TriviaRuntimePanelProps {
  partidaId: string;
  juegoId: string;
  accessToken: string;
  preguntasConfig: PreguntaDetail[];
  puedeOperar: boolean;
  refetchSignal: number;
  onTerminada: () => void;
  onJuegoAvanzado: () => void;
  rankingPush?: RankingJuegoDto | null;
}

type PreguntaVista =
  | { status: "cargando" }
  | { status: "activa"; pregunta: PreguntaActualDto }
  | { status: "sin-pregunta" }
  | { status: "error"; message: string };

export function TriviaRuntimePanel(props: TriviaRuntimePanelProps) {
  const { partidaId, juegoId, accessToken, preguntasConfig, puedeOperar, refetchSignal, onTerminada, onJuegoAvanzado, rankingPush } = props;
  const [preguntaVista, setPreguntaVista] = useState<PreguntaVista>({ status: "cargando" });
  const [ranking, setRanking] = useState<RankingJuegoDto | null>(null);
  const [posteando, setPosteando] = useState(false);
  const [tick, setTick] = useState(0); // refetch interno tras avance/finalizar fallido

  const refetch = useCallback(() => setTick((t) => t + 1), []);

  useEffect(() => {
    let active = true;
    getPreguntaActual(partidaId, accessToken)
      .then((pregunta) => {
        if (active) setPreguntaVista({ status: "activa", pregunta });
      })
      .catch((caught) => {
        if (!active) return;
        if (caught instanceof OperacionesApiError && caught.statusCode === 409) {
          setPreguntaVista({ status: "sin-pregunta" });
        } else {
          setPreguntaVista({
            status: "error",
            message: caught instanceof Error ? caught.message : "Error al consultar la pregunta."
          });
        }
      });
    getRankingJuego(partidaId, juegoId, accessToken)
      .then((r) => {
        if (active) setRanking(r);
      })
      .catch(() => {
        if (active) setRanking(null);
      });
    return () => {
      active = false;
    };
  }, [partidaId, juegoId, accessToken, refetchSignal, tick]);

  // Push SP-4c aditivo: aplica el ranking recibido por hub si es de este juego.
  useEffect(() => {
    if (rankingPush && rankingPush.juegoId === juegoId) {
      setRanking(rankingPush);
    }
  }, [rankingPush, juegoId]);

  async function onAvanzar() {
    setPosteando(true);
    try {
      await avanzarPregunta(partidaId, accessToken);
    } catch {
      // 409 de carrera/barrido: el refetch de abajo trae el estado real.
    } finally {
      setPosteando(false);
      refetch();
    }
  }

  async function onFinalizar() {
    setPosteando(true);
    try {
      const r = await finalizarJuegoActual(partidaId, accessToken);
      if (r.terminada) {
        onTerminada();
        return;
      }
      if (r.juegoActivadoOrden != null) {
        onJuegoAvanzado();
        return;
      }
      refetch();
    } catch {
      refetch();
    } finally {
      setPosteando(false);
    }
  }

  return (
    <div className="stack" data-testid="trivia-runtime">
      {preguntaVista.status === "cargando" ? <p className="muted">Cargando pregunta…</p> : null}
      {preguntaVista.status === "error" ? (
        <div className="notice error" role="alert">{preguntaVista.message}</div>
      ) : null}
      {preguntaVista.status === "activa" ? (
        <PreguntaActivaView
          pregunta={preguntaVista.pregunta}
          preguntasConfig={preguntasConfig}
          posteando={posteando}
          puedeOperar={puedeOperar}
          onAvanzar={() => void onAvanzar()}
        />
      ) : null}
      {preguntaVista.status === "sin-pregunta" ? (
        <div className="stack" data-testid="sin-pregunta-activa">
          <p className="muted">Sin pregunta activa.</p>
          {puedeOperar ? (
            <button type="button" data-testid="btn-finalizar-juego" disabled={posteando} onClick={() => void onFinalizar()}>
              Finalizar juego
            </button>
          ) : null}
        </div>
      ) : null}
      <RankingView ranking={ranking} />
    </div>
  );
}

function PreguntaActivaView({
  pregunta,
  preguntasConfig,
  posteando,
  puedeOperar,
  onAvanzar
}: {
  pregunta: PreguntaActualDto;
  preguntasConfig: PreguntaDetail[];
  posteando: boolean;
  puedeOperar: boolean;
  onAvanzar: () => void;
}) {
  const cfg =
    preguntasConfig.find((p) => p.preguntaId === pregunta.preguntaId) ??
    preguntasConfig.find((p) => p.texto === pregunta.texto);
  const correcta = cfg?.opciones.find((o) => o.esCorrecta);
  // El opcionId de config coincide con el del runtime (verificado en vivo); el cruce por texto
  // es solo fallback cuando ningun opcionId matchea, para no marcar dos opciones de igual texto.
  const correctaPorId = !!correcta && pregunta.opciones.some((o) => o.opcionId === correcta.opcionId);
  const target = new Date(
    new Date(pregunta.fechaActivacion).getTime() + pregunta.tiempoLimiteSegundos * 1000
  ).toISOString();

  return (
    <div className="question-card" data-testid="pregunta-activa">
      <h3 className="q-title">
        Pregunta {pregunta.orden} — <span>{pregunta.texto}</span>
      </h3>
      <Countdown target={target} testid="pregunta-countdown" />
      <ul>
        {pregunta.opciones.map((opcion) => {
          const esCorrecta = correcta
            ? correctaPorId
              ? correcta.opcionId === opcion.opcionId
              : correcta.texto === opcion.texto
            : false;
          return (
            <li key={opcion.opcionId} data-testid={esCorrecta ? "opcion-correcta" : undefined}>
              <span>{opcion.texto}</span>
              {esCorrecta ? <span className="badge">Correcta</span> : null}
            </li>
          );
        })}
      </ul>
      {puedeOperar ? (
        <button type="button" data-testid="btn-avanzar-pregunta" disabled={posteando} onClick={onAvanzar}>
          Cerrar y avanzar
        </button>
      ) : null}
    </div>
  );
}

