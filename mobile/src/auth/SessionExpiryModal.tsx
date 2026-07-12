// Modal de continuación de sesión (RNF-24), espejo del modal web.
import React from "react";
import { Modal, StyleSheet, View } from "react-native";
import { AppText, Button, Card } from "../shared/ui";
import { spacing } from "../shared/theme";

export function SessionExpiryModal({
  visible,
  onContinuar,
  onSalir,
}: {
  visible: boolean;
  onContinuar: () => void;
  onSalir: () => void;
}) {
  return (
    <Modal visible={visible} transparent animationType="fade" onRequestClose={onSalir}>
      <View style={styles.backdrop}>
        <Card style={styles.card}>
          <AppText variant="bodyStrong">¿Sigues ahí?</AppText>
          <AppText>Tu sesión está por expirar.</AppText>
          <Button label="Continuar sesión" onPress={onContinuar} />
          <Button label="Salir" variant="secondary" onPress={onSalir} />
        </Card>
      </View>
    </Modal>
  );
}

const styles = StyleSheet.create({
  backdrop: {
    flex: 1,
    justifyContent: "center",
    padding: spacing.xl,
    backgroundColor: "rgba(0,0,0,0.55)",
  },
  card: {
    gap: spacing.md,
  },
});
