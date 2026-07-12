import React, { useCallback, useEffect, useState } from "react";
import { ActivityIndicator, Pressable, RefreshControl, ScrollView, StyleSheet, View } from "react-native";
import { AppText, Button, Card, Notice, ScreenHeader } from "../../shared/ui";
import { colors, spacing } from "../../shared/theme";
import { cargarPanel, filtrarPorModalidad } from "./partidasPanelFlow.js";

type PartidaPublicada = {
  partidaId: string;
  nombre: string;
  modalidad: "Individual" | "Equipo";
  modoInicioPartida: string;
  tiempoInicio: string | null;
  minimosParticipacion: number;
  maximosParticipacion: number;
  inscritosActivos: number;
};

type MiSesion = { partidaId: string; estadoPartida: string } | null;

type Filtro = "Todas" | "Individual" | "Equipo";
const FILTROS: Filtro[] = ["Todas", "Individual", "Equipo"];

type Props = {
  apiBaseUrl: string;
  token: string;
  onOpenPartida: (partida: { partidaId: string; nombre: string }) => void;
  onOpenMiSesion: (s: { partidaId: string; estadoPartida: string }) => void;
};

export function PartidasPanelScreen({ apiBaseUrl, token, onOpenPartida, onOpenMiSesion }: Props) {
  const [partidas, setPartidas] = useState<PartidaPublicada[]>([]);
  const [miSesion, setMiSesion] = useState<MiSesion>(null);
  const [filtro, setFiltro] = useState<Filtro>("Todas");
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const load = useCallback(async () => {
    setErrorMessage(null);
    const result = await cargarPanel({ apiBaseUrl, token, fetchImpl: undefined });
    if (!result.ok) {
      setErrorMessage(result.message ?? "No se pudieron cargar las partidas.");
      return;
    }
    setPartidas(result.partidas as PartidaPublicada[]);
    setMiSesion(result.miSesion as MiSesion);
  }, [apiBaseUrl, token]);

  useEffect(() => {
    (async () => {
      setLoading(true);
      await load();
      setLoading(false);
    })();
  }, [load]);

  async function onRefresh() {
    setRefreshing(true);
    await load();
    setRefreshing(false);
  }

  const visibles = filtrarPorModalidad(partidas, filtro) as PartidaPublicada[];

  return (
    <ScrollView
      style={styles.container}
      contentContainerStyle={styles.content}
      refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => void onRefresh()} />}
    >
      <ScreenHeader title="Partidas" subtitle="Únete a una partida publicada" />
      {errorMessage ? <Notice variant="error">{errorMessage}</Notice> : null}
      {miSesion ? (
        <Pressable
          accessibilityLabel="Ir a mi participación activa"
          onPress={() => onOpenMiSesion({ partidaId: miSesion.partidaId, estadoPartida: miSesion.estadoPartida })}
        >
          <Notice variant="info">Tienes una participación activa. Toca para volver a tu partida.</Notice>
        </Pressable>
      ) : null}
      <View style={styles.filtros}>
        {FILTROS.map((f) => (
          <Button
            key={f}
            label={f}
            variant={f === filtro ? "primary" : "secondary"}
            onPress={() => setFiltro(f)}
          />
        ))}
      </View>
      {loading ? <ActivityIndicator color={colors.primaryBright} style={styles.spinner} /> : null}
      {!loading && visibles.length === 0 ? (
        <Card>
          <AppText style={styles.empty}>No hay partidas publicadas ahora mismo.</AppText>
        </Card>
      ) : null}
      {visibles.map((p) => (
        <Pressable key={p.partidaId} onPress={() => onOpenPartida({ partidaId: p.partidaId, nombre: p.nombre })}>
          <Card style={styles.card}>
            <AppText variant="bodyStrong">{p.nombre}</AppText>
            <AppText>
              {p.modalidad} · {p.inscritosActivos}/{p.maximosParticipacion} · min {p.minimosParticipacion}
            </AppText>
            <AppText>
              Inicio {p.modoInicioPartida}
              {p.tiempoInicio ? ` — ${new Date(p.tiempoInicio).toLocaleTimeString()}` : ""}
            </AppText>
          </Card>
        </Pressable>
      ))}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bg },
  content: { padding: spacing.xl, gap: spacing.lg },
  filtros: { flexDirection: "row", gap: spacing.sm },
  spinner: { marginTop: spacing.lg },
  empty: { color: colors.muted, textAlign: "center" },
  card: { marginBottom: spacing.sm },
});
