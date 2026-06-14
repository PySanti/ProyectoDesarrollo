import React from 'react';
import { StyleSheet, View } from 'react-native';
import { colors, game, radius, spacing, typography } from '../theme';
import { AppText } from './AppText';
import { Icon } from './Icon';

export interface PodiumEntry {
  posicion: number;
  participante: string;
  /**
   * Valor ya **formateado por quien llama** (p. ej. "300 pts" en Trivia, o "3 etapas · 4:12" en BDT).
   * El podio NO asume puntaje: solo lo muestra. Así sirve para Trivia (puntaje) y BDT (etapas/tiempo).
   */
  valor: string;
  /** Resalta la fila del propio participante. */
  esTu?: boolean;
  /** Cambio de posición respecto a un estado previo (opcional). >0 sube, <0 baja. */
  delta?: number;
}

interface Props {
  entries: PodiumEntry[];
  /** Sobre un `Stage` de color (texto claro). Por defecto `true`. */
  onStage?: boolean;
}

/** Alturas de pilar por puesto: 1.º el más alto. La jerarquía la dan tamaño y elevación, no colores nuevos. */
const BAR_HEIGHT: Record<number, number> = { 1: 96, 2: 72, 3: 56 };

/**
 * Podio del ranking (clímax competitivo): top-3 en pilares de distinta altura (2.º · 1.º · 3.º), el
 * resto como lista. Resalta al propio participante ("tú"). **Agnóstico del criterio**: muestra el
 * `valor` ya formateado, sin asumir puntaje — reutilizable por BDT (ordenado por etapas/tiempo).
 * Sin colores nuevos: rango = altura + número; "tú" = contorno claro + chip.
 */
export function Podium({ entries, onStage = true }: Props) {
  const sorted = [...entries].sort((a, b) => a.posicion - b.posicion);
  const top = sorted.filter((e) => e.posicion <= 3);
  const rest = sorted.filter((e) => e.posicion > 3);

  const first = top.find((e) => e.posicion === 1);
  const second = top.find((e) => e.posicion === 2);
  const third = top.find((e) => e.posicion === 3);

  return (
    <View style={styles.wrap}>
      <View style={styles.podiumRow}>
        {second ? <Pillar entry={second} onStage={onStage} /> : <View style={styles.pillarSlot} />}
        {first ? <Pillar entry={first} onStage={onStage} /> : <View style={styles.pillarSlot} />}
        {third ? <Pillar entry={third} onStage={onStage} /> : <View style={styles.pillarSlot} />}
      </View>

      {rest.length > 0 ? (
        <View style={styles.list}>
          {rest.map((e) => (
            <ListRow key={e.posicion} entry={e} onStage={onStage} />
          ))}
        </View>
      ) : null}
    </View>
  );
}

function Pillar({ entry, onStage }: { entry: PodiumEntry; onStage: boolean }) {
  const fg = onStage ? game.onStage : colors.ink;
  const muted = onStage ? game.onStageMuted : colors.muted;
  return (
    <View style={styles.pillarSlot}>
      <AppText variant="bodyStrong" color={fg} numberOfLines={1} style={styles.name}>
        {entry.participante}
      </AppText>
      <AppText variant="label" color={muted} numberOfLines={1}>
        {entry.valor}
      </AppText>
      {entry.esTu ? (
        <View style={styles.youChip}>
          <AppText variant="label" color={fg}>
            Tú
          </AppText>
        </View>
      ) : (
        <View style={styles.youChipSpacer} />
      )}
      <View
        style={[
          styles.bar,
          { height: BAR_HEIGHT[entry.posicion] ?? 56 },
          entry.esTu ? styles.barYou : null,
        ]}
      >
        <AppText style={[typography.hero, { color: fg }]} allowFontScaling={false}>
          {entry.posicion}
        </AppText>
      </View>
    </View>
  );
}

function ListRow({ entry, onStage }: { entry: PodiumEntry; onStage: boolean }) {
  const fg = entry.esTu ? (onStage ? game.onStage : colors.ink) : onStage ? game.onStageMuted : colors.inkSoft;
  return (
    <View style={[styles.row, entry.esTu ? styles.rowYou : null]}>
      <AppText variant="bodyStrong" color={fg} style={styles.rowName} numberOfLines={1}>
        {entry.posicion}. {entry.participante}
        {entry.esTu ? "  ·  Tú" : ""}
      </AppText>
      {typeof entry.delta === 'number' && entry.delta !== 0 ? <Delta delta={entry.delta} onStage={onStage} /> : null}
      <AppText variant="bodyStrong" color={fg}>
        {entry.valor}
      </AppText>
    </View>
  );
}

function Delta({ delta, onStage }: { delta: number; onStage: boolean }) {
  const up = delta > 0;
  const color = onStage ? game.onStageMuted : colors.muted;
  return (
    <View style={styles.delta}>
      <Icon name={up ? 'chevron-up' : 'chevron-down'} size={14} color={color} />
      <AppText variant="label" color={color}>
        {Math.abs(delta)}
      </AppText>
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: {
    gap: spacing.lg,
  },
  podiumRow: {
    flexDirection: 'row',
    alignItems: 'flex-end',
    justifyContent: 'center',
    gap: spacing.sm,
  },
  pillarSlot: {
    flex: 1,
    alignItems: 'center',
    gap: spacing.xs,
  },
  name: {
    textAlign: 'center',
  },
  youChip: {
    backgroundColor: game.onStageSunk,
    borderWidth: 1,
    borderColor: game.onStage,
    borderRadius: radius.pill,
    paddingHorizontal: spacing.sm,
    paddingVertical: 1,
  },
  youChipSpacer: {
    height: 20,
  },
  bar: {
    alignSelf: 'stretch',
    backgroundColor: game.onStageSunk,
    borderWidth: 1,
    borderColor: game.onStageLine,
    borderTopLeftRadius: radius.md,
    borderTopRightRadius: radius.md,
    alignItems: 'center',
    justifyContent: 'center',
  },
  barYou: {
    borderColor: game.onStage,
    borderWidth: 2,
  },
  list: {
    gap: spacing.xs,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    paddingVertical: spacing.sm,
    paddingHorizontal: spacing.md,
    borderRadius: radius.md,
  },
  rowYou: {
    backgroundColor: game.onStageSunk,
    borderWidth: 1,
    borderColor: game.onStageLine,
  },
  rowName: {
    flex: 1,
  },
  delta: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 2,
  },
});
