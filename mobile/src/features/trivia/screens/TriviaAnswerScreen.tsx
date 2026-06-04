import React, { useState } from "react";
import { Pressable, StyleSheet, Text, TextInput, View } from "react-native";
import ScreenWrapper from "../../../shared/components/ScreenWrapper";
import { screenStyles } from "../../../shared/styles";
import { colors } from "../../../shared/theme";
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
      setAnswer(await answerTriviaQuestion({ apiBaseUrl, token, partidaId, preguntaId: preguntaId.trim(), opcionIndex: Number(opcionIndex) }));
    } catch (caught) {
      setError(mapError(caught));
    } finally {
      setLoading(false);
    }
  }

  return (
    <ScreenWrapper>
      <View style={styles.container}>
        <Text style={styles.title}>Responder Trivia</Text>
        <Text style={styles.description}>HU-26 consume el contrato backend de respuesta individual. La pregunta activa se entrega por contrato futuro de ejecucion sincronizada.</Text>
        {error ? <Text style={styles.error}>{error}</Text> : null}
        {answer ? (
          <View style={styles.card}>
            <Text style={styles.cardTitle}>{answer.esCorrecta ? "Respuesta correcta" : "Respuesta registrada"}</Text>
            <Text style={styles.cardLine}>Puntaje obtenido: {answer.puntajeObtenido}</Text>
            <Text style={styles.cardLine}>Tiempo empleado: {answer.tiempoEmpleadoSegundos}s</Text>
          </View>
        ) : null}
        <Text style={styles.label}>ID de pregunta</Text>
        <TextInput style={styles.input} value={preguntaId} onChangeText={setPreguntaId} placeholder="uuid de pregunta activa" autoCapitalize="none" />
        <Text style={styles.label}>Opcion seleccionada (0-3)</Text>
        <TextInput style={styles.input} value={opcionIndex} onChangeText={setOpcionIndex} keyboardType="number-pad" />
        <Pressable style={styles.joinButton} onPress={() => void handleSubmit()} disabled={loading}>
          <Text style={styles.joinButtonText}>{loading ? "Enviando..." : "Enviar respuesta"}</Text>
        </Pressable>
        <Pressable style={styles.secondaryButton} onPress={() => preguntaId.trim() && onResult?.(partidaId, preguntaId.trim())}>
          <Text style={styles.secondaryButtonText}>Ver resultado de pregunta</Text>
        </Pressable>
      </View>
    </ScreenWrapper>
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
  container: screenStyles.scrollContainer,
  title: screenStyles.title,
  description: screenStyles.description,
  card: screenStyles.card,
  cardTitle: screenStyles.cardTitle,
  cardLine: screenStyles.cardLine,
  error: screenStyles.error,
  label: { color: colors.text, fontWeight: "700", marginTop: 10 },
  input: screenStyles.input,
  joinButton: screenStyles.joinButton,
  joinButtonText: screenStyles.joinButtonText,
  secondaryButton: { ...screenStyles.filterButton, marginTop: 10 },
  secondaryButtonText: screenStyles.filterText,
});
