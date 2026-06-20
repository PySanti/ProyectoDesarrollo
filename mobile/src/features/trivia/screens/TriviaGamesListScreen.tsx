import React, { useCallback, useEffect, useState } from 'react';
import { ActivityIndicator, FlatList, RefreshControl, StyleSheet, View } from 'react-native';
import { AppText, Button, EmptyPanel, Icon, Notice, PressableScale, Stage, StatePill } from '../../../shared/ui';
import { gameStatePill } from '../../../shared/statusPill';
import { game, radius, spacing } from '../../../shared/theme';
import { getPublishedTriviaGames, TriviaMobileApiError } from '../../../api/triviaApi';
import { TriviaGameListItem } from '../types';
import { TEAM_TRIVIA_LEADER_WARNING } from '../triviaParticipantScreenModel.js';

type Props = {
  apiBaseUrl: string;
  token: string;
  onOpenLobby?: (partidaId: string) => void;
  /** Lanza la maqueta de partida en vivo (G2): demostrable sin backend. */
  onPlayDemo?: () => void;
};

function formatDate(iso: string): string {
  const d = new Date(iso);
  return d.toLocaleDateString('es-ES', {
    day: 'numeric',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
  });
}

type Filtro = 'Todas' | 'Individual' | 'Equipo';

export default function TriviaGamesListScreen({ apiBaseUrl, token, onOpenLobby, onPlayDemo }: Props) {
  const [games, setGames] = useState<TriviaGameListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [filtro, setFiltro] = useState<Filtro>('Todas');

  const fetchGames = useCallback(async (isRefresh = false) => {
    try {
      if (isRefresh) setRefreshing(true);
      else setLoading(true);
      setError(null);
      const modalidadParam = filtro === 'Todas' ? undefined : filtro;
      const data = await getPublishedTriviaGames({ apiBaseUrl, token, modalidad: modalidadParam });
      setGames(data);
    } catch (err: unknown) {
      if (err instanceof TriviaMobileApiError && err.status === 401) {
        setError('Sesión expirada. Inicia sesión nuevamente.');
      } else {
        setError('No se pudieron cargar las partidas. Verifica tu conexión.');
      }
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [apiBaseUrl, filtro, token]);

  useEffect(() => {
    fetchGames();
  }, [fetchGames]);

  const renderGame = ({ item }: { item: TriviaGameListItem }) => {
    const pill = gameStatePill(item.estado);
    const individual = item.modalidad === 'Individual';
    return (
      <PressableScale
        style={styles.card}
        accessibilityRole="button"
        accessibilityLabel={item.nombre}
        onPress={() => onOpenLobby?.(item.id)}
      >
        <View style={styles.cardHead}>
          <View style={styles.iconChip}>
            <Icon name={individual ? 'user' : 'users'} size={20} color={game.onStage} />
          </View>
          <AppText variant="title" color={game.onStage} style={styles.cardName}>
            {item.nombre}
          </AppText>
          <StatePill state={pill.state} label={pill.label} />
        </View>
        <View style={styles.cardRow}>
          <AppText variant="label" color={game.onStage}>
            {individual ? 'Individual' : 'Equipo'}
          </AppText>
          <AppText variant="label" color={game.onStageMuted}>
            {formatDate(item.tiempoInicio)}
          </AppText>
        </View>
        <AppText variant="body" color={game.onStageMuted}>
          {individual
            ? `Jugadores: ${item.minimoParticipantes} - ${item.maximoJugadores ?? '-'}`
            : `Equipos: ${item.minimoParticipantes} - ${item.maximoEquipos ?? '-'}`}
        </AppText>
        <AppText variant="label" color={game.onStage}>
          {individual ? 'Toca para unirte o ver espera' : TEAM_TRIVIA_LEADER_WARNING}
        </AppText>
      </PressableScale>
    );
  };

  if (loading) {
    return (
      <Stage variant="ink" gradient>
        <View style={styles.centered}>
          <ActivityIndicator size="large" color={game.onStage} />
          <AppText variant="body" color={game.onStageMuted}>
            Cargando partidas...
          </AppText>
        </View>
      </Stage>
    );
  }

  if (error) {
    return (
      <Stage variant="ink" gradient>
        <View style={styles.centered}>
          <Notice variant="error" style={styles.notice}>
            {error}
          </Notice>
          <Button label="Reintentar" variant="secondary" onStage onPress={() => fetchGames()} />
          {onPlayDemo ? (
            <Button label="Probar partida en vivo (demo)" icon="play" onStage onPress={onPlayDemo} />
          ) : null}
        </View>
      </Stage>
    );
  }

  if (games.length === 0) {
    return (
      <Stage variant="ink" gradient>
        <View style={styles.centered}>
          <EmptyPanel
            title="No hay partidas de Trivia publicadas"
            message="Cuando un operador publique una Trivia, aparecerá aquí para que te unas."
            action={
              <View style={styles.emptyActions}>
                <Button label="Actualizar" variant="secondary" onPress={() => fetchGames()} />
                {onPlayDemo ? (
                  <Button label="Probar partida en vivo (demo)" icon="play" onPress={onPlayDemo} />
                ) : null}
              </View>
            }
          />
        </View>
      </Stage>
    );
  }

  return (
    <Stage variant="ink" gradient contentStyle={styles.stageContent}>
      <View style={styles.filterBar}>
        {(['Todas', 'Individual', 'Equipo'] as Filtro[]).map((opcion) => {
          const active = filtro === opcion;
          return (
            <PressableScale
              key={opcion}
              style={[styles.filterChip, active && styles.filterChipActive]}
              accessibilityRole="button"
              accessibilityLabel={opcion}
              onPress={() => setFiltro(opcion)}
            >
              <AppText variant="label" color={active ? game.stage.ink : game.onStageMuted}>
                {opcion}
              </AppText>
            </PressableScale>
          );
        })}
      </View>
      <FlatList
        data={games}
        keyExtractor={(item) => item.id}
        renderItem={renderGame}
        contentContainerStyle={styles.list}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={() => fetchGames(true)} tintColor={game.onStage} />
        }
      />
    </Stage>
  );
}

const styles = StyleSheet.create({
  stageContent: {
    padding: 0,
    gap: 0,
    flex: 1,
  },
  centered: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: spacing.xl,
    gap: spacing.lg,
  },
  notice: {
    alignSelf: 'stretch',
  },
  emptyActions: {
    gap: spacing.sm,
    alignSelf: 'stretch',
  },
  filterBar: {
    flexDirection: 'row',
    paddingHorizontal: spacing.lg,
    paddingTop: spacing.md,
    paddingBottom: spacing.xs,
    gap: spacing.sm,
  },
  filterChip: {
    minHeight: 44,
    justifyContent: 'center',
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
  list: {
    padding: spacing.lg,
    gap: spacing.md,
  },
  card: {
    backgroundColor: game.onStageSunk,
    borderWidth: 1,
    borderColor: game.onStageLine,
    borderRadius: radius.lg,
    padding: spacing.lg,
    gap: spacing.sm,
  },
  cardHead: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
  },
  iconChip: {
    width: 36,
    height: 36,
    borderRadius: radius.md,
    backgroundColor: game.onStageSunk,
    borderWidth: 1,
    borderColor: game.onStageLine,
    alignItems: 'center',
    justifyContent: 'center',
  },
  cardName: {
    flex: 1,
  },
  cardRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
});
