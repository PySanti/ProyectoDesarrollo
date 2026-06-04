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
import { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { useNavigation } from '@react-navigation/native';
import ScreenWrapper from '../../../shared/components/ScreenWrapper';
import { getPublishedTriviaGames } from '../../../api/triviaApi';
import { TriviaGameListItem } from '../types';

type NavigationProp = NativeStackNavigationProp<any>;

function formatDate(iso: string): string {
  const d = new Date(iso);
  return d.toLocaleDateString('es-ES', {
    day: 'numeric',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export default function TriviaGamesListScreen() {
  const navigation = useNavigation<NavigationProp>();
  const [games, setGames] = useState<TriviaGameListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchGames = useCallback(async (isRefresh = false) => {
    try {
      if (isRefresh) setRefreshing(true);
      else setLoading(true);
      setError(null);
      const data = await getPublishedTriviaGames();
      setGames(data);
    } catch (err: any) {
      if (err?.response?.status === 401) {
        setError('Sesión expirada. Inicia sesión nuevamente.');
      } else {
        setError('No se pudieron cargar las partidas. Verifica tu conexión.');
      }
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useEffect(() => {
    fetchGames();
  }, [fetchGames]);

  const renderGame = ({ item }: { item: TriviaGameListItem }) => (
    <TouchableOpacity style={styles.card} activeOpacity={0.7}>
      <Text style={styles.gameName}>{item.nombre}</Text>
      <View style={styles.cardRow}>
        <Text style={styles.modalidad}>
          {item.modalidad === 'Individual' ? '👤 Individual' : '👥 Equipo'}
        </Text>
        <Text style={styles.date}>{formatDate(item.tiempoInicio)}</Text>
      </View>
      <Text style={styles.participants}>
        {item.modalidad === 'Individual'
          ? `Jugadores: ${item.minimoParticipantes} – ${item.maximoJugadores ?? '—'}`
          : `Equipos: ${item.minimoParticipantes} – ${item.maximoEquipos ?? '—'}`}
      </Text>
    </TouchableOpacity>
  );

  if (loading) {
    return (
      <ScreenWrapper style={styles.centered}>
        <ActivityIndicator size="large" color="#2563EB" />
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
        <Text style={styles.emptyIcon}>📋</Text>
        <Text style={styles.emptyText}>No hay partidas de Trivia publicadas</Text>
        <TouchableOpacity style={styles.retryButton} onPress={() => fetchGames()}>
          <Text style={styles.retryText}>Actualizar</Text>
        </TouchableOpacity>
      </ScreenWrapper>
    );
  }

  return (
    <ScreenWrapper>
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
  centered: {
    justifyContent: 'center',
    alignItems: 'center',
    padding: 24,
  },
  list: {
    padding: 16,
  },
  card: {
    backgroundColor: '#FFFFFF',
    borderRadius: 12,
    padding: 16,
    marginBottom: 12,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.08,
    shadowRadius: 4,
    elevation: 2,
  },
  gameName: {
    fontSize: 16,
    fontWeight: '600',
    color: '#1F2937',
    marginBottom: 8,
  },
  cardRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 4,
  },
  modalidad: {
    fontSize: 13,
    color: '#6B7280',
  },
  date: {
    fontSize: 12,
    color: '#9CA3AF',
  },
  participants: {
    fontSize: 12,
    color: '#9CA3AF',
    marginTop: 4,
  },
  loadingText: {
    marginTop: 12,
    fontSize: 14,
    color: '#6B7280',
  },
  errorText: {
    fontSize: 14,
    color: '#DC2626',
    textAlign: 'center',
    marginBottom: 16,
  },
  retryButton: {
    backgroundColor: '#2563EB',
    paddingHorizontal: 24,
    paddingVertical: 10,
    borderRadius: 8,
  },
  retryText: {
    color: '#FFFFFF',
    fontSize: 14,
    fontWeight: '600',
  },
  emptyIcon: {
    fontSize: 48,
    marginBottom: 12,
  },
  emptyText: {
    fontSize: 15,
    color: '#6B7280',
    textAlign: 'center',
    marginBottom: 16,
  },
});
