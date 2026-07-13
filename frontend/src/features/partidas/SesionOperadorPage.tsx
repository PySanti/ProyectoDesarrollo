// Consola de sesion del operador: lobby (inscritos + controles de inicio) + shell de sesion iniciada
// con runtime Trivia y BDT (etapas + pistas + mapa de ubicaciones). El consolidado llega en 2c-4.
import { useCallback, useEffect, useRef, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { getPartida, type ModoInicioPartida, type PartidaDetail } from "../../api/partidasApi";
import {
  aceptarInscripcion,
  cancelarPartida,
  getEstadoSesion,
  getLobby,
  iniciarPartida,
  rechazarInscripcion,
  OperacionesApiError,
  type EstadoSesionDto,
  type LobbyDto
} from "../../api/operacionesApi";
import { useSesionHub, type SesionHubHandlers } from "./useSesionHub";
import { useRankingHub } from "./useRankingHub";
import { TriviaRuntimePanel } from "./TriviaRuntimePanel";
import { BdtRuntimePanel, type EtapaResultadoDto } from "./BdtRuntimePanel";
import { PistasPanel } from "./PistasPanel";
import { EnviosTesoroPanel } from "./EnviosTesoroPanel";
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
  const [confirmandoCancelacion, setConfirmandoCancelacion] = useState(false);
  const [cancelando, setCancelando] = useState(false);
  const [refetchSignal, setRefetchSignal] = useState(0);
  const [ubicaciones, setUbicaciones] = useState<Map<string, UbicacionParticipante>>(new Map());
  const [resultadosEtapas, setResultadosEtapas] = useState<Map<string, EtapaResultadoDto>>(new Map());
  const [rankingPush, setRankingPush] = useState<RankingJuegoDto | null>(null);
  const [consolidadoPush, setConsolidadoPush] = useState<RankingConsolidadoDto | null>(null);
  const seqRef = useRef(0);

  useEffect(() => {
    setUbicaciones(new Map());
    setResultadosEtapas(new Map());
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

  // Si la vista cambia (ej. push del hub lobby->iniciada), una confirmacion armada
  // en la vista anterior no debe sobrevivir: se resetea la garantia de 2 clics.
  useEffect(() => {
    setConfirmandoCancelacion(false);
  }, [vista.status]);

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

  // HU-35: EtapaCerrada (con o sin ganador) y EtapaGanada aportan el mismo resultado de cierre;
  // se acumula por etapaId (idempotente si llegan ambos eventos para la misma etapa).
  const registrarResultadoEtapa = (p: {
    juegoId: string;
    etapaId: string;
    ganadorParticipanteId?: string;
    ganadorEquipoId?: string;
  }) => {
    setResultadosEtapas((prev) =>
      new Map(prev).set(p.etapaId, {
        etapaId: p.etapaId,
        juegoId: p.juegoId,
        ganadorParticipanteId: p.ganadorParticipanteId,
        ganadorEquipoId: p.ganadorEquipoId
      })
    );
  };

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
    onEtapaCerrada: (p) => {
      setRefetchSignal((s) => s + 1);
      registrarResultadoEtapa(p);
    },
    onEtapaGanada: (p) => {
      setRefetchSignal((s) => s + 1);
      registrarResultadoEtapa(p);
    },
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

  async function onAceptarSolicitud(inscripcionId: string) {
    if (!partidaId) return;
    try {
      const lobbyActualizado = await aceptarInscripcion(partidaId, inscripcionId, accessToken);
      setVista((v) => (v.status === "lobby" ? { ...v, lobby: lobbyActualizado } : v));
    } catch {
      await cargar();
    }
  }

  async function onRechazarSolicitud(inscripcionId: string) {
    if (!partidaId) return;
    try {
      const lobbyActualizado = await rechazarInscripcion(partidaId, inscripcionId, accessToken);
      setVista((v) => (v.status === "lobby" ? { ...v, lobby: lobbyActualizado } : v));
    } catch {
      await cargar();
    }
  }

  function onCancelarPartida() {
    setConfirmandoCancelacion(true);
  }

  async function onConfirmarCancelacionPartida() {
    if (!partidaId) return;
    setCancelando(true);
    try {
      await cancelarPartida(partidaId, accessToken);
      await cargar(); // el push PartidaCancelada tambien llega por el hub; cargar() asegura la transicion inmediata
    } catch {
      await cargar();
    } finally {
      setConfirmandoCancelacion(false);
      setCancelando(false);
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
        onAceptarSolicitud: (inscripcionId) => void onAceptarSolicitud(inscripcionId),
        onRechazarSolicitud: (inscripcionId) => void onRechazarSolicitud(inscripcionId),
        confirmandoCancelacion,
        cancelando,
        onCancelarPartida,
        onConfirmarCancelacionPartida: () => void onConfirmarCancelacionPartida(),
        refetchSignal,
        onTerminada: () => {
          seqRef.current++;
          setVista({ status: "terminada" });
        },
        onJuegoAvanzado: () => void cargar(),
        ubicaciones: Array.from(ubicaciones.values()),
        resultadosEtapas: Array.from(resultadosEtapas.values()),
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
  onAceptarSolicitud: (inscripcionId: string) => void;
  onRechazarSolicitud: (inscripcionId: string) => void;
  confirmandoCancelacion: boolean;
  cancelando: boolean;
  onCancelarPartida: () => void;
  onConfirmarCancelacionPartida: () => void;
  refetchSignal: number;
  onTerminada: () => void;
  onJuegoAvanzado: () => void;
  ubicaciones: UbicacionParticipante[];
  resultadosEtapas: EtapaResultadoDto[];
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
          resultadosEtapas={ctx.resultadosEtapas}
          rankingPush={ctx.rankingPush}
          confirmandoCancelacion={ctx.confirmandoCancelacion}
          cancelando={ctx.cancelando}
          onCancelarPartida={ctx.onCancelarPartida}
          onConfirmarCancelacionPartida={ctx.onConfirmarCancelacionPartida}
        />
      );
    case "cancelada": {
      const motivoTexto =
        vista.motivo === "MinimosNoAlcanzados"
          ? "Mínimos de participación no alcanzados."
          : vista.motivo === "CanceladaPorOperador"
            ? "Cancelada por el operador."
            : null;
      return (
        <div className="card stack" data-testid="sesion-cancelada">
          <p>La partida fue cancelada.</p>
          {motivoTexto ? <p className="muted">{motivoTexto}</p> : null}
        </div>
      );
    }
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

interface CancelarPartidaActionProps {
  puedeOperar: boolean;
  confirmandoCancelacion: boolean;
  cancelando: boolean;
  onCancelarPartida: () => void;
  onConfirmarCancelacionPartida: () => void;
}

function CancelarPartidaAction({
  puedeOperar,
  confirmandoCancelacion,
  cancelando,
  onCancelarPartida,
  onConfirmarCancelacionPartida
}: CancelarPartidaActionProps) {
  if (!puedeOperar) return null;
  if (confirmandoCancelacion) {
    return (
      <button
        type="button"
        data-testid="btn-cancelar-partida-confirm"
        disabled={cancelando}
        onClick={onConfirmarCancelacionPartida}
      >
        Confirmar cancelación
      </button>
    );
  }
  return (
    <button type="button" className="secondary-button" data-testid="btn-cancelar-partida" onClick={onCancelarPartida}>
      Cancelar partida
    </button>
  );
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
        <div className="compact-actions">
          <button type="button" className="secondary-button" data-testid="btn-actualizar-lobby" onClick={ctx.onActualizar}>
            Actualizar
          </button>
          <CancelarPartidaAction
            puedeOperar={ctx.puedeOperar}
            confirmandoCancelacion={ctx.confirmandoCancelacion}
            cancelando={ctx.cancelando}
            onCancelarPartida={ctx.onCancelarPartida}
            onConfirmarCancelacionPartida={ctx.onConfirmarCancelacionPartida}
          />
        </div>
      </header>

      <p data-testid="lobby-inscritos">
        Inscritos: {lobby.inscritosActivos} / min {lobby.minimosParticipacion}
      </p>

      {lobby.modalidad === "Individual" && lobby.participantes.length > 0 ? (
        <div className="table-wrap" data-testid="lobby-participantes">
          <table aria-label="Participantes inscritos">
            <thead>
              <tr>
                <th scope="col">Participante</th>
              </tr>
            </thead>
            <tbody>
              {lobby.participantes.map((participanteId) => (
                <tr key={participanteId}>
                  <td>{participanteId}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}

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

      {lobby.solicitudesPendientesIndividual.length > 0 || lobby.solicitudesPendientesEquipo.length > 0 ? (
        <div className="table-wrap" data-testid="solicitudes-panel">
          <h2>Solicitudes pendientes</h2>
          <table aria-label="Solicitudes de inscripción pendientes">
            <thead>
              <tr>
                <th scope="col">{lobby.modalidad === "Equipo" ? "Equipo" : "Participante"}</th>
                <th scope="col">Fecha</th>
                {ctx.puedeOperar ? <th scope="col">Acciones</th> : null}
              </tr>
            </thead>
            <tbody>
              {lobby.solicitudesPendientesIndividual.map((s) => (
                <tr key={s.inscripcionId}>
                  <td>{s.participanteId}</td>
                  <td>{new Date(s.fechaInscripcion).toLocaleString()}</td>
                  {ctx.puedeOperar ? (
                    <td className="compact-actions">
                      <button type="button" data-testid="btn-aceptar-solicitud" onClick={() => ctx.onAceptarSolicitud(s.inscripcionId)}>
                        Aceptar
                      </button>
                      <button type="button" className="secondary-button" data-testid="btn-rechazar-solicitud" onClick={() => ctx.onRechazarSolicitud(s.inscripcionId)}>
                        Rechazar
                      </button>
                    </td>
                  ) : null}
                </tr>
              ))}
              {lobby.solicitudesPendientesEquipo.map((s) => (
                <tr key={s.inscripcionId}>
                  <td>{s.equipoId} ({s.miembros} miembros)</td>
                  <td>{new Date(s.fechaInscripcion).toLocaleString()}</td>
                  {ctx.puedeOperar ? (
                    <td className="compact-actions">
                      <button type="button" data-testid="btn-aceptar-solicitud" onClick={() => ctx.onAceptarSolicitud(s.inscripcionId)}>
                        Aceptar
                      </button>
                      <button type="button" className="secondary-button" data-testid="btn-rechazar-solicitud" onClick={() => ctx.onRechazarSolicitud(s.inscripcionId)}>
                        Rechazar
                      </button>
                    </td>
                  ) : null}
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
  resultadosEtapas: EtapaResultadoDto[];
  rankingPush: RankingJuegoDto | null;
  confirmandoCancelacion: boolean;
  cancelando: boolean;
  onCancelarPartida: () => void;
  onConfirmarCancelacionPartida: () => void;
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
  resultadosEtapas,
  rankingPush,
  confirmandoCancelacion,
  cancelando,
  onCancelarPartida,
  onConfirmarCancelacionPartida
}: IniciadaViewProps) {
  const juegos = [...estado.juegos].sort((a, b) => a.orden - b.orden);
  const juegoActual = juegos.find((j) => j.orden === estado.juegoActualOrden);
  return (
    <div className="card stack" data-testid="sesion-iniciada">
      <header className="create-head">
        <h1>Sesión en curso</h1>
        <CancelarPartidaAction
          puedeOperar={puedeOperar}
          confirmandoCancelacion={confirmandoCancelacion}
          cancelando={cancelando}
          onCancelarPartida={onCancelarPartida}
          onConfirmarCancelacionPartida={onConfirmarCancelacionPartida}
        />
      </header>
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
            resultadosEtapas={resultadosEtapas}
          />
          {puedeOperar ? <PistasPanel partidaId={partidaId} accessToken={accessToken} /> : null}
          <EnviosTesoroPanel partidaId={partidaId} accessToken={accessToken} refetchSignal={refetchSignal} />
          <GeoMapPanel ubicaciones={ubicaciones} />
        </div>
      ) : null}
    </div>
  );
}

