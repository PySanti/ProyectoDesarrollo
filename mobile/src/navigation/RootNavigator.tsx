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
import { DeleteTeamScreenContainer } from "../features/teams/DeleteTeamScreenContainer";
import { TriviaGamesListScreenContainer } from "../features/trivia/screens/TriviaGamesListScreenContainer";
import { TriviaLobbyScreenContainer } from "../features/trivia/screens/TriviaLobbyScreenContainer";
import { TriviaLivePlayScreenContainer } from "../features/trivia/live/TriviaLivePlayScreenContainer";
import { TriviaAnswerScreenContainer } from "../features/trivia/screens/TriviaAnswerScreenContainer";
import { TriviaResultScreenContainer } from "../features/trivia/screens/TriviaResultScreenContainer";
import { TriviaScoreScreenContainer } from "../features/trivia/screens/TriviaScoreScreenContainer";
import { BdtPublishedGamesScreenContainer } from "../features/bdt/BdtPublishedGamesScreenContainer";
import { BdtRankingScreenContainer } from "../features/bdt/ranking/BdtRankingScreenContainer";
import { BdtActiveStageScreenContainer } from "../features/bdt/BdtActiveStageScreenContainer";
import { BdtTreasureUploadScreenContainer } from "../features/bdt/BdtTreasureUploadScreenContainer";
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
        name="DeleteTeam"
        component={DeleteTeamScreenContainer}
        options={{ title: "Eliminar equipo" }}
      />
      <AppStack.Screen
        name="TriviaGamesList"
        component={TriviaGamesListScreenContainer}
        options={{ title: "Partidas Trivia" }}
      />
      <AppStack.Screen
        name="TriviaLobby"
        component={TriviaLobbyScreenContainer}
        options={{ title: "Espera Trivia" }}
      />
      <AppStack.Screen
        name="TriviaLivePlay"
        component={TriviaLivePlayScreenContainer}
        options={{ headerShown: false }}
      />
      <AppStack.Screen
        name="TriviaAnswer"
        component={TriviaAnswerScreenContainer}
        options={{ title: "Responder Trivia" }}
      />
      <AppStack.Screen
        name="TriviaResult"
        component={TriviaResultScreenContainer}
        options={{ title: "Resultado Trivia" }}
      />
      <AppStack.Screen
        name="TriviaScore"
        component={TriviaScoreScreenContainer}
        options={{ title: "Puntaje Trivia" }}
      />
      <AppStack.Screen
        name="BdtPublishedGames"
        component={BdtPublishedGamesScreenContainer}
        options={{ title: "Partidas BDT" }}
      />
      <AppStack.Screen
        name="BdtRanking"
        component={BdtRankingScreenContainer}
        options={{ headerShown: false }}
      />
      <AppStack.Screen
        name="BdtActiveStage"
        component={BdtActiveStageScreenContainer}
        options={{ title: "Etapa activa BDT" }}
      />
      <AppStack.Screen
        name="BdtTreasureUpload"
        component={BdtTreasureUploadScreenContainer}
        options={{ title: "Subir tesoro QR" }}
      />
    </AppStack.Navigator>
  );
}
