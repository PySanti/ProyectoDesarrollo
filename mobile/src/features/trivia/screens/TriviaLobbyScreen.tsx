import React, { useCallback, useEffect, useState } from "react";
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from "react-native";
import ScreenWrapper from "../../../shared/components/ScreenWrapper";
import { screenStyles } from "../../../shared/styles";
import { colors } from "../../../shared/theme";
import { TriviaLobbyResponse, joinIndividualTriviaGame, getTriviaLobby, TriviaMobileApiError } from "../../../api/triviaApi";

type Props = {
  apiBaseUrl: string;
  token: string;
  partidaId: string;
  onAnswer?: (partidaId: string) => void;
  onScore?: (partidaId: string) => void;
};

export function TriviaLobbyScreen({ apiBaseUrl, token, partidaId, onAnswer, onScore }: Props) {
  const [lobby, setLobby] = useState<TriviaLobbyResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [joining, setJoining] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const loadLobby = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setLobby(await getTriviaLobby({ apiBaseUrl, token, partidaId }));
    } catch (caught) {
      setError(mapError(caught, "No se pudo cargar la espera de Trivia."));
    } finally {
      setLoading(false);
    }
  }, [apiBaseUrl, partidaId, token]);

  useEffect(() => {
    void loadLobby();
  }, [loadLobby]);

  async function handleJoin() {
    setJoining(true);
    setError(null);
    setMessage(null);
    try {
      await joinIndividualTriviaGame({ apiBaseUrl, token, partidaId });
      setMessage("Te uniste a la Trivia. Espera el inicio de la partida.");
      await loadLobby();
    } catch (caught) {
      setError(mapError(caught, "No se pudo unir a la Trivia."));
    } finally {
      setJoining(false);
    }
  }

  return (
    <ScreenWrapper>
      <View style={styles.container}>
        <Text style={styles.title}>Espera de Trivia</Text>
        <Text style={styles.description}>HU-18 y HU-21 para participante movil.</Text>
        {loading ? <ActivityIndicator color={colors.primary} /> : null}
        {message ? <Text style={styles.success}>{message}</Text> : null}
        {error ? <Text style={styles.error}>{error}</Text> : null}
        {lobby ? (
          <View style={styles.card}>
            <Text style={styles.cardTitle}>{lobby.nombre}</Text>
            <Text style={styles.cardLine}>Estado: {lobby.estado}</Text>
            <Text style={styles.cardLine}>Modalidad: {lobby.modalidad}</Text>
            <Text style={styles.cardLine}>Participantes: {lobby.participantesActual}</Text>
            <Text style={styles.cardLine}>Minimo: {lobby.minimoParticipantes}</Text>
          </View>
        ) : null}
        <Pressable style={styles.joinButton} onPress={() => void handleJoin()} disabled={joining}>
          <Text style={styles.joinButtonText}>{joining ? "Uniendote..." : "Unirme individualmente"}</Text>
        </Pressable>
        <Pressable style={styles.secondaryButton} onPress={() => onAnswer?.(partidaId)}>
          <Text style={styles.secondaryButtonText}>Responder pregunta</Text>
        </Pressable>
        <Pressable style={styles.secondaryButton} onPress={() => onScore?.(partidaId)}>
          <Text style={styles.secondaryButtonText}>Ver puntaje</Text>
        </Pressable>
      </View>
    </ScreenWrapper>
  );
}

function mapError(caught: unknown, fallback: string): string {
  if (caught instanceof TriviaMobileApiError) {
    if (caught.status === 409) return caught.message;
    if (caught.status === 403) return "No estas registrado para esta Trivia o no tienes permiso.";
    return caught.message;
  }

  return fallback;
}

const styles = StyleSheet.create({
  container: screenStyles.scrollContainer,
  title: screenStyles.title,
  description: screenStyles.description,
  card: screenStyles.card,
  cardTitle: screenStyles.cardTitle,
  cardLine: screenStyles.cardLine,
  error: screenStyles.error,
  success: { ...screenStyles.empty, color: colors.success },
  joinButton: screenStyles.joinButton,
  joinButtonText: screenStyles.joinButtonText,
  secondaryButton: { ...screenStyles.filterButton, marginTop: 10 },
  secondaryButtonText: screenStyles.filterText,
});
