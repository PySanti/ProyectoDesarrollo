// Consola de sesion del operador: lobby (inscritos + controles de inicio) + shell de sesion iniciada
// con runtime Trivia y BDT (etapas + pistas + mapa de ubicaciones). El consolidado llega en 2c-4.
import { useCallback, useEffect, useRef, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { getPartida, type ModoInicioPartida, type PartidaDetail } from "../../api/partidasApi";
import {
  getEstadoSesion,
  getLobby,
  iniciarPartida,
  OperacionesApiError,
  type EstadoSesionDto,
  type LobbyDto
} from "../../api/operacionesApi";
import { useSesionHub, type SesionHubHandlers } from "./useSesionHub";
import { useRankingHub } from "./useRankingHub";
import { TriviaRuntimePanel } from "./TriviaRuntimePanel";
import { BdtRuntimePanel } from "./BdtRuntimePanel";
import { PistasPanel } from "./PistasPanel";
import { GeoMapPanel, type UbicacionParticipante } from "./GeoMapPanel";
import { Countdown } from "./runtimeShared";
import { ConsolidadoPanel } from "./ConsolidadoPanel";
import type { RankingJuegoDto, RankingConsolidadoDto } from "../../api/puntuacionesApi";

interface Props {
  accessToken: string;
  puedeOperar: boolean;
}

type Vista =
  | { status: "loading" }
  | { status: "no-publicada" }
  | { status: "error"; message: string }
  | {
      status: "lobby";
      estado: EstadoSesionDto;
      lobby: LobbyDto;
      modoInicio: ModoInicioPartida;
      tiempoInicio: string | null;
    }
  | { status: "iniciada"; estado: EstadoSesionDto; config: PartidaDetail | null }
  | { status: "cancelada"; motivo?: string }
  | { status: "terminada" };

export function SesionOperadorPage({ accessToken, puedeOperar }: Props) {
  const { partidaId } = useParams<{ partidaId: string }>();
  const [vista, setVista] = useState<Vista>({ status: "loading" });
  const [iniciando, setIniciando] = useState(false);
  const [refetchSignal, setRefetchSignal] = useState(0);
  const [ubicaciones, setUbicaciones] = useState<Map<string, UbicacionParticipante>>(new Map());
  const [rankingPush, setRankingPush] = useState<RankingJuegoDto | null>(null);
  const [consolidadoPush, setConsolidadoPush] = useState<RankingConsolidadoDto | null>(null);
  const seqRef = useRef(0);

  useEffect(() => {
    setUbicaciones(new Map());
  }, [partidaId]);

  const cargar = useCallback(async () => {
    const my = ++seqRef.current;
    if (!partidaId) {
      if (my !== seqRef.current) return;
      setVista({ status: "error", message: "Partida no encontrada" });
      return;
    }
    try {
      const estado = await getEstadoSesion(partidaId, accessToken);
      if (estado.estado === "Iniciada") {
        const config = await getPartida(partidaId, accessToken).catch(() => null);
        if (my !== seqRef.current) return;
        setVista({ status: "iniciada", estado, config });
        return;
      }
      if (estado.estado === "Cancelada") {
        if (my !== seqRef.current) return;
        setVista({ status: "cancelada" });
        return;
      }
      if (estado.estado === "Terminada") {
        if (my !== seqRef.current) return;
        setVista({ status: "terminada" });
        return;
      }
      const [lobby, config] = await Promise.all([
        getLobby(partidaId, accessToken),
        getPartida(partidaId, accessToken)
      ]);
      if (my !== seqRef.current) return;
      setVista({
        status: "lobby",
        estado,
        lobby,
        modoInicio: config.modoInicioPartida,
        tiempoInicio: config.tiempoInicio
      });
    } catch (caught) {
      if (my !== seqRef.current) return;
      if (caught instanceof OperacionesApiError && caught.statusCode === 404) {
        setVista({ status: "no-publicada" });
        return;
      }
      setVista({
        status: "error",
        message: caught instanceof Error ? caught.message : "Error inesperado al consultar la sesión."
      });
    }
  }, [partidaId, accessToken]);

  useEffect(() => {
    void cargar();
  }, [cargar]);

  // Refetch de inscritos por intervalo (el hub no pushea inscripciones).
  const enLobby = vista.status === "lobby";
  useEffect(() => {
    if (!enLobby || !partidaId) return;
    const id = setInterval(() => {
      getLobby(partidaId, accessToken)
        .then((lobby) => setVista((v) => (v.status === "lobby" ? { ...v, lobby } : v)))
        .catch(() => {});
    }, 5000);
    return () => clearInterval(id);
  }, [enLobby, partidaId, accessToken]);

  const handlers: SesionHubHandlers = {
    onEnLobby: () => void cargar(),
    onIniciada: () => void cargar(),
    onCancelada: (p) => {
      seqRef.current++;
      setVista({ status: "cancelada", motivo: p.motivo });
    },
    onJuegoActivado: () => {
      setRefetchSignal((s) => s + 1);
      void cargar();
    },
    onFinalizada: () => {
      seqRef.current++;
      setVista({ status: "terminada" });
    },
    onPreguntaActivada: () => setRefetchSignal((s) => s + 1),
    onPreguntaCerrada: () => setRefetchSignal((s) => s + 1),
    onEtapaActivada: () => setRefetchSignal((s) => s + 1),
    onEtapaCerrada: () => setRefetchSignal((s) => s + 1),
    onEtapaGanada: () => setRefetchSignal((s) => s + 1),
    onUbicacionActualizada: (p) =>
      setUbicaciones((prev) => new Map(prev).set(p.participanteId, p))
  };
  useSesionHub(partidaId ?? "", accessToken, handlers);
  useRankingHub(partidaId ?? "", accessToken, {
    onRankingJuego: setRankingPush,
    onConsolidado: setConsolidadoPush
  });

  async function onIniciar() {
    if (!partidaId) return;
    setIniciando(true);
    try {
      const r = await iniciarPartida(partidaId, accessToken);
      if (r.estado === "Cancelada") {
        seqRef.current++;
        setVista({ status: "cancelada" });
      } else {
        await cargar();
      }
    } catch {
      await cargar();
    } finally {
      setIniciando(false);
    }
  }

  return (
    <div className="page" data-testid="sesion-operador">
      {renderVista(vista, {
        partidaId: partidaId ?? "",
        accessToken,
        puedeOperar,
        iniciando,
        onIniciar,
        onActualizar: () => void cargar(),
        refetchSignal,
        onTerminada: () => {
          seqRef.current++;
          setVista({ status: "terminada" });
        },
        onJuegoAvanzado: () => void cargar(),
        ubicaciones: Array.from(ubicaciones.values()),
        rankingPush,
        consolidadoPush
      })}
    </div>
  );
}

interface VistaCtx {
  partidaId: string;
  accessToken: string;
  puedeOperar: boolean;
  iniciando: boolean;
  onIniciar: () => void;
  onActualizar: () => void;
  refetchSignal: number;
  onTerminada: () => void;
  onJuegoAvanzado: () => void;
  ubicaciones: UbicacionParticipante[];
  rankingPush: RankingJuegoDto | null;
  consolidadoPush: RankingConsolidadoDto | null;
}

function renderVista(vista: Vista, ctx: VistaCtx) {
  switch (vista.status) {
    case "loading":
      return <p className="muted">Cargando sesión…</p>;
    case "no-publicada":
      return (
        <div className="card stack" data-testid="sesion-no-publicada">
          <p>La partida no está publicada.</p>
          <Link to={`/partidas/${ctx.partidaId}`} className="row-link">
            Ir al detalle para publicar
          </Link>
        </div>
      );
    case "error":
      return (
        <div className="notice error" role="alert">
          {vista.message}
        </div>
      );
    case "lobby":
      return <LobbyView vista={vista} ctx={ctx} />;
    case "iniciada":
      return (
        <IniciadaView
          estado={vista.estado}
          config={vista.config}
          accessToken={ctx.accessToken}
          partidaId={ctx.partidaId}
          puedeOperar={ctx.puedeOperar}
          refetchSignal={ctx.refetchSignal}
          onTerminada={ctx.onTerminada}
          onJuegoAvanzado={ctx.onJuegoAvanzado}
          ubicaciones={ctx.ubicaciones}
          rankingPush={ctx.rankingPush}
        />
      );
    case "cancelada":
      return (
        <div className="card stack" data-testid="sesion-cancelada">
          <p>La partida fue cancelada (mínimos de participación no alcanzados).</p>
          {vista.motivo ? <p className="muted">{vista.motivo}</p> : null}
        </div>
      );
    case "terminada":
      return (
        <div className="stack">
          <ConsolidadoPanel
            partidaId={ctx.partidaId}
            accessToken={ctx.accessToken}
            consolidadoPush={ctx.consolidadoPush}
          />
          <Link to={`/partidas/${ctx.partidaId}/historial`} className="row-link">
            Ver historial de la partida
          </Link>
        </div>
      );
    default:
      return null;
  }
}

function LobbyView({
  vista,
  ctx
}: {
  vista: Extract<Vista, { status: "lobby" }>;
  ctx: VistaCtx;
}) {
  const { lobby, modoInicio, tiempoInicio } = vista;
  const mostrarManual = modoInicio === "Manual" || modoInicio === "ManualYAutomatico";
  const mostrarAutomatico = modoInicio === "Automatico" || modoInicio === "ManualYAutomatico";

  return (
    <div className="card stack" data-testid="lobby-panel">
      <header className="create-head">
        <div>
          <h1>Lobby de la partida</h1>
          <div className="compact-actions">
            <span className="pill pill--lobby">
              <span className="pill__dot" />
              {lobby.modalidad}
            </span>
            <span className="muted">
              Min {lobby.minimosParticipacion} · Max {lobby.maximosParticipacion}
            </span>
          </div>
        </div>
        <button type="button" className="secondary-button" data-testid="btn-actualizar-lobby" onClick={ctx.onActualizar}>
          Actualizar
        </button>
      </header>

      <p data-testid="lobby-inscritos">
        Inscritos: {lobby.inscritosActivos} / min {lobby.minimosParticipacion}
      </p>

      {lobby.modalidad === "Equipo" && lobby.equipos.length > 0 ? (
        <div className="table-wrap">
          <table aria-label="Equipos convocados">
            <thead>
              <tr>
                <th scope="col">Equipo</th>
                <th scope="col">Convocados</th>
                <th scope="col">Aceptados</th>
              </tr>
            </thead>
            <tbody>
              {lobby.equipos.map((equipo) => (
                <tr key={equipo.equipoId}>
                  <td>{equipo.equipoId}</td>
                  <td>{equipo.convocados}</td>
                  <td>{equipo.aceptados}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}

      <div className="compact-actions">
        {mostrarManual && ctx.puedeOperar ? (
          <button type="button" data-testid="btn-iniciar" disabled={ctx.iniciando} onClick={ctx.onIniciar}>
            Iniciar ahora
          </button>
        ) : null}
        {mostrarAutomatico ? (
          tiempoInicio ? (
            <Countdown target={tiempoInicio} testid="inicio-countdown" expiredLabel="Iniciando…" muted={false} />
          ) : (
            <span className="muted">Inicio automático pendiente de configuración</span>
          )
        ) : null}
      </div>
    </div>
  );
}

function pillClaseParaEstado(estadoJuego: string): string {
  if (estadoJuego === "Activo") return "pill--live";
  if (estadoJuego === "Finalizado") return "pill--done";
  return "pill--lobby";
}

interface IniciadaViewProps {
  estado: EstadoSesionDto;
  config: PartidaDetail | null;
  accessToken: string;
  partidaId: string;
  puedeOperar: boolean;
  refetchSignal: number;
  onTerminada: () => void;
  onJuegoAvanzado: () => void;
  ubicaciones: UbicacionParticipante[];
  rankingPush: RankingJuegoDto | null;
}

function IniciadaView({
  estado,
  config,
  accessToken,
  partidaId,
  puedeOperar,
  refetchSignal,
  onTerminada,
  onJuegoAvanzado,
  ubicaciones,
  rankingPush
}: IniciadaViewProps) {
  const juegos = [...estado.juegos].sort((a, b) => a.orden - b.orden);
  const juegoActual = juegos.find((j) => j.orden === estado.juegoActualOrden);
  return (
    <div className="card stack" data-testid="sesion-iniciada">
      <h1>Sesión en curso</h1>
      <div className="question-list">
        {juegos.map((juego) => {
          const esActual = juego.orden === estado.juegoActualOrden;
          return (
            <div
              key={juego.juegoId}
              className={`pill ${pillClaseParaEstado(juego.estado)}`}
              data-testid={esActual ? "juego-actual" : undefined}
            >
              <span className="pill__dot" />
              Juego {juego.orden} — {juego.tipoJuego}
            </div>
          );
        })}
      </div>
      {juegoActual?.tipoJuego === "Trivia" ? (
        <TriviaRuntimePanel
          key={juegoActual.juegoId}
          partidaId={partidaId}
          juegoId={juegoActual.juegoId}
          accessToken={accessToken}
          puedeOperar={puedeOperar}
          preguntasConfig={config?.juegos.find((j) => j.orden === estado.juegoActualOrden)?.trivia?.preguntas ?? []}
          refetchSignal={refetchSignal}
          onTerminada={onTerminada}
          onJuegoAvanzado={onJuegoAvanzado}
          rankingPush={rankingPush}
        />
      ) : null}
      {juegoActual?.tipoJuego === "BusquedaDelTesoro" ? (
        <div className="stack" key={juegoActual.juegoId}>
          <BdtRuntimePanel
            partidaId={partidaId}
            juegoId={juegoActual.juegoId}
            accessToken={accessToken}
            puedeOperar={puedeOperar}
            refetchSignal={refetchSignal}
            onTerminada={onTerminada}
            onJuegoAvanzado={onJuegoAvanzado}
            rankingPush={rankingPush}
          />
          {puedeOperar ? <PistasPanel partidaId={partidaId} accessToken={accessToken} /> : null}
          <GeoMapPanel ubicaciones={ubicaciones} />
        </div>
      ) : null}
    </div>
  );
}

