import React, { useCallback, useEffect, useState } from "react";
import { ActivityIndicator, SafeAreaView, ScrollView, StyleSheet, View } from "react-native";
import { AppText, Button, Card, DetailRow, Notice, StatePill } from "../../../shared/ui";
import { gameStatePill } from "../../../shared/statusPill";
import { colors, spacing } from "../../../shared/theme";
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

  const pill = lobby ? gameStatePill(lobby.estado) : null;

  return (
    <SafeAreaView style={styles.safe}>
      <ScrollView contentContainerStyle={styles.content}>
        <View style={styles.head}>
          <AppText variant="display">Espera de Trivia</AppText>
          <AppText variant="body" color={colors.muted}>
            HU-18 y HU-21 para participante movil.
          </AppText>
        </View>

        {loading ? <ActivityIndicator color={colors.primaryFill} /> : null}
        {message ? <Notice variant="success">{message}</Notice> : null}
        {error ? <Notice variant="error">{error}</Notice> : null}

        {lobby ? (
          <Card>
            <View style={styles.cardHead}>
              <AppText variant="title" style={styles.cardName}>
                {lobby.nombre}
              </AppText>
              {pill ? <StatePill state={pill.state} label={pill.label} /> : null}
            </View>
            <DetailRow label="Modalidad" value={lobby.modalidad} />
            <DetailRow label="Participantes" value={String(lobby.participantesActual)} />
            <DetailRow label="Mínimo" value={String(lobby.minimoParticipantes)} />
          </Card>
        ) : null}

        <Button label="Unirme individualmente" onPress={() => void handleJoin()} loading={joining} />
        <Button label="Responder pregunta" variant="secondary" onPress={() => onAnswer?.(partidaId)} />
        <Button label="Ver puntaje" variant="ghost" onPress={() => onScore?.(partidaId)} />
      </ScrollView>
    </SafeAreaView>
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
  safe: {
    flex: 1,
    backgroundColor: colors.bg,
  },
  content: {
    padding: spacing.xl,
    gap: spacing.lg,
  },
  head: {
    gap: spacing.xs,
  },
  cardHead: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "flex-start",
    gap: spacing.sm,
  },
  cardName: {
    flex: 1,
  },
});
