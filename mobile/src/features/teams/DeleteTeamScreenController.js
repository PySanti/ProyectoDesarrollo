import React, { useState } from "react";
import { deleteTeamConfirmMessage } from "./deleteTeamScreenContent.js";
import { submitDeleteTeamFromScreen } from "./deleteTeamScreenModel.js";

const emptyStyles = {};

export function DeleteTeamScreenController({ apiBaseUrl, token, onDeleted, components, styles = emptyStyles }) {
  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState(null);
  const [successMessage, setSuccessMessage] = useState(null);
  const [confirming, setConfirming] = useState(false);

  const { ActivityIndicator, Pressable, SafeAreaView, Text, View } = components;

  function handleRequestConfirm() {
    setErrorMessage(null);
    setConfirming(true);
  }

  function handleCancel() {
    setConfirming(false);
    setErrorMessage(null);
  }

  async function handleConfirm() {
    await submitDeleteTeamFromScreen({
      apiBaseUrl,
      token,
      onDeleted,
      setLoading,
      setErrorMessage,
      setSuccessMessage,
    });
  }

  return React.createElement(
    SafeAreaView,
    { style: styles.safeArea },
    React.createElement(
      View,
      { style: styles.container },
      React.createElement(Text, { style: styles.title }, "Eliminar equipo"),
      React.createElement(
        Text,
        { style: styles.description },
        "Esta accion elimina el equipo de forma permanente y libera a todos sus integrantes.",
      ),
      errorMessage ? React.createElement(Text, { style: styles.error }, errorMessage) : null,
      successMessage ? React.createElement(Text, { style: styles.success }, successMessage) : null,
      confirming
        ? React.createElement(
            View,
            { style: styles.confirmCard },
            React.createElement(Text, { style: styles.confirmText }, deleteTeamConfirmMessage),
            React.createElement(
              Pressable,
              {
                accessibilityRole: "button",
                onPress: handleConfirm,
                disabled: loading,
                style: [styles.button, loading && styles.buttonDisabled],
              },
              loading
                ? React.createElement(ActivityIndicator, { color: "#ffffff" })
                : React.createElement(Text, { style: styles.buttonText }, "Confirmar eliminación"),
            ),
            React.createElement(
              Pressable,
              {
                accessibilityRole: "button",
                onPress: handleCancel,
                disabled: loading,
                style: [styles.cancelButton, loading && styles.buttonDisabled],
              },
              React.createElement(Text, { style: styles.cancelButtonText }, "Cancelar"),
            ),
          )
        : React.createElement(
            Pressable,
            {
              accessibilityRole: "button",
              onPress: handleRequestConfirm,
              disabled: loading,
              style: [styles.button, loading && styles.buttonDisabled],
            },
            React.createElement(Text, { style: styles.buttonText }, "Eliminar equipo"),
          ),
    ),
  );
}
