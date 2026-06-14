import React from 'react';
import { Text, TextProps } from 'react-native';
import { typography } from '../theme';

type Variant = keyof typeof typography;

interface Props extends TextProps {
  /** Preset tipográfico de `DESIGN.md`. Por defecto `body`. */
  variant?: Variant;
  /** Color de texto (token de `colors`); por defecto el del preset. */
  color?: string;
}

/**
 * Texto de marca: aplica un preset de `typography` (familia + tamaño + interlineado +
 * tracking) y opcionalmente un color. Punto único para usar las fuentes correctas.
 */
export function AppText({ variant = 'body', color, style, ...rest }: Props) {
  return <Text {...rest} style={[typography[variant], color ? { color } : null, style]} />;
}
