import React, { useCallback, useEffect, useState } from "react";
import { ActivityIndicator, ScrollView, StyleSheet, View } from "react-native";
import { AppText, Card, Notice, ScreenHeader, StatePill } from "../../shared/ui";
import { colors, spacing } from "../../shared/theme";
import { getHistorialPartidas } from "./historialPartidasApi.js";
import { useNombresPartida } from "../shared/useNombresPartida.js";
import { lineaContextoPartida } from "./historialLabels.js";

type JuegoHistorial = {
  juegoId: string;
  orden: number;
  tipoJuego: "Trivia" | "BusquedaDelTesoro";
  puntos: number;
};

type PartidaHistorial = {
  partidaId: string;
  modalidad?: "Individual" | "Equipo" | null;
  fechaFin?: string | null;
  equipoId?: string | null;
  puntosTotales: number;
  posicion: number;
  gano: boolean;
  juegos: JuegoHistorial[];
};

type Props = {
  apiBaseUrl: string;
  token: string;
};

// getHistorialPartidas vive en un .js sin checkJs: TS pierde el discriminante literal de "ok"
// al inferir su tipo exportado (mismo caso que partidaLobbyFlow.js, ver PartidaLobbyScreen.tsx).
// Se declara aquí la forma real y se castea una vez por llamada en lugar de perder narrowing.
type HistorialResult =
  | { ok: true; data: { participanteId: string; partidas: PartidaHistorial[] } }
  | { ok: false; type: string; message?: string };

const TIPO_JUEGO_LABEL: Record<string, string> = {
  Trivia: "Trivia",
  BusquedaDelTesoro: "Búsqueda del tesoro",
};

export function HistorialPartidasScreen({ apiBaseUrl, token }: Props) {
  const [partidas, setPartidas] = useState<PartidaHistorial[]>([]);
  const [loading, setLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const nombrePartidaDe = useNombresPartida(
    partidas.map((p) => p.partidaId),
    apiBaseUrl,
    token
  );

  const load = useCallback(async () => {
    setErrorMessage(null);
    const result = (await getHistorialPartidas(apiBaseUrl, token)) as HistorialResult;
    if (!result.ok) {
      setErrorMessage(result.message ?? "No se pudo cargar el historial de partidas.");
      return;
    }
    setPartidas(result.data.partidas);
  }, [apiBaseUrl, token]);

  useEffect(() => {
    (async () => {
      setLoading(true);
      await load();
      setLoading(false);
    })();
  }, [load]);

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <ScreenHeader title="Mi historial" subtitle="Partidas que has jugado" />
      {errorMessage ? <Notice variant="error">{errorMessage}</Notice> : null}
      {loading ? <ActivityIndicator color={colors.primaryBright} style={styles.spinner} /> : null}
      {!loading && !errorMessage && partidas.length === 0 ? (
        <Card>
          <AppText style={styles.empty}>Aún no has jugado partidas.</AppText>
        </Card>
      ) : null}
      {partidas.map((p) => (
        <Card key={p.partidaId} style={styles.card}>
          <View style={styles.headerRow}>
            <AppText variant="bodyStrong" style={styles.titulo}>
              {nombrePartidaDe(p.partidaId)}
            </AppText>
            <StatePill state={p.gano ? "ok" : "done"} label={p.gano ? "Ganó" : "No ganó"} />
          </View>
          <AppText variant="label" color={colors.muted}>
            {lineaContextoPartida(p)}
          </AppText>
          <View style={styles.juegos}>
            {p.juegos.map((j) => (
              <AppText key={j.juegoId} variant="body">
                {j.orden}. {TIPO_JUEGO_LABEL[j.tipoJuego] ?? j.tipoJuego} — {j.puntos} pts
              </AppText>
            ))}
          </View>
        </Card>
      ))}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bg },
  content: { padding: spacing.xl, gap: spacing.lg },
  spinner: { marginTop: spacing.lg },
  empty: { color: colors.muted, textAlign: "center" },
  card: { gap: spacing.xs },
  headerRow: { flexDirection: "row", justifyContent: "space-between", alignItems: "center", gap: spacing.md },
  // El nombre es texto libre del operador (antes el título era el acotado
  // "Individual · N pts"): sin flex, uno largo empuja el StatePill fuera de la tarjeta.
  titulo: { flex: 1 },
  juegos: { marginTop: spacing.xs, gap: 2 },
});
