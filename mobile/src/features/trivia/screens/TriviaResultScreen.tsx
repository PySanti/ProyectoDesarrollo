import React, { useCallback, useEffect, useState } from "react";
import { ActivityIndicator } from "react-native";
import { AppText, DetailRow, Notice, Panel, Reaction, Stage } from "../../../shared/ui";
import { game } from "../../../shared/theme";
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
    <Stage variant="ink" gradient scroll>
      {loading ? <ActivityIndicator color={game.onStage} /> : null}
      {error ? <Notice variant="error">{error}</Notice> : null}

      {result ? (
        <>
          <Reaction
            correct={result.esCorrecta === true}
            title={result.esCorrecta ? "¡Correcta!" : "Incorrecta"}
            subtitle={result.textoPregunta}
          />
          <Panel>
            <DetailRow label="Correcta" value={result.opcionCorrectaText} onStage />
            <DetailRow label="Tu respuesta" value={result.miOpcionText ?? "Sin respuesta"} onStage />
            <DetailRow label="Puntaje" value={String(result.puntajeObtenido)} onStage />
            <DetailRow label="Cierre" value={result.motivoCierre} onStage />
          </Panel>
          <AppText variant="body" color={game.onStageMuted}>
            HU-28 muestra la respuesta correcta enviada por backend.
          </AppText>
        </>
      ) : null}
    </Stage>
  );
}
