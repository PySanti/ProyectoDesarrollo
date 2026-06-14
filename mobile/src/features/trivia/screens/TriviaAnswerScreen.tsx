import React, { useState } from "react";
import { StyleSheet } from "react-native";
import { AppText, Button, Card, DetailRow, Field, Hero, Notice, Panel, Reaction, Stage } from "../../../shared/ui";
import { fonts, game, spacing } from "../../../shared/theme";
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
    <Stage variant="magenta" gradient scroll>
      <Hero
        title="Responde"
        subtitle="HU-26 envía tu respuesta individual al backend autoritativo."
        onStage
      />

      {error ? <Notice variant="error">{error}</Notice> : null}

      {answer ? (
        <Panel>
          <Reaction
            correct={answer.esCorrecta}
            title={answer.esCorrecta ? "¡Respuesta correcta!" : "Respuesta registrada"}
            subtitle={answer.esCorrecta ? "Sumaste puntos en esta pregunta." : "Tu respuesta quedó registrada."}
          />
          <DetailRow label="Puntaje obtenido" value={String(answer.puntajeObtenido)} onStage />
          <DetailRow label="Tiempo empleado" value={`${answer.tiempoEmpleadoSegundos}s`} onStage />
        </Panel>
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
        <Button label="Enviar respuesta" icon="send" onPress={() => void handleSubmit()} loading={loading} />
      </Card>

      <Button
        label="Ver resultado de pregunta"
        variant="secondary"
        onStage
        onPress={() => preguntaId.trim() && onResult?.(partidaId, preguntaId.trim())}
      />

      <AppText variant="label" color={game.onStageMuted} style={styles.note}>
        La pregunta activa se entrega por contrato futuro de ejecución sincronizada.
      </AppText>
    </Stage>
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
  mono: {
    fontFamily: fonts.mono,
  },
  note: {
    marginTop: spacing.xs,
  },
});
