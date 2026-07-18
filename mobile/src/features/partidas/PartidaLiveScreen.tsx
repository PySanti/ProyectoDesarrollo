// Pantalla live del participante: fases por mi-sesion + hub de sesion; monta el panel del juego activo.
import React, { useCallback, useEffect, useRef, useState } from "react";
import { ActivityIndicator, ScrollView, StyleSheet, View } from "react-native";
import { AppText, Button, Card, Notice, ScreenHeader } from "../../shared/ui";
import { colors, spacing } from "../../shared/theme";
import { cargarLive, debeRecargarLive } from "./partidaLiveFlow.js";
import { getRankingConsolidado } from "./gameplayApi.js";
import { crearSesionHub, reengancharAlReconectar } from "./sesionHub.js";
import { crearRankingHub } from "./rankingHub.js";
import { TriviaPlayPanel } from "./TriviaPlayPanel";
import { BdtPlayPanel, type Pista } from "./BdtPlayPanel";
import { requestBdtGeolocationPermission } from "../../permissions/bdtGeolocationPermission.js";
import { RankingTable, type RankingEntrada } from "./liveShared";
import { idsDeCompetidores, motivosDesempateConsolidado } from "./liveLabels.js";
import { useNombres } from "../shared/useNombres.js";

type JuegoActivo = { juegoId: string; orden: number; tipoJuego: string; estadoJuego: string };

type LiveResult =
  | { ok: true; fase: "sin-participacion" }
  | { ok: true; fase: "lobby" }
  | { ok: true; fase: "iniciada"; juegoActivo: JuegoActivo | null; yaRespondio: boolean }
  | { ok: false; type: string; message?: string };

type ConsolidadoEntrada = {
  posicion: number;
  competidorId: string;
  juegosGanados: number;
  puntosTotales: number;
  tiempoTotalMs: number;
  tipoCompetidor?: "Participante" | "Equipo";
};
type ConsolidadoResult =
  | { ok: true; ranking: { entradas: ConsolidadoEntrada[] } }
  | { ok: false; type: string; message?: string };

type Fase =
  | { status: "cargando" }
  | { status: "sin-participacion" }
  | { status: "iniciada"; juegoActivo: JuegoActivo | null; yaRespondio: boolean }
  | { status: "finalizada" }
  | { status: "cancelada"; motivo?: string };

type Props = {
  apiBaseUrl: string;
  token: string;
  partidaId: string;
  nombre: string;
  miSub: string;
  onVolverAlPanel: () => void;
  onVolverAlMenu: () => void;
};

export function PartidaLiveScreen({ apiBaseUrl, token, partidaId, nombre, miSub, onVolverAlPanel, onVolverAlMenu }: Props) {
  const [fase, setFase] = useState<Fase>({ status: "cargando" });
  const [refetchSignal, setRefetchSignal] = useState(0);
  const [resetSignal, setResetSignal] = useState(0);
  const [aviso, setAviso] = useState<string | null>(null);
  const [consolidado, setConsolidado] = useState<ConsolidadoEntrada[] | null>(null);
  const nombreDeConsolidado = useNombres(idsDeCompetidores(consolidado ?? []), apiBaseUrl, token);
  const [pistas, setPistas] = useState<Pista[]>([]);
  const [avisoGeo, setAvisoGeo] = useState<string | null>(null);
  const [rankingPush, setRankingPush] = useState<{ juegoId: string; entradas: RankingEntrada[] } | null>(null);
  // HU-24/BR-T06: última PreguntaCerrada, para mostrar la respuesta correcta en el panel Trivia.
  // juegoId viaja en el payload (7d review fix): el panel descarta cierres de un juego que ya no
  // es el activo, porque este estado vive a nivel de partida y no se limpia al cambiar de juego.
  const [preguntaCerrada, setPreguntaCerrada] = useState<{ texto: string | null; juegoId: string } | null>(null);
  // Equipo: última RespuestaEquipoRegistrada (la respuesta de un miembro sella al equipo entero).
  const [respuestaEquipo, setRespuestaEquipo] =
    useState<{ juegoId: string; preguntaId: string; esCorrecta: boolean } | null>(null);
  const hubRef = useRef<ReturnType<typeof crearSesionHub> | null>(null);
  // El token va por ref: un refresh de sesión (RNF-24) no debe derribar la conexión viva
  // (solo se usa en el handshake de conexión/reconexión).
  const tokenRef = useRef(token);
  tokenRef.current = token;

  // fase por ref para leerla dentro de load sin re-crear el callback: si ya estamos en una fase
  // terminal (finalizada/cancelada) no recargamos, o un refresh de token pisaría el ranking final
  // con "sin-participacion" (a cada teléfono le pasaba a distinta hora).
  const faseRef = useRef(fase);
  faseRef.current = fase;

  const load = useCallback(async () => {
    if (!debeRecargarLive(faseRef.current.status)) return;
    const r = (await cargarLive({ apiBaseUrl, token, partidaId, fetchImpl: undefined })) as LiveResult;
    if (!r.ok) {
      setAviso(r.message ?? "No se pudo cargar la sesión.");
      return;
    }
    if (r.fase === "iniciada") {
      setFase({ status: "iniciada", juegoActivo: r.juegoActivo, yaRespondio: r.yaRespondio });
    } else {
      // "lobby" en el live = la partida aún no inició; tratamos igual que sin-participacion:
      // el usuario llegó antes de tiempo, que vuelva por el flujo normal.
      setFase({ status: "sin-participacion" });
    }
  }, [apiBaseUrl, token, partidaId]);

  useEffect(() => {
    void load();
  }, [load]);

  const cargarConsolidado = useCallback(async () => {
    // Si aún no está calculado, se queda en null y llega solo por el push RankingConsolidadoCalculado.
    const r = (await getRankingConsolidado(apiBaseUrl, token, partidaId, undefined)) as ConsolidadoResult;
    if (r.ok) setConsolidado(r.ranking.entradas ?? []);
  }, [apiBaseUrl, token, partidaId]);

  // Hub: señales de pregunta → panel; transiciones → recargar/fases terminales.
  const loadRef = useRef(load);
  loadRef.current = load;
  const cargarConsolidadoRef = useRef(cargarConsolidado);
  cargarConsolidadoRef.current = cargarConsolidado;
  useEffect(() => {
    const hub = crearSesionHub(apiBaseUrl, () => tokenRef.current);
    hubRef.current = hub;
    hub.on("PreguntaActivada", () => {
      setResetSignal((s) => s + 1);
      setRefetchSignal((s) => s + 1);
      // La pregunta avanzó: el sello de "respuesta de equipo" es de la pregunta anterior. Se limpia
      // para que un evento que llegue pegado al avance no reviva el sello sobre la pregunta nueva
      // (equipo que falla justo cuando otro acierta).
      setRespuestaEquipo(null);
    });
    hub.on("PreguntaCerrada", (p: { juegoId?: string; textoOpcionCorrecta?: string | null }) => {
      setRefetchSignal((s) => s + 1);
      setPreguntaCerrada({ texto: p?.textoOpcionCorrecta ?? null, juegoId: p?.juegoId ?? "" });
    });
    // Equipo: un miembro respondió y eso sella al equipo entero. Se baja al panel para que el
    // resto vea el mismo resultado ("Incorrecta." / "¡Correcta!") sin tener que tocar nada.
    hub.on("RespuestaEquipoRegistrada", (p: { juegoId?: string; preguntaId?: string; esCorrecta?: boolean }) => {
      if (!p?.juegoId || !p?.preguntaId) return;
      setRespuestaEquipo({ juegoId: p.juegoId, preguntaId: p.preguntaId, esCorrecta: p.esCorrecta === true });
    });
    hub.on("EtapaActivada", () => {
      setResetSignal((s) => s + 1);
      setRefetchSignal((s) => s + 1);
    });
    hub.on("EtapaCerrada", () => setRefetchSignal((s) => s + 1));
    hub.on("EtapaGanada", () => setRefetchSignal((s) => s + 1));
    hub.on("PistaEnviada", (p: { texto?: string; timestampUtc?: string }) => {
      if (p?.texto) {
        setPistas((prev) => [{ texto: p.texto as string, timestampUtc: p.timestampUtc ?? "" }, ...prev]);
      }
    });
    hub.on("JuegoActivado", () => {
      setPistas([]);
      void loadRef.current();
    });
    hub.on("PartidaFinalizada", () => {
      setFase({ status: "finalizada" });
      void cargarConsolidadoRef.current();
    });
    hub.on("PartidaCancelada", (p: { motivo?: string }) =>
      setFase({ status: "cancelada", motivo: p?.motivo })
    );
    reengancharAlReconectar(hub, partidaId);
    // La reconexión es indefinida (reconexion.js): mientras reintenta, avisar en vez de
    // dejar la pantalla congelada; al volver, limpiar el aviso.
    hub.onreconnecting(() => setAviso("Reconectando…"));
    hub.onreconnected(() => setAviso(null));
    hub
      .start()
      .then(() => hub.invoke("SuscribirAPartida", partidaId))
      .catch(() => {});
    return () => {
      hubRef.current = null;
      void hub.stop().catch(() => {});
    };
  }, [apiBaseUrl, partidaId]);

  // Hub de rankings (SP-4c): push aditivo; el GET existente sigue siendo la fuente recuperable.
  useEffect(() => {
    const hub = crearRankingHub(apiBaseUrl, () => tokenRef.current);
    const aplicarRankingJuego = (p: { juegoId?: string; entradas?: unknown[] }) => {
      if (p?.juegoId && Array.isArray(p.entradas)) {
        setRankingPush({ juegoId: p.juegoId, entradas: p.entradas as never });
      }
    };
    hub.on("RankingTriviaActualizado", aplicarRankingJuego);
    hub.on("RankingBDTActualizado", aplicarRankingJuego);
    hub.on("RankingConsolidadoCalculado", (p: { entradas?: ConsolidadoEntrada[] }) => {
      if (Array.isArray(p?.entradas)) {
        setConsolidado(p.entradas);
      }
    });
    hub
      .start()
      .then(() => hub.invoke("SuscribirAPartida", partidaId))
      .catch(() => {});
    return () => {
      void hub.stop().catch(() => {});
    };
  }, [apiBaseUrl, partidaId]);

  const esBdtActivo = fase.status === "iniciada" && fase.juegoActivo?.tipoJuego === "BusquedaDelTesoro";
  useEffect(() => {
    if (!esBdtActivo) return;
    let cancelado = false;
    let watcher: { remove: () => void } | null = null;
    (async () => {
      const permiso = (await requestBdtGeolocationPermission()) as { granted: boolean; unavailable: boolean };
      if (cancelado) return;
      if (!permiso.granted) {
        setAvisoGeo("La geolocalización es obligatoria en Búsqueda del Tesoro. Actívala para continuar.");
        return;
      }
      setAvisoGeo(null);
      const Location = await import("expo-location");
      if (cancelado) return;
      watcher = await Location.watchPositionAsync(
        { accuracy: Location.Accuracy.Balanced, timeInterval: 2000, distanceInterval: 0 },
        (pos) => {
          void hubRef.current?.invoke("EnviarUbicacion", pos.coords.latitude, pos.coords.longitude).catch(() => {});
        },
      );
      if (cancelado) watcher.remove();
    })().catch(() => setAvisoGeo("No se pudo iniciar la geolocalización."));
    return () => {
      cancelado = true;
      watcher?.remove();
      setAvisoGeo(null);
    };
  }, [esBdtActivo]);

  const motivosConsolidado: Record<string, string> = consolidado ? motivosDesempateConsolidado(consolidado) : {};

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <ScreenHeader title={nombre} subtitle="Partida en vivo" />
      {aviso ? <Notice variant="info">{aviso}</Notice> : null}
      {avisoGeo ? <Notice variant="error">{avisoGeo}</Notice> : null}
      {fase.status === "cargando" ? <ActivityIndicator color={colors.primaryBright} style={styles.spinner} /> : null}
      {fase.status === "sin-participacion" ? (
        <Card style={styles.card}>
          <AppText>No tienes una participación activa en esta partida.</AppText>
          <Button label="Volver a partidas" onPress={onVolverAlPanel} />
        </Card>
      ) : null}
      {fase.status === "iniciada" && fase.juegoActivo?.tipoJuego === "Trivia" ? (
        <TriviaPlayPanel
          key={fase.juegoActivo.juegoId}
          apiBaseUrl={apiBaseUrl}
          token={token}
          partidaId={partidaId}
          juegoId={fase.juegoActivo.juegoId}
          yaRespondioInicial={fase.yaRespondio}
          refetchSignal={refetchSignal}
          resetSignal={resetSignal}
          miSub={miSub}
          rankingPush={rankingPush}
          preguntaCerrada={preguntaCerrada}
          respuestaEquipo={respuestaEquipo}
        />
      ) : null}
      {fase.status === "iniciada" && fase.juegoActivo?.tipoJuego === "BusquedaDelTesoro" ? (
        <BdtPlayPanel
          key={fase.juegoActivo.juegoId}
          apiBaseUrl={apiBaseUrl}
          token={token}
          partidaId={partidaId}
          juegoId={fase.juegoActivo.juegoId}
          refetchSignal={refetchSignal}
          resetSignal={resetSignal}
          miSub={miSub}
          pistas={pistas}
          rankingPush={rankingPush}
        />
      ) : null}
      {fase.status === "iniciada" && fase.juegoActivo == null ? (
        <Card style={styles.card}>
          <AppText>Esperando el siguiente juego…</AppText>
        </Card>
      ) : null}
      {fase.status === "finalizada" ? (
        <Card style={styles.card}>
          <AppText variant="bodyStrong">Partida finalizada</AppText>
          {consolidado ? (
            <RankingTable
              entradas={consolidado.map((e) => ({
                posicion: e.posicion,
                competidorId: e.competidorId,
                puntos: e.puntosTotales,
                juegosGanados: e.juegosGanados,
                tipoCompetidor: e.tipoCompetidor,
                motivoDesempate: motivosConsolidado[e.competidorId],
              }))}
              resaltarId={miSub}
              nombreDe={nombreDeConsolidado}
            />
          ) : null}
          {!consolidado ? (
            // Al terminar, Puntuaciones aún calcula el consolidado (async); llega solo por el push
            // RankingConsolidadoCalculado. Nada de botón "Reintentar": era un parpadeo redundante.
            <AppText>Calculando el ranking final…</AppText>
          ) : null}
          <Button label="Volver al menú" variant="secondary" onPress={onVolverAlMenu} />
        </Card>
      ) : null}
      {fase.status === "cancelada" ? (
        <Card style={styles.card}>
          <Notice variant="error">
            {fase.motivo ? `Partida cancelada: ${fase.motivo}` : "Partida cancelada."}
          </Notice>
          <Button label="Volver a partidas" onPress={onVolverAlPanel} />
        </Card>
      ) : null}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bg },
  content: { padding: spacing.xl, gap: spacing.lg },
  spinner: { marginTop: spacing.lg },
  card: { gap: spacing.sm },
});
