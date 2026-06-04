import React, { useCallback, useEffect, useState } from 'react';
import {
  ActivityIndicator,
  FlatList,
  RefreshControl,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from 'react-native';
import ScreenWrapper from '../../../shared/components/ScreenWrapper';
import { colors } from '../../../shared/theme';
import { screenStyles } from '../../../shared/styles';
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

  const renderGame = ({ item }: { item: TriviaGameListItem }) => (
    <TouchableOpacity style={styles.card} activeOpacity={0.7} onPress={() => onOpenLobby?.(item.id)}>
      <Text style={styles.gameName}>{item.nombre}</Text>
      <View style={styles.cardRow}>
        <Text style={styles.modalidad}>
          {item.modalidad === 'Individual' ? 'Individual' : 'Equipo'}
        </Text>
        <Text style={styles.date}>{formatDate(item.tiempoInicio)}</Text>
      </View>
      <Text style={styles.participants}>
        {item.modalidad === 'Individual'
          ? `Jugadores: ${item.minimoParticipantes} - ${item.maximoJugadores ?? '-'}`
          : `Equipos: ${item.minimoParticipantes} - ${item.maximoEquipos ?? '-'}`}
      </Text>
      <Text style={styles.actionText}>
        {item.modalidad === 'Individual' ? 'Toca para unirte o ver espera' : TEAM_TRIVIA_LEADER_WARNING}
      </Text>
    </TouchableOpacity>
  );

  if (loading) {
    return (
      <ScreenWrapper style={styles.centered}>
        <ActivityIndicator size="large" color={colors.primary} />
        <Text style={styles.loadingText}>Cargando partidas...</Text>
      </ScreenWrapper>
    );
  }

  if (error) {
    return (
      <ScreenWrapper style={styles.centered}>
        <Text style={styles.errorText}>{error}</Text>
        <TouchableOpacity style={styles.retryButton} onPress={() => fetchGames()}>
          <Text style={styles.retryText}>Reintentar</Text>
        </TouchableOpacity>
      </ScreenWrapper>
    );
  }

  if (games.length === 0) {
    return (
      <ScreenWrapper style={styles.centered}>
        <Text style={styles.emptyText}>No hay partidas de Trivia publicadas</Text>
        <TouchableOpacity style={styles.retryButton} onPress={() => fetchGames()}>
          <Text style={styles.retryText}>Actualizar</Text>
        </TouchableOpacity>
      </ScreenWrapper>
    );
  }

  return (
    <ScreenWrapper>
      <View style={styles.filterBar}>
        {(['Todas', 'Individual', 'Equipo'] as Filtro[]).map((opcion) => (
          <TouchableOpacity
            key={opcion}
            style={[styles.filterChip, filtro === opcion && styles.filterChipActive]}
            onPress={() => setFiltro(opcion)}
          >
            <Text style={[styles.filterChipText, filtro === opcion && styles.filterChipTextActive]}>
              {opcion === 'Todas' ? 'Todas' : opcion}
            </Text>
          </TouchableOpacity>
        ))}
      </View>
      <FlatList
        data={games}
        keyExtractor={(item) => item.id}
        renderItem={renderGame}
        contentContainerStyle={styles.list}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={() => fetchGames(true)} />
        }
      />
    </ScreenWrapper>
  );
}

const styles = StyleSheet.create({
  filterBar: {
    flexDirection: 'row',
    paddingHorizontal: 16,
    paddingTop: 12,
    paddingBottom: 4,
    gap: 8,
  },
  filterChip: {
    ...screenStyles.filterButton,
    paddingHorizontal: 16,
    paddingVertical: 6,
  },
  filterChipActive: screenStyles.filterButtonActive,
  filterChipText: screenStyles.filterText,
  filterChipTextActive: screenStyles.filterTextActive,
  centered: {
    ...screenStyles.centered,
  },
  list: {
    padding: 16,
  },
  card: {
    ...screenStyles.card,
    padding: 16,
    marginBottom: 12,
  },
  gameName: screenStyles.cardTitle,
  cardRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 4,
  },
  modalidad: screenStyles.cardLine,
  date: {
    fontSize: 12,
    color: colors.textSoft,
  },
  participants: {
    fontSize: 12,
    color: colors.textSoft,
    marginTop: 4,
  },
  actionText: {
    color: colors.primary,
    fontWeight: '700',
    marginTop: 10,
  },
  loadingText: screenStyles.loadingText,
  errorText: screenStyles.errorText,
  retryButton: screenStyles.joinButton,
  retryText: screenStyles.joinButtonText,
  emptyText: screenStyles.emptyText,
});
