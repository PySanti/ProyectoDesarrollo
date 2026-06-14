import React, { useCallback, useEffect, useState } from "react";
import { ActivityIndicator, SafeAreaView, ScrollView, StyleSheet, View } from "react-native";
import { AppText, Card, DetailRow, Notice, StatePill } from "../../../shared/ui";
import { colors, spacing } from "../../../shared/theme";
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
    <SafeAreaView style={styles.safe}>
      <ScrollView contentContainerStyle={styles.content}>
        <View style={styles.head}>
          <AppText variant="display">Resultado de pregunta</AppText>
          <AppText variant="body" color={colors.muted}>
            HU-28 muestra la respuesta correcta enviada por backend.
          </AppText>
        </View>

        {loading ? <ActivityIndicator color={colors.primaryFill} /> : null}
        {error ? <Notice variant="error">{error}</Notice> : null}

        {result ? (
          <Card>
            <AppText variant="title">{result.textoPregunta}</AppText>
            <StatePill
              state={result.esCorrecta ? "ok" : "cancel"}
              label={result.esCorrecta ? "Correcta" : "Incorrecta"}
            />
            <DetailRow label="Correcta" value={result.opcionCorrectaText} />
            <DetailRow label="Tu respuesta" value={result.miOpcionText ?? "Sin respuesta"} />
            <DetailRow label="Puntaje" value={String(result.puntajeObtenido)} />
            <DetailRow label="Cierre" value={result.motivoCierre} />
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
});
