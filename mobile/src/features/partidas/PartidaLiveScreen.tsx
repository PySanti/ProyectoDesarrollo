// Pantalla live del participante: fases por mi-sesion + hub de sesion; monta el panel del juego activo.
import React, { useCallback, useEffect, useRef, useState } from "react";
import { ActivityIndicator, ScrollView, StyleSheet, View } from "react-native";
import { AppText, Button, Card, Notice, ScreenHeader } from "../../shared/ui";
import { colors, spacing } from "../../shared/theme";
import { cargarLive } from "./partidaLiveFlow.js";
import { getRankingConsolidado } from "./gameplayApi.js";
import { crearSesionHub } from "./sesionHub.js";
import { crearRankingHub } from "./rankingHub.js";
import { TriviaPlayPanel } from "./TriviaPlayPanel";
import { BdtPlayPanel, type Pista } from "./BdtPlayPanel";
import { requestBdtGeolocationPermission } from "../../permissions/bdtGeolocationPermission.js";
import { RankingTable, type RankingEntrada } from "./liveShared";

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
};

export function PartidaLiveScreen({ apiBaseUrl, token, partidaId, nombre, miSub, onVolverAlPanel }: Props) {
  const [fase, setFase] = useState<Fase>({ status: "cargando" });
  const [refetchSignal, setRefetchSignal] = useState(0);
  const [resetSignal, setResetSignal] = useState(0);
  const [aviso, setAviso] = useState<string | null>(null);
  const [consolidado, setConsolidado] = useState<ConsolidadoEntrada[] | null>(null);
  const [consolidadoError, setConsolidadoError] = useState(false);
  const [pistas, setPistas] = useState<Pista[]>([]);
  const [avisoGeo, setAvisoGeo] = useState<string | null>(null);
  const [rankingPush, setRankingPush] = useState<{ juegoId: string; entradas: RankingEntrada[] } | null>(null);
  // HU-24/BR-T06: última PreguntaCerrada, para mostrar la respuesta correcta en el panel Trivia.
  // juegoId viaja en el payload (7d review fix): el panel descarta cierres de un juego que ya no
  // es el activo, porque este estado vive a nivel de partida y no se limpia al cambiar de juego.
  const [preguntaCerrada, setPreguntaCerrada] = useState<{ texto: string | null; juegoId: string } | null>(null);
  const hubRef = useRef<ReturnType<typeof crearSesionHub> | null>(null);
  // El token va por ref: un refresh de sesión (RNF-24) no debe derribar la conexión viva
  // (solo se usa en el handshake de conexión/reconexión).
  const tokenRef = useRef(token);
  tokenRef.current = token;

  const load = useCallback(async () => {
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
    setConsolidadoError(false);
    const r = (await getRankingConsolidado(apiBaseUrl, token, partidaId, undefined)) as ConsolidadoResult;
    if (r.ok) setConsolidado(r.ranking.entradas ?? []);
    else setConsolidadoError(true);
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
    });
    hub.on("PreguntaCerrada", (p: { juegoId?: string; textoOpcionCorrecta?: string | null }) => {
      setRefetchSignal((s) => s + 1);
      setPreguntaCerrada({ texto: p?.textoOpcionCorrecta ?? null, juegoId: p?.juegoId ?? "" });
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
    hub
      .start()
      .then(() => hub.invoke("SuscribirAPartida", partidaId))
      .catch(() => setAviso("Sin conexión en vivo; los cambios pueden tardar."));
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
        setConsolidadoError(false);
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
              }))}
              resaltarId={miSub}
            />
          ) : null}
          {consolidadoError ? (
            <View style={styles.retry}>
              <AppText>Consolidado no disponible aún.</AppText>
              <Button label="Reintentar" onPress={() => void cargarConsolidado()} />
            </View>
          ) : null}
          <Button label="Volver a partidas" variant="secondary" onPress={onVolverAlPanel} />
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
  retry: { gap: spacing.sm },
});
