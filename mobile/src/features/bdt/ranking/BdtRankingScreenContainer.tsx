import React, { useMemo } from "react";
import { NativeStackScreenProps } from "@react-navigation/native-stack";
import { AppStackParamList } from "../../../navigation/types";
import { BdtRankingScreen } from "./BdtRankingScreen";
import { createMockBdtRankingSource } from "./mockBdtRankingSource";

type Props = NativeStackScreenProps<AppStackParamList, "BdtRanking">;

export function BdtRankingScreenContainer({ navigation }: Props) {
  // TODO(backend): reemplazar el mock por una `BackendBdtRankingSource(apiBaseUrl, token, partidaId)` que
  // cumpla `BdtRankingSource` (endpoint/evento de ranking de Puntuaciones, orden por puntaje acumulado de
  // etapas ganadas; desempate por tiempo). Ver `bdtRankingTypes.ts`. La pantalla NO cambia: solo la fuente.
  const source = useMemo(() => createMockBdtRankingSource(), []);

  return <BdtRankingScreen source={source} onExit={() => navigation.goBack()} />;
}
