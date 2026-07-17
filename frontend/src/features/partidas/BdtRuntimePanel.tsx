// Runtime BDT del operador: etapa activa + avance + finalizar + ranking del juego.
import { useCallback, useEffect, useState } from "react";
import {
  avanzarEtapa,
  finalizarJuegoActual,
  getEtapaActual,
  OperacionesApiError,
  type EtapaActualDto
} from "../../api/operacionesApi";
import { getRankingJuego, type RankingJuegoDto } from "../../api/puntuacionesApi";
import { Countdown, RankingView } from "./runtimeShared";
import { idsDeCompetidores, useNombres } from "../shared/useNombres";

export interface EtapaResultadoDto {
  etapaId: string;
  juegoId: string;
  ganadorParticipanteId?: string;
  ganadorEquipoId?: string;
}

export interface BdtRuntimePanelProps {
  partidaId: string;
  juegoId: string;
  accessToken: string;
  puedeOperar: boolean;
  refetchSignal: number;
  onTerminada: () => void;
  onJuegoAvanzado: () => void;
  rankingPush?: RankingJuegoDto | null;
  resultadosEtapas?: EtapaResultadoDto[];
}

type EtapaVista =
  | { status: "cargando" }
  | { status: "activa"; etapa: EtapaActualDto }
  | { status: "sin-etapa" }
  | { status: "error"; message: string };

export function BdtRuntimePanel(props: BdtRuntimePanelProps) {
  const {
    partidaId,
    juegoId,
    accessToken,
    puedeOperar,
    refetchSignal,
    onTerminada,
    onJuegoAvanzado,
    rankingPush,
    resultadosEtapas
  } = props;
  const [etapaVista, setEtapaVista] = useState<EtapaVista>({ status: "cargando" });
  const [ranking, setRanking] = useState<RankingJuegoDto | null>(null);
  const [posteando, setPosteando] = useState(false);
  const [tick, setTick] = useState(0); // refetch interno tras avance/finalizar fallido
  const nombreDe = useNombres(idsDeCompetidores(ranking?.entradas ?? []), accessToken);

  const refetch = useCallback(() => setTick((t) => t + 1), []);

  useEffect(() => {
    let active = true;
    getEtapaActual(partidaId, accessToken)
      .then((etapa) => {
        if (active) setEtapaVista({ status: "activa", etapa });
      })
      .catch((caught) => {
        if (!active) return;
        if (caught instanceof OperacionesApiError && caught.statusCode === 409) {
          setEtapaVista({ status: "sin-etapa" });
        } else {
          setEtapaVista({
            status: "error",
            message: caught instanceof Error ? caught.message : "Error al consultar la etapa."
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
      await avanzarEtapa(partidaId, accessToken);
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
    <div className="stack" data-testid="bdt-runtime">
      {etapaVista.status === "cargando" ? <p className="muted">Cargando etapa…</p> : null}
      {etapaVista.status === "error" ? (
        <div className="notice error" role="alert">{etapaVista.message}</div>
      ) : null}
      {etapaVista.status === "activa" ? (
        <EtapaActivaView
          etapa={etapaVista.etapa}
          posteando={posteando}
          puedeOperar={puedeOperar}
          onAvanzar={() => void onAvanzar()}
        />
      ) : null}
      {etapaVista.status === "sin-etapa" ? (
        <div className="stack" data-testid="sin-etapa-activa">
          <p className="muted">Sin etapa activa.</p>
          {puedeOperar ? (
            <button type="button" data-testid="btn-finalizar-juego" disabled={posteando} onClick={() => void onFinalizar()}>
              Finalizar juego
            </button>
          ) : null}
        </div>
      ) : null}
      <ResultadoPorEtapa resultadosEtapas={resultadosEtapas} juegoId={juegoId} />
      <RankingView ranking={ranking} nombreDe={nombreDe} />
    </div>
  );
}

// HU-35: historial simple de cierres de etapa (EtapaCerrada/EtapaGanada), acumulado en la pagina
// padre y filtrado aqui por juego (el mismo etapaId nunca se repite entre juegos).
function ResultadoPorEtapa({
  resultadosEtapas,
  juegoId
}: {
  resultadosEtapas?: EtapaResultadoDto[];
  juegoId: string;
}) {
  const delJuego = (resultadosEtapas ?? []).filter((r) => r.juegoId === juegoId);
  if (delJuego.length === 0) return null;
  return (
    <div className="stack">
      <h3 className="q-title">Resultado por etapa</h3>
      {delJuego.map((r) => {
        const ganador = r.ganadorEquipoId ?? r.ganadorParticipanteId;
        return (
          <p key={r.etapaId} data-testid="resultado-etapa">
            {ganador ? `Ganada por ${ganador}` : "Nadie consiguió el tesoro"}
          </p>
        );
      })}
    </div>
  );
}

function EtapaActivaView({
  etapa,
  posteando,
  puedeOperar,
  onAvanzar
}: {
  etapa: EtapaActualDto;
  posteando: boolean;
  puedeOperar: boolean;
  onAvanzar: () => void;
}) {
  const target = new Date(
    new Date(etapa.fechaActivacion).getTime() + etapa.tiempoLimiteSegundos * 1000
  ).toISOString();

  return (
    <div className="question-card" data-testid="etapa-activa">
      <h3 className="q-title">
        Etapa {etapa.orden} — <span>{etapa.areaBusqueda}</span>
      </h3>
      <Countdown target={target} testid="etapa-countdown" />
      {puedeOperar ? (
        <button type="button" data-testid="btn-avanzar-etapa" disabled={posteando} onClick={onAvanzar}>
          Cerrar y avanzar
        </button>
      ) : null}
    </div>
  );
}

