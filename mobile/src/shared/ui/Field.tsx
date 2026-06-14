import React, { useState } from 'react';
import { StyleSheet, TextInput, TextInputProps, View } from 'react-native';
import { colors, fonts, radius, spacing } from '../theme';
import { AppText } from './AppText';

interface Props extends TextInputProps {
  label: string;
  /** Mensaje de error bajo el campo (texto, no solo borde). */
  error?: string | null;
  /** Ayuda bajo el campo cuando no hay error. */
  hint?: string;
}

type FocusArg = Parameters<NonNullable<TextInputProps['onFocus']>>[0];
type BlurArg = Parameters<NonNullable<TextInputProps['onBlur']>>[0];

/**
 * Campo de formulario: label en sentence case sobre el input, borde 1px que pasa a magenta
 * en foco, mensaje de error en texto rojo bajo el campo. Altura táctil ≥48px.
 */
export function Field({ label, error, hint, style, onFocus, onBlur, ...rest }: Props) {
  const [focused, setFocused] = useState(false);

  const handleFocus = (e: FocusArg) => {
    setFocused(true);
    onFocus?.(e);
  };
  const handleBlur = (e: BlurArg) => {
    setFocused(false);
    onBlur?.(e);
  };

  return (
    <View style={styles.wrap}>
      <AppText variant="label" color={colors.inkSoft}>
        {label}
      </AppText>
      <TextInput
        placeholderTextColor={colors.muted}
        {...rest}
        onFocus={handleFocus}
        onBlur={handleBlur}
        style={[styles.input, focused && styles.inputFocused, !!error && styles.inputError, style]}
      />
      {error ? (
        <AppText variant="label" color={colors.danger}>
          {error}
        </AppText>
      ) : hint ? (
        <AppText variant="label" color={colors.muted}>
          {hint}
        </AppText>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: {
    gap: spacing.xs + 2,
  },
  input: {
    minHeight: 48,
    borderWidth: 1,
    borderColor: colors.lineStrong,
    borderRadius: radius.md,
    backgroundColor: colors.bg,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.md,
    fontFamily: fonts.body,
    fontSize: 15,
    color: colors.ink,
  },
  inputFocused: {
    borderColor: colors.primaryBright,
    borderWidth: 1.5,
  },
  inputError: {
    borderColor: colors.danger,
  },
});
