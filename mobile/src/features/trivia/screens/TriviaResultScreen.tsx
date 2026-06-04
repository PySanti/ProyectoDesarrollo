import React, { useCallback, useEffect, useState } from "react";
import { ActivityIndicator, StyleSheet, Text, View } from "react-native";
import ScreenWrapper from "../../../shared/components/ScreenWrapper";
import { screenStyles } from "../../../shared/styles";
import { colors } from "../../../shared/theme";
import { TriviaMobileApiError, TriviaQuestionResultResponse, getTriviaQuestionResult } from "../../../api/triviaApi";

type Props = {
  apiBaseUrl: string;
  token: string;
  partidaId: string;
  preguntaId: string;
};

export function TriviaResultScreen({ apiBaseUrl, token, partidaId, preguntaId }: Props) {
  const [result, setResult] = useState<TriviaQuestionResultResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadResult = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setResult(await getTriviaQuestionResult({ apiBaseUrl, token, partidaId, preguntaId }));
    } catch (caught) {
      setError(caught instanceof TriviaMobileApiError ? caught.message : "No se pudo cargar el resultado.");
    } finally {
      setLoading(false);
    }
  }, [apiBaseUrl, partidaId, preguntaId, token]);

  useEffect(() => {
    void loadResult();
  }, [loadResult]);

  return (
    <ScreenWrapper>
      <View style={styles.container}>
        <Text style={styles.title}>Resultado de pregunta</Text>
        <Text style={styles.description}>HU-28 muestra la respuesta correcta enviada por backend.</Text>
        {loading ? <ActivityIndicator color={colors.primary} /> : null}
        {error ? <Text style={styles.error}>{error}</Text> : null}
        {result ? (
          <View style={styles.card}>
            <Text style={styles.cardTitle}>{result.textoPregunta}</Text>
            <Text style={styles.cardLine}>Correcta: {result.opcionCorrectaText}</Text>
            <Text style={styles.cardLine}>Tu respuesta: {result.miOpcionText ?? "Sin respuesta"}</Text>
            <Text style={styles.cardLine}>Resultado: {result.esCorrecta ? "Correcta" : "Incorrecta"}</Text>
            <Text style={styles.cardLine}>Puntaje: {result.puntajeObtenido}</Text>
            <Text style={styles.cardLine}>Cierre: {result.motivoCierre}</Text>
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
