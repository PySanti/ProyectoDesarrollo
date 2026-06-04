import React from "react";
import { ActivityIndicator, Pressable, SafeAreaView, ScrollView, StyleSheet, Text, View } from "react-native";
import { requestBdtGeolocationPermission } from "../../permissions/bdtGeolocationPermission.js";
import { pickBdtTreasureImage, requestBdtTreasureImagePermission } from "../../permissions/bdtTreasureImagePicker.js";
import { BdtTreasureUploadScreenController } from "./BdtTreasureUploadScreenController.js";

type Props = {
  apiBaseUrl: string;
  token: string;
  partidaId: string;
  etapaId: string;
};

export function BdtTreasureUploadScreen({ apiBaseUrl, token, partidaId, etapaId }: Props) {
  return (
    <BdtTreasureUploadScreenController
      apiBaseUrl={apiBaseUrl}
      token={token}
      partidaId={partidaId}
      etapaId={etapaId}
      components={{ ActivityIndicator, Pressable, SafeAreaView, ScrollView, Text, View }}
      styles={styles}
      requestImagePermission={requestBdtTreasureImagePermission}
      requestGeolocationPermission={requestBdtGeolocationPermission}
      pickImage={pickBdtTreasureImage}
    />
  );
}

const styles = StyleSheet.create({
  safeArea: { flex: 1, backgroundColor: "#f4f7fb" },
  container: { padding: 20, gap: 12 },
  title: { fontSize: 24, fontWeight: "700", color: "#0f172a" },
  description: { color: "#475569", fontSize: 14, lineHeight: 20 },
  error: { color: "#b91c1c", fontSize: 13 },
  success: { color: "#166534", fontSize: 13 },
  empty: { color: "#475569", fontSize: 14 },
  card: { borderRadius: 14, backgroundColor: "#ffffff", borderWidth: 1, borderColor: "#dbe4f0", padding: 14, gap: 4 },
  cardTitle: { color: "#0f172a", fontSize: 17, fontWeight: "700" },
  cardLine: { color: "#334155", fontSize: 13 },
  joinButton: { marginTop: 8, borderRadius: 10, backgroundColor: "#0b5fff", paddingHorizontal: 12, paddingVertical: 10, alignItems: "center" },
  disabledButton: { marginTop: 8, borderRadius: 10, backgroundColor: "#94a3b8", paddingHorizontal: 12, paddingVertical: 10, alignItems: "center" },
  joinButtonText: { color: "#ffffff", fontWeight: "700" },
  secondaryButton: { borderRadius: 10, backgroundColor: "#e0ecff", paddingHorizontal: 12, paddingVertical: 10, alignItems: "center" },
  secondaryButtonText: { color: "#0b5fff", fontWeight: "700" },
});
