import React from "react";
import { ActivityIndicator, StyleSheet, View } from "react-native";
import {
  AppText,
  Button,
  DetailRow,
  Hero,
  Icon,
  Notice,
  Panel,
  PressableScale,
  Stage,
} from "../../shared/ui";
import { game, radius, spacing } from "../../shared/theme";
import { bdtModalityFilters, useBdtPublishedGames } from "./useBdtPublishedGames.js";

type Props = {
  apiBaseUrl: string;
  token: string;
  /** Abre la maqueta de ranking BDT (G4): demostrable sin backend. */
  onViewRanking?: () => void;
};

type BdtGame = {
  partidaId: string;
  nombre: string;
  modalidad: string;
  estado: string;
  areaBusqueda: string;
  cantidadEtapas: number;
};

type WaitingData = { nombre: string; modalidad: string; posicionEnLobby: number; mensaje?: string };

export function BdtPublishedGamesScreen({ apiBaseUrl, token, onViewRanking }: Props) {
  const {
    filter,
    setFilter,
    loading,
    errorMessage,
    joinErrorMessage,
    joiningPartidaId,
    waitingData,
    games,
    joinIndividual,
  } = useBdtPublishedGames({ apiBaseUrl, token }) as {
    filter: string;
    setFilter: (f: string) => void;
    loading: boolean;
    errorMessage: string | null;
    joinErrorMessage: string | null;
    joiningPartidaId: string | null;
    waitingData: WaitingData | null;
    games: BdtGame[];
    joinIndividual: (game: BdtGame) => void;
  };

  if (waitingData) {
    return (
      <Stage variant="indigo" gradient scroll>
        <Hero title="Sala de espera" subtitle={waitingData.mensaje ?? "Espera el inicio de la partida."} onStage />
        <View style={styles.posWrap}>
          <AppText style={styles.posNumber} color={game.onStage} allowFontScaling={false}>
            {waitingData.posicionEnLobby}
          </AppText>
          <AppText variant="label" color={game.onStageMuted}>
            posición en el lobby
          </AppText>
        </View>
        <Panel>
          <AppText variant="title" color={game.onStage}>
            {waitingData.nombre}
          </AppText>
          <DetailRow label="Modalidad" value={waitingData.modalidad} onStage />
        </Panel>
      </Stage>
    );
  }

  return (
    <Stage variant="ink" gradient scroll>
      <Hero title="Búsqueda del Tesoro" subtitle="Partidas BDT publicadas en lobby." onStage />

      <View style={styles.filters}>
        {bdtModalityFilters.map((item) => {
          const active = filter === item;
          return (
            <PressableScale
              key={item}
              accessibilityRole="button"
              accessibilityLabel={item}
              onPress={() => setFilter(item)}
              style={[styles.filterChip, active && styles.filterChipActive]}
            >
              <AppText variant="label" color={active ? game.stage.ink : game.onStageMuted}>
                {item}
              </AppText>
            </PressableScale>
          );
        })}
      </View>

      {loading ? <ActivityIndicator color={game.onStage} /> : null}
      {errorMessage ? <Notice variant="error">{errorMessage}</Notice> : null}
      {joinErrorMessage ? <Notice variant="error">{joinErrorMessage}</Notice> : null}
      {!loading && !errorMessage && games.length === 0 ? (
        <AppText variant="label" color={game.onStageMuted}>
          No hay partidas BDT publicadas para este filtro.
        </AppText>
      ) : null}

      {games.map((item) => {
        const individual = item.modalidad === "Individual";
        const joining = joiningPartidaId === item.partidaId;
        return (
          <Panel key={item.partidaId}>
            <View style={styles.cardHead}>
              <View style={styles.iconChip}>
                <Icon name="map-pin" size={20} color={game.onStage} />
              </View>
              <AppText variant="title" color={game.onStage} style={styles.flex}>
                {item.nombre}
              </AppText>
            </View>
            <DetailRow label="Modalidad" value={item.modalidad} onStage />
            <DetailRow label="Estado" value={item.estado} onStage />
            <DetailRow label="Área" value={item.areaBusqueda} onStage />
            <DetailRow label="Etapas" value={String(item.cantidadEtapas)} onStage />
            {individual ? (
              <Button
                label={joining ? "Uniéndote…" : "Unirme individualmente"}
                icon="user-plus"
                onStage
                disabled={joining}
                loading={joining}
                onPress={() => joinIndividual(item)}
              />
            ) : (
              <AppText variant="label" color={game.onStageMuted}>
                La unión por equipo se gestiona con el líder.
              </AppText>
            )}
          </Panel>
        );
      })}

      {onViewRanking ? (
        <Button label="Ver ranking BDT (demo)" icon="award" variant="secondary" onStage onPress={onViewRanking} />
      ) : null}
    </Stage>
  );
}

const styles = StyleSheet.create({
  filters: {
    flexDirection: "row",
    gap: spacing.sm,
  },
  filterChip: {
    minHeight: 44,
    justifyContent: "center",
    borderWidth: 1,
    borderColor: game.onStageLine,
    backgroundColor: game.onStageSunk,
    borderRadius: radius.pill,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.sm,
  },
  filterChipActive: {
    borderColor: game.onStage,
    backgroundColor: game.onStage,
  },
  cardHead: {
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.sm,
  },
  iconChip: {
    width: 36,
    height: 36,
    borderRadius: radius.md,
    backgroundColor: game.onStageSunk,
    borderWidth: 1,
    borderColor: game.onStageLine,
    alignItems: "center",
    justifyContent: "center",
  },
  flex: {
    flex: 1,
  },
  posWrap: {
    alignItems: "center",
    gap: spacing.xs,
  },
  posNumber: {
    fontFamily: "SpaceGrotesk_700Bold",
    fontSize: 72,
    lineHeight: 76,
    color: game.onStage,
  },
});
