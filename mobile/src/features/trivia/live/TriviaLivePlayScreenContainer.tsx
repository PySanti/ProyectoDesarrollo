import React, { useMemo } from "react";
import { NativeStackScreenProps } from "@react-navigation/native-stack";
import { AppStackParamList } from "../../../navigation/types";
import { TriviaLivePlayScreen } from "./TriviaLivePlayScreen";
import { createMockLiveTriviaSource } from "./mockLiveTriviaSource";

type Props = NativeStackScreenProps<AppStackParamList, "TriviaLivePlay">;

export function TriviaLivePlayScreenContainer({ navigation }: Props) {
  // TODO(backend): cuando exista la ejecución sincronizada, reemplazar el mock por una
  // `BackendLiveTriviaSource(apiBaseUrl, token, partidaId)` que cumpla `LiveTriviaSource`
  // (SignalR para el push de pregunta/cierre + los endpoints REST ya existentes). Ver
  // `liveTriviaTypes.ts`. La pantalla NO cambia: solo se cambia esta fuente.
  const source = useMemo(() => createMockLiveTriviaSource(), []);

  return <TriviaLivePlayScreen source={source} onExit={() => navigation.popToTop()} />;
}
