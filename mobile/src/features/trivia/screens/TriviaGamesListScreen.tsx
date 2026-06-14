import React, { useCallback, useEffect, useState } from 'react';
import {
  ActivityIndicator,
  FlatList,
  RefreshControl,
  SafeAreaView,
  StyleSheet,
  TouchableOpacity,
  View,
} from 'react-native';
import { AppText, Button, EmptyPanel, Notice, StatePill } from '../../../shared/ui';
import { gameStatePill } from '../../../shared/statusPill';
import { colors, radius, spacing } from '../../../shared/theme';
import { getPublishedTriviaGames, TriviaMobileApiError } from '../../../api/triviaApi';
import { TriviaGameListItem } from '../types';
import { TEAM_TRIVIA_LEADER_WARNING } from '../triviaParticipantScreenModel.js';

type Props = {
  apiBaseUrl: string;
  token: string;
  onOpenLobby?: (partidaId: string) => void;
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

export default function TriviaGamesListScreen({ apiBaseUrl, token, onOpenLobby }: Props) {
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
    return (
      <TouchableOpacity style={styles.card} activeOpacity={0.7} onPress={() => onOpenLobby?.(item.id)}>
        <View style={styles.cardHead}>
          <AppText variant="title" style={styles.cardName}>
            {item.nombre}
          </AppText>
          <StatePill state={pill.state} label={pill.label} />
        </View>
        <View style={styles.cardRow}>
          <AppText variant="label" color={colors.inkSoft}>
            {item.modalidad === 'Individual' ? 'Individual' : 'Equipo'}
          </AppText>
          <AppText variant="label" color={colors.muted}>
            {formatDate(item.tiempoInicio)}
          </AppText>
        </View>
        <AppText variant="body" color={colors.muted}>
          {item.modalidad === 'Individual'
            ? `Jugadores: ${item.minimoParticipantes} - ${item.maximoJugadores ?? '-'}`
            : `Equipos: ${item.minimoParticipantes} - ${item.maximoEquipos ?? '-'}`}
        </AppText>
        <AppText variant="label" color={colors.primaryStrong}>
          {item.modalidad === 'Individual' ? 'Toca para unirte o ver espera' : TEAM_TRIVIA_LEADER_WARNING}
        </AppText>
      </TouchableOpacity>
    );
  };

  if (loading) {
    return (
      <SafeAreaView style={styles.safe}>
        <View style={styles.centered}>
          <ActivityIndicator size="large" color={colors.primaryFill} />
          <AppText variant="body" color={colors.muted}>
            Cargando partidas...
          </AppText>
        </View>
      </SafeAreaView>
    );
  }

  if (error) {
    return (
      <SafeAreaView style={styles.safe}>
        <View style={styles.centered}>
          <Notice variant="error" style={styles.notice}>
            {error}
          </Notice>
          <Button label="Reintentar" variant="secondary" onPress={() => fetchGames()} />
        </View>
      </SafeAreaView>
    );
  }

  if (games.length === 0) {
    return (
      <SafeAreaView style={styles.safe}>
        <View style={styles.centered}>
          <EmptyPanel
            title="No hay partidas de Trivia publicadas"
            message="Cuando un operador publique una Trivia, aparecerá aquí para que te unas."
            action={<Button label="Actualizar" variant="secondary" onPress={() => fetchGames()} />}
          />
        </View>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.safe}>
      <View style={styles.filterBar}>
        {(['Todas', 'Individual', 'Equipo'] as Filtro[]).map((opcion) => {
          const active = filtro === opcion;
          return (
            <TouchableOpacity
              key={opcion}
              style={[styles.filterChip, active && styles.filterChipActive]}
              onPress={() => setFiltro(opcion)}
            >
              <AppText variant="label" color={active ? colors.primaryStrong : colors.inkSoft}>
                {opcion}
              </AppText>
            </TouchableOpacity>
          );
        })}
      </View>
      <FlatList
        data={games}
        keyExtractor={(item) => item.id}
        renderItem={renderGame}
        contentContainerStyle={styles.list}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => fetchGames(true)} />}
      />
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: {
    flex: 1,
    backgroundColor: colors.bg,
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
    borderColor: colors.lineStrong,
    backgroundColor: colors.surface,
    borderRadius: radius.pill,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.sm,
  },
  filterChipActive: {
    borderColor: colors.primaryBright,
    backgroundColor: colors.primaryWash,
  },
  list: {
    padding: spacing.lg,
    gap: spacing.md,
  },
  card: {
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    borderRadius: radius.lg,
    padding: spacing.lg,
    gap: spacing.sm,
  },
  cardHead: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    gap: spacing.sm,
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
