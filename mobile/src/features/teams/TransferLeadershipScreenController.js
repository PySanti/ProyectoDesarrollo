import React, { useState } from "react";
import { getEligibleLeaderMembers } from "./transferLeadershipFlow.js";
import { submitTransferLeadershipFromScreen } from "./transferLeadershipScreenModel.js";

const emptyStyles = {};

export function TransferLeadershipScreenController({
  apiBaseUrl,
  token,
  members = [],
  currentLeaderUserId,
  onTransferred,
  components,
  styles = emptyStyles,
}) {
  const [selectedUserId, setSelectedUserId] = useState("");
  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState(null);
  const [successMessage, setSuccessMessage] = useState(null);
  const { ActivityIndicator, Pressable, SafeAreaView, Text, TextInput, View } = components;
  const eligibleMembers = getEligibleLeaderMembers(members, currentLeaderUserId);

  async function handleSubmit() {
    await submitTransferLeadershipFromScreen({
      apiBaseUrl,
      token,
      nuevoLiderUserId: selectedUserId,
      onTransferred,
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
      React.createElement(Text, { style: styles.title }, "Transferir liderazgo"),
      React.createElement(
        Text,
        { style: styles.description },
        "Selecciona otro integrante del equipo como nuevo lider. Luego podras salir del equipo desde HU-07.",
      ),
      eligibleMembers.length > 0
        ? React.createElement(
            View,
            { style: styles.memberList },
            eligibleMembers.map((member) => {
              const userId = member.userId ?? member.usuarioId;
              const label = member.nombre ?? userId;
              return React.createElement(
                Pressable,
                {
                  key: userId,
                  accessibilityRole: "button",
                  onPress: () => setSelectedUserId(userId),
                  style: [styles.memberButton, selectedUserId === userId && styles.memberButtonActive],
                },
                React.createElement(Text, { style: styles.memberButtonText }, label),
              );
            }),
          )
        : React.createElement(
            Text,
            { style: styles.description },
            "No hay integrantes cargados para seleccionar. Puedes pegar el userId del nuevo lider.",
          ),
      React.createElement(TextInput, {
        accessibilityLabel: "Nuevo lider user id",
        autoCapitalize: "none",
        placeholder: "nuevoLiderUserId",
        value: selectedUserId,
        onChangeText: setSelectedUserId,
        style: styles.input,
      }),
      errorMessage ? React.createElement(Text, { style: styles.error }, errorMessage) : null,
      successMessage ? React.createElement(Text, { style: styles.success }, successMessage) : null,
      React.createElement(
        Pressable,
        {
          accessibilityRole: "button",
          onPress: handleSubmit,
          disabled: loading,
          style: [styles.button, loading && styles.buttonDisabled],
        },
        loading
          ? React.createElement(ActivityIndicator, { color: "#ffffff" })
          : React.createElement(Text, { style: styles.buttonText }, "Transferir liderazgo"),
      ),
    ),
  );
}
