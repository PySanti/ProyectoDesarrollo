import React from "react";
import { ActivityIndicator, SafeAreaView, StyleSheet, Text, View } from "react-native";
import { colors } from "../shared/theme";
import { screenStyles } from "../shared/styles";

export function SplashScreen() {
  return (
    <SafeAreaView style={styles.safeArea}>
      <View style={styles.container}>
        <ActivityIndicator size="large" color={colors.primary} />
        <Text style={styles.text}>Cargando sesion...</Text>
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safeArea: screenStyles.safeArea,
  container: {
    ...screenStyles.container,
    justifyContent: "center",
    alignItems: "center",
  },
  text: {
    color: colors.text,
    fontSize: 16,
  },
});
