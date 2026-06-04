import React, { useCallback, useEffect, useState } from "react";
import { ActivityIndicator, StyleSheet, Text, View } from "react-native";
import ScreenWrapper from "../../../shared/components/ScreenWrapper";
import { screenStyles } from "../../../shared/styles";
import { colors } from "../../../shared/theme";
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
    <ScreenWrapper>
      <View style={styles.container}>
        <Text style={styles.title}>Puntaje Trivia</Text>
        <Text style={styles.description}>HU-29 muestra el puntaje acumulado calculado por Trivia Game Service.</Text>
        {loading ? <ActivityIndicator color={colors.primary} /> : null}
        {error ? <Text style={styles.error}>{error}</Text> : null}
        {score ? (
          <View style={styles.card}>
            <Text style={styles.cardTitle}>{score.puntajeAcumulado} puntos</Text>
            <Text style={styles.cardLine}>Correctas: {score.respuestasCorrectas}</Text>
            <Text style={styles.cardLine}>Respuestas totales: {score.totalRespuestas}</Text>
            <Text style={styles.cardLine}>Tiempo acumulado: {score.tiempoAcumuladoSegundos}s</Text>
          </View>
        ) : null}
      </View>
    </ScreenWrapper>
  );
}

const styles = StyleSheet.create({
  container: screenStyles.scrollContainer,
  title: screenStyles.title,
  description: screenStyles.description,
  card: screenStyles.card,
  cardTitle: screenStyles.cardTitle,
  cardLine: screenStyles.cardLine,
  error: screenStyles.error,
});
