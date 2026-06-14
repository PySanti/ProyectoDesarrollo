import React, { useEffect, useState } from "react";
import { ActivityIndicator, StyleSheet, View } from "react-native";
import { AppText, Button, Hero, Icon, Podium, PodiumEntry, Stage } from "../../../shared/ui";
import { game, radius, spacing } from "../../../shared/theme";
import { BdtRankingEntry, BdtRankingSource, formatBdtRankingValue } from "./bdtRankingTypes";

type Props = {
  /** Fuente del ranking (mock hoy; `BackendBdtRankingSource` cuando exista). Ver `bdtRankingTypes.ts`. */
  source: BdtRankingSource;
  onExit?: () => void;
};

/**
 * Maqueta del **ranking BDT** (G4). Depende solo de `BdtRankingSource`; sirve de plantilla de
 * integración. Aplica el `Podium` con el criterio de BDT: ordena por **etapas ganadas** y desempata por
 * **tiempo acumulado** — el valor mostrado es "N etapas · m:ss", **no** puntaje.
 */
export function BdtRankingScreen({ source, onExit }: Props) {
  const [ranking, setRanking] = useState<BdtRankingEntry[] | null>(null);

  useEffect(() => {
    let active = true;
    void source.load().then((rows) => {
      if (active) setRanking(rows);
    });
    return () => {
      active = false;
    };
  }, [source]);

  const podio: PodiumEntry[] = (ranking ?? []).map((row) => ({
    posicion: row.posicion,
    participante: row.participante,
    valor: formatBdtRankingValue(row),
    esTu: row.esTu,
  }));

  return (
    <Stage variant="magenta" gradient scroll>
      <View style={styles.demoBanner}>
        <Icon name="alert-triangle" size={14} color={game.onStage} />
        <AppText variant="label" color={game.onStage}>
          Maqueta · datos de ejemplo (sin backend)
        </AppText>
      </View>

      <Hero title="Ranking BDT" subtitle="Por etapas ganadas; desempata el menor tiempo acumulado." onStage />

      {ranking === null ? <ActivityIndicator color={game.onStage} /> : <Podium entries={podio} />}

      <AppText variant="label" color={game.onStageMuted}>
        El orden NO es por puntaje: gana quien conquista más etapas; a igualdad de etapas, el menor tiempo.
      </AppText>

      <Button label="Volver" variant="secondary" onStage onPress={() => onExit?.()} />
    </Stage>
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
});
