import React, { useCallback, useEffect, useRef, useState } from "react";
import { ActivityIndicator, StyleSheet, View } from "react-native";
import {
  AppText,
  Button,
  Countdown,
  Hero,
  Icon,
  Podium,
  PodiumEntry,
  PressableScale,
  Reaction,
  Stage,
} from "../../../shared/ui";
import { colors, game, radius, spacing, typography } from "../../../shared/theme";
import { useCountUp } from "../../../shared/useCountUp";
import { TriviaQuestionResultResponse } from "../../../api/triviaApi";
import { LiveFinalScore, LiveQuestion, LiveTriviaSource } from "./liveTriviaTypes";

type Phase = "connecting" | "question" | "result" | "final";

type Props = {
  /** Fuente de la partida (mock hoy; `BackendLiveTriviaSource` cuando exista). Ver `liveTriviaTypes.ts`. */
  source: LiveTriviaSource;
  onExit?: () => void;
};

const OPTION_LETTERS = ["A", "B", "C", "D", "E", "F"];

/**
 * Maqueta de **partida de Trivia en vivo** (G2). Depende solo de `LiveTriviaSource`, así que sirve de
 * plantilla: cuando exista el backend de ejecución sincronizada, se inyecta otra fuente y esta pantalla
 * no cambia. Demuestra los momentos estelares: cuenta regresiva dramática, reacción correcto/incorrecto
 * y puntaje con count-up. El timer local es de la maqueta; el real lo sincroniza el servidor.
 */
export function TriviaLivePlayScreen({ source, onExit }: Props) {
  const [phase, setPhase] = useState<Phase>("connecting");
  const [question, setQuestion] = useState<LiveQuestion | null>(null);
  const [remaining, setRemaining] = useState(0);
  const [submitting, setSubmitting] = useState(false);
  const [reveal, setReveal] = useState<TriviaQuestionResultResponse | null>(null);
  const [final, setFinal] = useState<LiveFinalScore | null>(null);

  // `ref` para que el tick del timer no dependa de closures viejos al auto-responder al agotar el tiempo.
  const submitRef = useRef<(opcionIndex: number | null) => void>(() => {});

  const handleAnswer = useCallback(
    async (opcionIndex: number | null) => {
      if (submitting || !question) return;
      setSubmitting(true);
      try {
        await source.submit(question.preguntaId, opcionIndex);
        setReveal(await source.questionResult(question.preguntaId));
        setPhase("result");
      } finally {
        setSubmitting(false);
      }
    },
    [source, question, submitting],
  );
  submitRef.current = (opcionIndex) => void handleAnswer(opcionIndex);

  // Suscripción a la pregunta activa (real: push SignalR). `null` = fin de partida → puntaje + ranking.
  useEffect(() => {
    const unsub = source.onQuestion((next) => {
      if (next) {
        setReveal(null);
        setQuestion(next);
        setRemaining(next.limiteSegundos);
        setPhase("question");
      } else {
        setQuestion(null);
        void source.finalScore().then((f) => {
          setFinal(f);
          setPhase("final");
        });
      }
    });
    source.start();
    return () => {
      unsub();
      source.stop();
    };
  }, [source]);

  // Cuenta regresiva local de la maqueta (real: sincronizada a la deadline del servidor). Al llegar a 0
  // sin responder, se envía `null` (tiempo agotado).
  useEffect(() => {
    if (phase !== "question") return;
    if (remaining <= 0) {
      submitRef.current(null);
      return;
    }
    const id = setTimeout(() => setRemaining((r) => r - 1), 1000);
    return () => clearTimeout(id);
  }, [phase, remaining]);

  function optionState(index: number): "correct" | "wrong" | "idle" {
    if (!reveal) return "idle";
    if (index === reveal.opcionCorrectaIndex) return "correct";
    if (index === reveal.miOpcionIndex) return "wrong";
    return "idle";
  }

  return (
    <Stage variant="magenta" gradient scroll>
      <View style={styles.demoBanner}>
        <Icon name="alert-triangle" size={14} color={game.onStage} />
        <AppText variant="label" color={game.onStage}>
          Maqueta · datos de ejemplo (sin backend)
        </AppText>
      </View>

      {phase === "connecting" ? (
        <View style={styles.center}>
          <ActivityIndicator color={game.onStage} />
          <AppText variant="body" color={game.onStageMuted}>
            Conectando a la partida…
          </AppText>
        </View>
      ) : null}

      {question && (phase === "question" || phase === "result") ? (
        <>
          <AppText variant="label" color={game.onStageMuted}>
            Pregunta {question.index} de {question.total}
          </AppText>

          {phase === "question" ? (
            <Countdown seconds={remaining} label="segundos" />
          ) : reveal ? (
            <Reaction
              correct={reveal.esCorrecta === true}
              title={
                reveal.esCorrecta === true
                  ? "¡Correcta!"
                  : reveal.miOpcionIndex === null
                    ? "Tiempo agotado"
                    : "Incorrecta"
              }
              subtitle={`+${reveal.puntajeObtenido} puntos`}
            />
          ) : null}

          <AppText variant="headline" color={game.onStage} style={styles.questionText}>
            {question.texto}
          </AppText>

          <View style={styles.options}>
            {question.opciones.map((texto, index) => (
              <OptionButton
                key={index}
                letter={OPTION_LETTERS[index] ?? String(index + 1)}
                texto={texto}
                state={optionState(index)}
                disabled={phase !== "question" || submitting}
                onPress={() => void handleAnswer(index)}
              />
            ))}
          </View>

          {phase === "result" ? (
            <Button
              label={question.index >= question.total ? "Ver resultados" : "Siguiente pregunta"}
              icon="arrow-right"
              onStage
              onPress={() => source.advance()}
            />
          ) : null}
        </>
      ) : null}

      {phase === "final" && final ? <FinalBlock final={final} onExit={onExit} /> : null}
    </Stage>
  );
}

function OptionButton({
  letter,
  texto,
  state,
  disabled,
  onPress,
}: {
  letter: string;
  texto: string;
  state: "correct" | "wrong" | "idle";
  disabled?: boolean;
  onPress: () => void;
}) {
  const tint = state === "correct" ? colors.success : state === "wrong" ? colors.danger : null;
  return (
    <PressableScale
      onPress={onPress}
      disabled={disabled}
      accessibilityRole="button"
      accessibilityLabel={texto}
      style={[styles.option, tint ? { borderColor: tint, backgroundColor: tintWash(state) } : null]}
    >
      <View style={[styles.optionLetter, tint ? { borderColor: tint } : null]}>
        <AppText variant="bodyStrong" color={tint ?? game.onStage}>
          {letter}
        </AppText>
      </View>
      <AppText variant="bodyStrong" color={game.onStage} style={styles.optionText}>
        {texto}
      </AppText>
      {state === "correct" ? <Icon name="check" size={18} color={colors.success} /> : null}
      {state === "wrong" ? <Icon name="x" size={18} color={colors.danger} /> : null}
    </PressableScale>
  );
}

function tintWash(state: "correct" | "wrong" | "idle"): string {
  if (state === "correct") return "rgba(28,135,66,0.18)";
  if (state === "wrong") return "rgba(204,39,46,0.18)";
  return game.onStageSunk;
}

/** Cierre: puntaje con count-up + **podio** del ranking (G3). */
function FinalBlock({ final, onExit }: { final: LiveFinalScore; onExit?: () => void }) {
  const value = useCountUp(final.score.puntajeAcumulado);
  const podio: PodiumEntry[] = final.ranking.map((row) => ({
    posicion: row.posicion,
    participante: row.participante,
    valor: `${row.puntaje} pts`,
    esTu: row.esTu,
  }));
  return (
    <View style={styles.finalBlock}>
      <Hero title="Resultados" subtitle={`Terminaste en posición ${final.posicion}.`} onStage />

      <View style={styles.scoreWrap}>
        <AppText style={styles.scoreNumber} color={game.onStage} allowFontScaling={false}>
          {value}
        </AppText>
        <AppText variant="label" color={game.onStageMuted}>
          puntos · {final.score.respuestasCorrectas}/{final.score.totalRespuestas} correctas
        </AppText>
      </View>

      <Podium entries={podio} />

      <Button label="Volver" variant="secondary" onStage onPress={() => onExit?.()} />
    </View>
  );
}

const styles = StyleSheet.create({
  demoBanner: {
    flexDirection: "row",
    alignItems: "center",
    alignSelf: "flex-start",
    gap: spacing.xs + 2,
    backgroundColor: game.onStageSunk,
    borderWidth: 1,
    borderColor: game.onStageLine,
    borderRadius: radius.pill,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.xs,
  },
  center: {
    alignItems: "center",
    gap: spacing.md,
    paddingVertical: spacing.xxl,
  },
  questionText: {
    marginTop: spacing.xs,
  },
  options: {
    gap: spacing.sm,
  },
  option: {
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.md,
    backgroundColor: game.onStageSunk,
    borderWidth: 1,
    borderColor: game.onStageLine,
    borderRadius: radius.lg,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.md,
    minHeight: 56,
  },
  optionLetter: {
    width: 32,
    height: 32,
    borderRadius: radius.md,
    borderWidth: 1,
    borderColor: game.onStageLine,
    alignItems: "center",
    justifyContent: "center",
  },
  optionText: {
    flex: 1,
  },
  finalBlock: {
    gap: spacing.xl,
  },
  scoreWrap: {
    alignItems: "center",
    gap: spacing.xs,
  },
  scoreNumber: {
    ...typography.mega,
    fontSize: 88,
    lineHeight: 92,
    color: game.onStage,
  },
});
