import React, { useCallback, useEffect, useState } from "react";
import { ActivityIndicator, ScrollView, StyleSheet, View } from "react-native";
import { AppText, Card, Notice, ScreenHeader, StatePill } from "../../shared/ui";
import { colors, spacing } from "../../shared/theme";
import { getRendimientoMiEquipo } from "./rendimientoEquipoApi.js";

type PartidaRendimiento = {
  partidaId: string;
  fechaFin?: string | null;
  posicion: number;
  gano: boolean;
};

type Props = {
  apiBaseUrl: string;
  token: string;
};

// getRendimientoMiEquipo vive en un .js sin checkJs, mismo caso que historialPartidasApi.js
// (ver HistorialPartidasScreen.tsx): se declara aquí la forma real y se castea una vez.
type RendimientoResult =
  | { ok: true; data: { equipoId: string; partidas: PartidaRendimiento[] } }
  | { ok: false; type: string; message?: string };

export function RendimientoEquipoScreen({ apiBaseUrl, token }: Props) {
  const [partidas, setPartidas] = useState<PartidaRendimiento[]>([]);
  const [loading, setLoading] = useState(true);
  const [sinEquipo, setSinEquipo] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const load = useCallback(async () => {
    setErrorMessage(null);
    setSinEquipo(false);
    const result = (await getRendimientoMiEquipo(apiBaseUrl, token)) as RendimientoResult;
    if (!result.ok) {
      if (result.type === "sinEquipo") {
        setSinEquipo(true);
        return;
      }
      setErrorMessage(result.message ?? "No se pudo cargar el rendimiento del equipo.");
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
      <ScreenHeader title="Rendimiento del equipo" subtitle="Tus partidas jugadas en equipo" />
      {errorMessage ? <Notice variant="error">{errorMessage}</Notice> : null}
      {loading ? <ActivityIndicator color={colors.primaryBright} style={styles.spinner} /> : null}
      {!loading && sinEquipo ? (
        <Card>
          <AppText style={styles.empty}>No perteneces a un equipo activo.</AppText>
        </Card>
      ) : null}
      {!loading && !errorMessage && !sinEquipo && partidas.length === 0 ? (
        <Card>
          <AppText style={styles.empty}>Tu equipo aún no ha jugado partidas.</AppText>
        </Card>
      ) : null}
      {partidas.map((p) => (
        <Card key={p.partidaId} style={styles.card}>
          <View style={styles.headerRow}>
            <AppText variant="bodyStrong">Posición {p.posicion}</AppText>
            <StatePill state={p.gano ? "ok" : "done"} label={p.gano ? "Ganó" : "No ganó"} />
          </View>
          {p.fechaFin ? (
            <AppText variant="label" color={colors.muted}>
              {new Date(p.fechaFin).toLocaleDateString()}
            </AppText>
          ) : null}
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
});
