import React, { useCallback, useState } from "react";
import { ActivityIndicator, Pressable, RefreshControl, ScrollView, StyleSheet, View } from "react-native";
import { useFocusEffect } from "@react-navigation/native";
import { AppText, Button, Card, Notice, ScreenHeader } from "../../shared/ui";
import { colors, spacing } from "../../shared/theme";
import { cargarPanel, filtrarPorModalidad } from "./partidasPanelFlow.js";
import { nombrePartidaResuelto, useNombresPartida } from "../shared/useNombresPartida.js";

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
  onOpenMiSesion: (s: { partidaId: string; estadoPartida: string; nombre: string }) => void;
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

  // Recarga al ganar foco, no solo al montar: al volver del live tras terminar la
  // partida el stack hace pop (no re-monta), así que un useEffect de montaje dejaría
  // el banner "Tienes una participación activa" pegado. Mismo patrón que TeamPanelScreen.
  useFocusEffect(
    useCallback(() => {
      let vivo = true;
      (async () => {
        await load();
        if (vivo) setLoading(false);
      })();
      return () => {
        vivo = false;
      };
    }, [load])
  );

  async function onRefresh() {
    setRefreshing(true);
    await load();
    setRefreshing(false);
  }

  const visibles = filtrarPorModalidad(partidas, filtro) as PartidaPublicada[];

  // Se resuelve al cargar el panel, no al pulsar: una partida Iniciada no esta en
  // partidas-publicadas, asi que su nombre solo puede venir del directorio, y resolverlo
  // dentro del onPress dejaria el tap colgado mientras va la red.
  //
  // El retorno se descarta a proposito (no es un olvido): aqui el hook solo dispara la
  // carga y el repintado. El nombre se lee con nombrePartidaResuelto, que devuelve null
  // en vez del GUID corto, para poder caer a "Mi partida".
  useNombresPartida(miSesion ? [miSesion.partidaId] : [], apiBaseUrl, token);

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
          onPress={() =>
            onOpenMiSesion({
              partidaId: miSesion.partidaId,
              estadoPartida: miSesion.estadoPartida,
              // "Mi partida" y no el GUID corto: en una cabecera de juego en vivo es
              // mejor copy que "a3f9c1d2".
              nombre: nombrePartidaResuelto(miSesion.partidaId) ?? "Mi partida",
            })
          }
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
