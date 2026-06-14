import React, { useState } from "react";
import { SafeAreaView, ScrollView, StyleSheet, View } from "react-native";
import { AppText, Button, Card, DetailRow, Field, Notice, StatePill } from "../../../shared/ui";
import { colors, fonts, spacing } from "../../../shared/theme";
import { TriviaAnswerResponse, TriviaMobileApiError, answerTriviaQuestion } from "../../../api/triviaApi";

type Props = {
  apiBaseUrl: string;
  token: string;
  partidaId: string;
  onResult?: (partidaId: string, preguntaId: string) => void;
};

export function TriviaAnswerScreen({ apiBaseUrl, token, partidaId, onResult }: Props) {
  const [preguntaId, setPreguntaId] = useState("");
  const [opcionIndex, setOpcionIndex] = useState("0");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [answer, setAnswer] = useState<TriviaAnswerResponse | null>(null);

  async function handleSubmit() {
    if (!preguntaId.trim()) {
      setError("Indica el ID de la pregunta activa.");
      return;
    }

    setLoading(true);
    setError(null);
    setAnswer(null);
    try {
      setAnswer(
        await answerTriviaQuestion({
          apiBaseUrl,
          token,
          partidaId,
          preguntaId: preguntaId.trim(),
          opcionIndex: Number(opcionIndex),
        }),
      );
    } catch (caught) {
      setError(mapError(caught));
    } finally {
      setLoading(false);
    }
  }

  return (
    <SafeAreaView style={styles.safe}>
      <ScrollView contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
        <View style={styles.head}>
          <AppText variant="display">Responder Trivia</AppText>
          <AppText variant="body" color={colors.muted}>
            HU-26 consume el contrato backend de respuesta individual. La pregunta activa se entrega por
            contrato futuro de ejecucion sincronizada.
          </AppText>
        </View>

        {error ? <Notice variant="error">{error}</Notice> : null}

        {answer ? (
          <Card>
            <StatePill
              state={answer.esCorrecta ? "ok" : "done"}
              label={answer.esCorrecta ? "Respuesta correcta" : "Respuesta registrada"}
            />
            <DetailRow label="Puntaje obtenido" value={String(answer.puntajeObtenido)} />
            <DetailRow label="Tiempo empleado" value={`${answer.tiempoEmpleadoSegundos}s`} />
          </Card>
        ) : null}

        <Card>
          <Field
            label="ID de pregunta"
            value={preguntaId}
            onChangeText={setPreguntaId}
            placeholder="uuid de pregunta activa"
            autoCapitalize="none"
            autoCorrect={false}
            style={styles.mono}
          />
          <Field
            label="Opcion seleccionada (0-3)"
            value={opcionIndex}
            onChangeText={setOpcionIndex}
            keyboardType="number-pad"
          />
          <Button label="Enviar respuesta" onPress={() => void handleSubmit()} loading={loading} />
        </Card>

        <Button
          label="Ver resultado de pregunta"
          variant="secondary"
          onPress={() => preguntaId.trim() && onResult?.(partidaId, preguntaId.trim())}
        />
      </ScrollView>
    </SafeAreaView>
  );
}

function mapError(caught: unknown): string {
  if (caught instanceof TriviaMobileApiError) {
    if (caught.status === 400) return caught.message || "Respuesta repetida, tardia o fuera de la pregunta activa.";
    return caught.message;
  }

  return "No se pudo enviar la respuesta.";
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
  mono: {
    fontFamily: fonts.mono,
  },
});
