import React from 'react';
import { Feather } from '@expo/vector-icons';
import { colors } from '../theme';

export type IconName = React.ComponentProps<typeof Feather>['name'];

interface Props {
  name: IconName;
  size?: number;
  color?: string;
}

/**
 * Ícono de línea (Feather, vía `@expo/vector-icons`) — stroke coherente con los SVG de la web.
 * Único punto para iconografía; no introducir otra librería.
 */
export function Icon({ name, size = 22, color = colors.ink }: Props) {
  return <Feather name={name} size={size} color={color} />;
}
