import React, { useState } from "react";
import {
  getLeaveTeamNoActiveTeamDescription,
  getLeaveTeamSuccessMessage,
  leaveTeamNoActiveTeamTitle,
} from "./leaveTeamScreenContent.js";
import { submitLeaveTeamFromScreen } from "./leaveTeamScreenModel.js";

const emptyStyles = {};

export function LeaveTeamScreenController({ apiBaseUrl, token, onLeft, components, styles = emptyStyles }) {
  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState(null);
  const [successMessage, setSuccessMessage] = useState(null);
  const [hasActiveTeam, setHasActiveTeam] = useState(true);

  const { ActivityIndicator, Pressable, SafeAreaView, Text, View } = components;

  async function handleSubmit() {
    await submitLeaveTeamFromScreen({
      apiBaseUrl,
      token,
      onLeft,
      setLoading,
      setErrorMessage,
      setSuccessMessage,
      setHasActiveTeam,
    });
  }

  return React.createElement(
    SafeAreaView,
    { style: styles.safeArea },
    React.createElement(
      View,
      { style: styles.container },
      React.createElement(Text, { style: styles.title }, "Salir del equipo"),
      React.createElement(
        Text,
        { style: styles.description },
        "Esta accion quitara tu membresia activa. Si eres lider y hay otros integrantes, primero debes transferir el liderazgo.",
      ),
      errorMessage ? React.createElement(Text, { style: styles.error }, errorMessage) : null,
      successMessage ? React.createElement(Text, { style: styles.success }, successMessage) : null,
      !hasActiveTeam
        ? React.createElement(
            View,
            { style: styles.noTeamCard },
            React.createElement(Text, { style: styles.noTeamTitle }, leaveTeamNoActiveTeamTitle),
            React.createElement(Text, { style: styles.noTeamDescription }, getLeaveTeamNoActiveTeamDescription()),
          )
        : null,
      React.createElement(
        Pressable,
        {
          accessibilityRole: "button",
          onPress: handleSubmit,
          disabled: loading || !hasActiveTeam,
          style: [styles.button, (loading || !hasActiveTeam) && styles.buttonDisabled],
        },
        loading
          ? React.createElement(ActivityIndicator, { color: "#ffffff" })
          : React.createElement(Text, { style: styles.buttonText }, hasActiveTeam ? "Salir de mi equipo" : leaveTeamNoActiveTeamTitle),
      ),
    ),
  );
}
