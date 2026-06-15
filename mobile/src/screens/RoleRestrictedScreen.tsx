import React from "react";
import { SafeAreaView, StyleSheet, View } from "react-native";
import { useAuth } from "../auth/AuthProvider";
import { AppText, Button, Card, Notice } from "../shared/ui";
import { colors, spacing } from "../shared/theme";

/**
 * Pantalla mostrada cuando una cuenta sin rol `Participante` (Administrador / Operador) inicia
 * sesión en la app móvil. La app móvil es exclusiva para participantes; la administración y
 * operación se realizan desde el panel web. Se ofrece cerrar sesión para cambiar de cuenta.
 */
export function RoleRestrictedScreen() {
  const { session, logout } = useAuth();
  const username = session?.user.username ?? "";

  return (
    <SafeAreaView style={styles.safe}>
      <View style={styles.container}>
        <View style={styles.brand}>
          <AppText variant="display" color={colors.primaryStrong} style={styles.wordmark}>
            UMBRAL
          </AppText>
        </View>

        <Card>
          <AppText variant="title">Acceso restringido</AppText>
          <Notice variant="error" testID="role-restricted-notice">
            El panel móvil es exclusivo para participantes
          </Notice>
          <AppText variant="body" color={colors.muted}>
            {username
              ? `La cuenta ${username} tiene rol de administración u operación. `
              : ""}
            Usa el panel web para administrar y operar partidas.
          </AppText>
          <Button label="Cerrar sesión" variant="danger" onPress={logout} />
        </Card>
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: {
    flex: 1,
    backgroundColor: colors.bg,
  },
  container: {
    flex: 1,
    justifyContent: "center",
    padding: spacing.xl,
    gap: spacing.xxl,
  },
  brand: {
    gap: spacing.sm,
  },
  wordmark: {
    fontSize: 44,
    lineHeight: 48,
    letterSpacing: -1,
  },
});
