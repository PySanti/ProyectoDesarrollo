import React, { useCallback, useEffect, useState } from "react";
import { ActivityIndicator, SafeAreaView, ScrollView, StyleSheet, View } from "react-native";
import { AppText, Card, DetailRow, Notice } from "../../../shared/ui";
import { colors, spacing } from "../../../shared/theme";
import { TriviaMobileApiError, TriviaScoreResponse, getTriviaScore } from "../../../api/triviaApi";

type Props = {
  apiBaseUrl: string;
  token: string;
  partidaId: string;
};

export function TriviaScoreScreen({ apiBaseUrl, token, partidaId }: Props) {
  const [score, setScore] = useState<TriviaScoreResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadScore = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setScore(await getTriviaScore({ apiBaseUrl, token, partidaId }));
    } catch (caught) {
      setError(caught instanceof TriviaMobileApiError ? caught.message : "No se pudo cargar el puntaje.");
    } finally {
      setLoading(false);
    }
  }, [apiBaseUrl, partidaId, token]);

  useEffect(() => {
    void loadScore();
  }, [loadScore]);

  return (
    <SafeAreaView style={styles.safe}>
      <ScrollView contentContainerStyle={styles.content}>
        <View style={styles.head}>
          <AppText variant="display">Puntaje Trivia</AppText>
          <AppText variant="body" color={colors.muted}>
            HU-29 muestra el puntaje acumulado calculado por Trivia Game Service.
          </AppText>
        </View>

        {loading ? <ActivityIndicator color={colors.primaryFill} /> : null}
        {error ? <Notice variant="error">{error}</Notice> : null}

        {score ? (
          <Card>
            <View style={styles.scoreRow}>
              <AppText variant="display" color={colors.primaryStrong} style={styles.scoreNumber}>
                {score.puntajeAcumulado}
              </AppText>
              <AppText variant="title" color={colors.muted}>
                puntos
              </AppText>
            </View>
            <DetailRow label="Correctas" value={String(score.respuestasCorrectas)} />
            <DetailRow label="Respuestas totales" value={String(score.totalRespuestas)} />
            <DetailRow label="Tiempo acumulado" value={`${score.tiempoAcumuladoSegundos}s`} />
          </Card>
        ) : null}
      </ScrollView>
    </SafeAreaView>
  );
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
  scoreRow: {
    flexDirection: "row",
    alignItems: "baseline",
    gap: spacing.sm,
  },
  scoreNumber: {
    fontSize: 40,
    lineHeight: 44,
  },
});
