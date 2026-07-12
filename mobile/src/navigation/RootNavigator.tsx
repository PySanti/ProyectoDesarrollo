import React from "react";
import { createNativeStackNavigator } from "@react-navigation/native-stack";
import { useAuth } from "../auth/AuthProvider";
import { SplashScreen } from "../screens/SplashScreen";
import { LoginScreen } from "../screens/LoginScreen";
import { RoleRestrictedScreen } from "../screens/RoleRestrictedScreen";
import { HomeScreen } from "../screens/HomeScreen";
import { CreateTeamScreenContainer } from "../features/teams/CreateTeamScreenContainer";
import { InvitationsScreenContainer } from "../features/teams/InvitationsScreenContainer";
import { InviteMemberScreenContainer } from "../features/teams/InviteMemberScreenContainer";
import { TransferLeadershipScreenContainer } from "../features/teams/TransferLeadershipScreenContainer";
import { LeaveTeamScreenContainer } from "../features/teams/LeaveTeamScreenContainer";
import { PartidasPanelScreenContainer } from "../features/partidas/PartidasPanelScreenContainer";
import { PartidaLobbyScreenContainer } from "../features/partidas/PartidaLobbyScreenContainer";
import { PartidaLiveScreenContainer } from "../features/partidas/PartidaLiveScreenContainer";
import { ConvocatoriasScreenContainer } from "../features/partidas/ConvocatoriasScreenContainer";
import { AppStackParamList, AuthStackParamList } from "./types";
import { colors, fonts } from "../shared/theme";

const AuthStack = createNativeStackNavigator<AuthStackParamList>();
const AppStack = createNativeStackNavigator<AppStackParamList>();

export function RootNavigator() {
  const { loading, session } = useAuth();

  if (loading) {
    return <SplashScreen />;
  }

  if (!session) {
    return (
      <AuthStack.Navigator>
        <AuthStack.Screen name="Login" component={LoginScreen} options={{ headerShown: false }} />
      </AuthStack.Navigator>
    );
  }

  // La app móvil es exclusiva para participantes. Una cuenta de Administrador u Operador puede
  // autenticarse en Keycloak, pero no debe entrar al flujo de juego: se le muestra un aviso.
  if (!session.user.roles.includes("Participante")) {
    return (
      <AuthStack.Navigator>
        <AuthStack.Screen
          name="Login"
          component={RoleRestrictedScreen}
          options={{ headerShown: false }}
        />
      </AuthStack.Navigator>
    );
  }

  return (
    <AppStack.Navigator
      screenOptions={{
        headerStyle: { backgroundColor: colors.bg },
        headerTintColor: colors.primaryStrong,
        headerTitleStyle: { fontFamily: fonts.display, color: colors.ink },
        headerShadowVisible: false,
        contentStyle: { backgroundColor: colors.bg },
        animation: "slide_from_right",
      }}
    >
      <AppStack.Screen name="Home" component={HomeScreen} options={{ headerShown: false }} />
      <AppStack.Screen
        name="CreateTeam"
        component={CreateTeamScreenContainer}
        options={{ title: "Crear equipo" }}
      />
      <AppStack.Screen
        name="Invitations"
        component={InvitationsScreenContainer}
        options={{ title: "Invitaciones" }}
      />
      <AppStack.Screen
        name="InviteMember"
        component={InviteMemberScreenContainer}
        options={{ title: "Invitar miembro" }}
      />
      <AppStack.Screen
        name="TransferLeadership"
        component={TransferLeadershipScreenContainer}
        options={{ title: "Transferir liderazgo" }}
      />
      <AppStack.Screen
        name="LeaveTeam"
        component={LeaveTeamScreenContainer}
        options={{ title: "Salir del equipo" }}
      />
      <AppStack.Screen
        name="PartidasPanel"
        component={PartidasPanelScreenContainer}
        options={{ title: "Partidas" }}
      />
      <AppStack.Screen
        name="PartidaLobby"
        component={PartidaLobbyScreenContainer}
        options={{ title: "Lobby" }}
      />
      <AppStack.Screen
        name="PartidaLive"
        component={PartidaLiveScreenContainer}
        options={{ title: "En vivo" }}
      />
      <AppStack.Screen
        name="Convocatorias"
        component={ConvocatoriasScreenContainer}
        options={{ title: "Convocatorias" }}
      />
    </AppStack.Navigator>
  );
}
