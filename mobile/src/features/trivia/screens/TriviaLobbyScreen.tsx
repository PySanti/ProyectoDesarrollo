import React, { useCallback, useEffect, useState } from "react";
import { ActivityIndicator, StyleSheet, View } from "react-native";
import { AppText, Button, DetailRow, Hero, Notice, Panel, Stage, StatePill } from "../../../shared/ui";
import { gameStatePill } from "../../../shared/statusPill";
import { game, spacing, typography } from "../../../shared/theme";
import { useCountUp } from "../../../shared/useCountUp";
import { TriviaLobbyResponse, joinIndividualTriviaGame, getTriviaLobby, TriviaMobileApiError } from "../../../api/triviaApi";

type Props = {
  apiBaseUrl: string;
  token: string;
  partidaId: string;
  onLivePlay?: (partidaId: string) => void;
  onAnswer?: (partidaId: string) => void;
  onScore?: (partidaId: string) => void;
};

export function TriviaLobbyScreen({ apiBaseUrl, token, partidaId, onLivePlay, onAnswer, onScore }: Props) {
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
    <Stage variant="indigo" gradient scroll>
      <Hero
        title={lobby?.nombre ?? "Sala de espera"}
        subtitle="Esperando el inicio de la partida (HU-18 · HU-21)."
        onStage
        right={pill ? <StatePill state={pill.state} label={pill.label} /> : undefined}
      />

      {loading ? <ActivityIndicator color={game.onStage} /> : null}
      {message ? <Notice variant="success">{message}</Notice> : null}
      {error ? <Notice variant="error">{error}</Notice> : null}

      {lobby ? <LobbyFill lobby={lobby} /> : null}

      <View style={styles.actions}>
        <Button label="Unirme individualmente" icon="user-plus" onPress={() => void handleJoin()} loading={joining} onStage />
        <Button label="Jugar partida en vivo (demo)" icon="play" variant="secondary" onPress={() => onLivePlay?.(partidaId)} onStage />
        <Button label="Responder pregunta" variant="ghost" onPress={() => onAnswer?.(partidaId)} onStage />
        <Button label="Ver puntaje" variant="ghost" onPress={() => onScore?.(partidaId)} onStage />
      </View>
    </Stage>
  );
}

/** Sala "llenándose": el conteo de participantes hace count-up hacia el valor real. */
function LobbyFill({ lobby }: { lobby: TriviaLobbyResponse }) {
  const count = useCountUp(lobby.participantesActual);
  return (
    <Panel>
      <View style={styles.fillRow}>
        <AppText style={styles.fillNumber} color={game.onStage} allowFontScaling={false}>
          {count}
        </AppText>
        <AppText variant="title" color={game.onStageMuted}>
          participantes
        </AppText>
      </View>
      <DetailRow label="Modalidad" value={lobby.modalidad} onStage />
      <DetailRow label="Mínimo para iniciar" value={String(lobby.minimoParticipantes)} onStage />
    </Panel>
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
  actions: {
    gap: spacing.sm,
  },
  fillRow: {
    flexDirection: "row",
    alignItems: "baseline",
    gap: spacing.sm,
  },
  fillNumber: {
    ...typography.hero,
    fontSize: 56,
    lineHeight: 60,
    color: game.onStage,
  },
});
