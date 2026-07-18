// Piezas compartidas del live del participante (Trivia 2e-1 + BDT 2e-2).
import React, { useEffect, useState } from "react";
import { StyleSheet, View } from "react-native";
import { AppText } from "../../shared/ui";
import { colors, spacing } from "../../shared/theme";
import { etiquetaCompetidor } from "./liveLabels.js";

export function Countdown({ target, expiredLabel = "Tiempo agotado" }: { target: string; expiredLabel?: string }) {
  const [now, setNow] = useState(() => Date.now());
  useEffect(() => {
    const id = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(id);
  }, []);
  const remaining = Math.max(0, Math.floor((new Date(target).getTime() - now) / 1000));
  const mm = String(Math.floor(remaining / 60)).padStart(2, "0");
  const ss = String(remaining % 60).padStart(2, "0");
  return <AppText>{remaining > 0 ? `⏱ ${mm}:${ss}` : expiredLabel}</AppText>;
}

export type RankingEntrada = {
  posicion: number;
  competidorId: string;
  puntos: number;
  juegosGanados?: number;
  tipoCompetidor?: "Participante" | "Equipo";
  // Solo en el consolidado: por qué ganó el desempate (ej. "por menor tiempo").
  motivoDesempate?: string;
};

export function RankingTable({
  entradas,
  resaltarId,
  nombreDe,
}: {
  entradas: RankingEntrada[];
  resaltarId?: string;
  nombreDe: (id: string) => string;
}) {
  if (!entradas?.length) {
    return <AppText>Sin datos de ranking todavía.</AppText>;
  }
  return (
    <View style={styles.tabla}>
      {entradas.map((e) => (
        <View key={e.competidorId} style={[styles.item, e.competidorId === resaltarId ? styles.propia : null]}>
          <View style={styles.fila}>
            <AppText variant="bodyStrong">#{e.posicion}</AppText>
            <AppText>{etiquetaCompetidor(e.competidorId, resaltarId, nombreDe)}</AppText>
            {e.juegosGanados != null ? <AppText>{e.juegosGanados} 🏆</AppText> : null}
            <AppText variant="bodyStrong">{e.puntos} pts</AppText>
          </View>
          {e.motivoDesempate ? (
            <AppText variant="label" color={colors.muted} style={styles.motivo}>
              Desempate: ganó {e.motivoDesempate}
            </AppText>
          ) : null}
        </View>
      ))}
    </View>
  );
}

const styles = StyleSheet.create({
  tabla: { gap: spacing.xs },
  item: { borderRadius: 6 },
  fila: { flexDirection: "row", justifyContent: "space-between", paddingVertical: spacing.xs },
  propia: { backgroundColor: colors.primaryBright + "22", paddingHorizontal: spacing.xs },
  motivo: { paddingHorizontal: spacing.xs, paddingBottom: spacing.xs },
});
