import React, { useCallback, useEffect, useState } from "react";
import { ActivityIndicator, SafeAreaView, ScrollView, StyleSheet, Text, View } from "react-native";
import { Button, Card, Notice, ScreenHeader } from "../../shared/ui";
import { colors, fonts, radius, spacing, typography } from "../../shared/theme";
import { submitAcceptInvitation, submitRejectInvitation, fetchInvitations } from "./invitationsFlow.js";

type Invitation = {
  invitacionId: string;
  equipoId: string;
  nombreEquipo: string;
  liderUserId: string;
  estado: string;
};

type InvitationsScreenProps = {
  apiBaseUrl: string;
  token: string;
  onAccepted?: (result: unknown) => void;
  onRejected?: (result: unknown) => void;
};

export function InvitationsScreen({ apiBaseUrl, token, onAccepted, onRejected }: InvitationsScreenProps) {
  const [invitations, setInvitations] = useState<Invitation[]>([]);
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [feedbackMessage, setFeedbackMessage] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setErrorMessage(null);
    const result = await fetchInvitations({ apiBaseUrl, token, fetchImpl: undefined });
    setLoading(false);
    if (!result.ok) {
      setErrorMessage(result.message ?? "No se pudieron cargar las invitaciones.");
      return;
    }
    setInvitations((result.data as Invitation[]).filter((inv) => inv.estado === "Pendiente"));
  }, [apiBaseUrl, token]);

  useEffect(() => {
    load();
  }, [load]);

  async function handleAccept(invitacionId: string) {
    setActionLoading(invitacionId);
    setErrorMessage(null);
    setFeedbackMessage(null);
    const result = await submitAcceptInvitation({ apiBaseUrl, token, invitacionId, fetchImpl: undefined });
    setActionLoading(null);
    if (!result.ok) {
      setErrorMessage(result.message ?? "No se pudo aceptar la invitacion.");
      return;
    }
    setFeedbackMessage("Te uniste al equipo con exito.");
    setInvitations((prev) => prev.filter((inv) => inv.invitacionId !== invitacionId));
    onAccepted?.(result.data);
  }

  async function handleReject(invitacionId: string) {
    setActionLoading(invitacionId);
    setErrorMessage(null);
    setFeedbackMessage(null);
    const result = await submitRejectInvitation({ apiBaseUrl, token, invitacionId, fetchImpl: undefined });
    setActionLoading(null);
    if (!result.ok) {
      setErrorMessage(result.message ?? "No se pudo rechazar la invitacion.");
      return;
    }
    setFeedbackMessage("Invitacion rechazada.");
    setInvitations((prev) => prev.filter((inv) => inv.invitacionId !== invitacionId));
    onRejected?.(result.data);
  }

  return (
    <SafeAreaView style={styles.safe}>
      <ScrollView contentContainerStyle={styles.content}>
        <ScreenHeader
          title="Invitaciones"
          subtitle="Acepta o rechaza las invitaciones de equipo que has recibido."
        />
        {errorMessage ? <Notice variant="error">{errorMessage}</Notice> : null}
        {feedbackMessage ? <Notice variant="success">{feedbackMessage}</Notice> : null}
        {loading ? (
          <ActivityIndicator color={colors.primaryBright} size="large" />
        ) : invitations.length === 0 ? (
          <Card>
            <Text style={styles.empty}>No tienes invitaciones pendientes.</Text>
          </Card>
        ) : (
          invitations.map((inv) => (
            <Card key={inv.invitacionId}>
              <Text style={styles.teamName}>{inv.nombreEquipo}</Text>
              <View style={styles.actions}>
                <Button
                  label="Aceptar"
                  onPress={() => handleAccept(inv.invitacionId)}
                  disabled={actionLoading !== null}
                  loading={actionLoading === inv.invitacionId}
                />
                <Button
                  label="Rechazar"
                  onPress={() => handleReject(inv.invitacionId)}
                  disabled={actionLoading !== null}
                  loading={false}
                />
              </View>
            </Card>
          ))
        )}
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: {
    flex: 1,
    backgroundColor: colors.bg,
  },
  content: {
    padding: spacing.xl,
    gap: spacing.lg,
  },
  empty: {
    ...typography.body,
    color: colors.muted,
    textAlign: "center",
  },
  teamName: {
    fontFamily: fonts.semibold,
    fontSize: 16,
    color: colors.ink,
    marginBottom: spacing.md,
  },
  actions: {
    flexDirection: "row",
    gap: spacing.sm,
  },
});
