import React, { useCallback, useEffect, useState } from "react";
import { ActivityIndicator, Modal, Pressable, SafeAreaView, ScrollView, StyleSheet, View } from "react-native";
import { AppText, Button, Card, Notice, ScreenHeader } from "../../shared/ui";
import { colors, radius, spacing } from "../../shared/theme";
import { loadMyTeam } from "./teamPanelApi.js";
import { getEligibleLeaderMembers } from "./transferLeadershipFlow.js";
import { submitTransferLeadershipFromScreen } from "./transferLeadershipScreenModel.js";

type Miembro = { usuarioId: string; nombre: string; esLider: boolean };

// `loadMyTeam` (teamPanelApi.js) has no JSDoc, so TS infers/widens its return type from the
// plain object literals instead of preserving the `ok` discriminant. Annotate the real shape here.
type LoadMyTeamResult =
  | { ok: false; type?: string; message?: string }
  | { ok: true; data: null }
  | {
      ok: true;
      data: {
        equipoId: string;
        nombreEquipo: string;
        estado: string;
        participantes: Miembro[];
      };
    };

type TransferLeadershipScreenProps = {
  apiBaseUrl: string;
  token: string;
  currentUserId: string;
  onTransferred?: (result: unknown) => void;
};

export function TransferLeadershipScreen({
  apiBaseUrl,
  token,
  currentUserId,
  onTransferred,
}: TransferLeadershipScreenProps) {
  const [loadingTeam, setLoadingTeam] = useState(true);
  const [teamError, setTeamError] = useState<string | null>(null);
  const [participantes, setParticipantes] = useState<Miembro[]>([]);
  const [selectedMember, setSelectedMember] = useState<Miembro | null>(null);
  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const loadTeam = useCallback(async () => {
    setLoadingTeam(true);
    setTeamError(null);
    const result = (await loadMyTeam(apiBaseUrl, token)) as LoadMyTeamResult;
    if (!result.ok) {
      setTeamError(result.message ?? "No se pudo cargar tu equipo.");
      setLoadingTeam(false);
      return;
    }
    if (result.data === null) {
      setTeamError("No pertenecés a ningún equipo activo.");
      setLoadingTeam(false);
      return;
    }
    setParticipantes(result.data.participantes);
    setLoadingTeam(false);
  }, [apiBaseUrl, token]);

  useEffect(() => {
    loadTeam();
  }, [loadTeam]);

  const eligibleMembers = getEligibleLeaderMembers(participantes, currentUserId) as Miembro[];

  async function handleConfirm() {
    if (!selectedMember) {
      return;
    }
    await submitTransferLeadershipFromScreen({
      apiBaseUrl,
      token,
      nuevoLiderUserId: selectedMember.usuarioId,
      onTransferred,
      setLoading,
      setErrorMessage,
      setSuccessMessage,
    });
    setSelectedMember(null);
  }

  return (
    <SafeAreaView style={styles.safe}>
      <ScrollView contentContainerStyle={styles.content}>
        <ScreenHeader title="Transferir liderazgo" subtitle="Elegí quién será el nuevo líder del equipo." />
        {errorMessage ? <Notice variant="error">{errorMessage}</Notice> : null}
        {successMessage ? <Notice variant="success">{successMessage}</Notice> : null}

        {loadingTeam ? (
          <ActivityIndicator color={colors.primaryBright} size="large" />
        ) : teamError ? (
          <View style={styles.group}>
            <Notice variant="error">{teamError}</Notice>
            <Button label="Reintentar" variant="secondary" onPress={loadTeam} />
          </View>
        ) : eligibleMembers.length === 0 ? (
          <Card>
            <AppText variant="body" color={colors.muted}>
              No hay integrantes en el equipo
            </AppText>
          </Card>
        ) : (
          <Card>
            <View style={styles.memberList}>
              {eligibleMembers.map((member) => (
                <Pressable
                  key={member.usuarioId}
                  accessibilityRole="button"
                  onPress={() => setSelectedMember(member)}
                  style={styles.memberRow}
                >
                  <AppText variant="body">{member.nombre}</AppText>
                </Pressable>
              ))}
            </View>
          </Card>
        )}
      </ScrollView>

      <Modal
        visible={!!selectedMember}
        transparent
        animationType="fade"
        onRequestClose={() => setSelectedMember(null)}
      >
        <View style={styles.backdrop}>
          <Card style={styles.modalCard}>
            <AppText variant="bodyStrong">
              ¿Confirmás transferir el liderazgo a {selectedMember?.nombre}?
            </AppText>
            <Button label="Transferir liderazgo" onPress={handleConfirm} loading={loading} disabled={loading} />
            <Button label="Cancelar" variant="secondary" onPress={() => setSelectedMember(null)} disabled={loading} />
          </Card>
        </View>
      </Modal>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: { flex: 1, backgroundColor: colors.bg },
  content: { padding: spacing.xl, gap: spacing.lg },
  group: { gap: spacing.sm },
  memberList: { gap: spacing.xs },
  memberRow: {
    minHeight: 48,
    justifyContent: "center",
    paddingVertical: spacing.sm,
    paddingHorizontal: spacing.sm,
    borderRadius: radius.md,
  },
  backdrop: {
    flex: 1,
    justifyContent: "center",
    padding: spacing.xl,
    backgroundColor: "rgba(0,0,0,0.55)",
  },
  modalCard: {
    gap: spacing.md,
  },
});
