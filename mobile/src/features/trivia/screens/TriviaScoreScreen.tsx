import React, { useCallback, useEffect, useState } from "react";
import { ActivityIndicator, StyleSheet, View } from "react-native";
import { AppText, DetailRow, Hero, Notice, Panel, Stage } from "../../../shared/ui";
import { game, spacing, typography } from "../../../shared/theme";
import { useCountUp } from "../../../shared/useCountUp";
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
    <Stage variant="magenta" gradient scroll>
      <Hero title="Tu puntaje" subtitle="Lo calcula Trivia Game Service (HU-29)." onStage />

      {loading ? <ActivityIndicator color={game.onStage} /> : null}
      {error ? <Notice variant="error">{error}</Notice> : null}

      {score ? <ScoreBlock score={score} /> : null}
    </Stage>
  );
}

/** Puntaje protagonista con count-up; detalles de apoyo en panel translúcido. */
function ScoreBlock({ score }: { score: TriviaScoreResponse }) {
  const value = useCountUp(score.puntajeAcumulado);
  return (
    <View style={styles.block}>
      <View style={styles.scoreWrap}>
        <AppText style={styles.scoreNumber} color={game.onStage} allowFontScaling={false}>
          {value}
        </AppText>
        <AppText variant="label" color={game.onStageMuted}>
          puntos acumulados
        </AppText>
      </View>

      <Panel>
        <DetailRow label="Correctas" value={String(score.respuestasCorrectas)} onStage />
        <DetailRow label="Respuestas totales" value={String(score.totalRespuestas)} onStage />
        <DetailRow label="Tiempo acumulado" value={`${score.tiempoAcumuladoSegundos}s`} onStage />
      </Panel>
    </View>
  );
}

const styles = StyleSheet.create({
  block: {
    gap: spacing.xl,
  },
  scoreWrap: {
    alignItems: "center",
    gap: spacing.xs,
    paddingVertical: spacing.lg,
  },
  scoreNumber: {
    ...typography.mega,
    fontSize: 88,
    lineHeight: 92,
    color: game.onStage,
  },
});
