import React from "react";
import { createNativeStackNavigator } from "@react-navigation/native-stack";
import { useAuth } from "../auth/AuthProvider";
import { SplashScreen } from "../screens/SplashScreen";
import { LoginScreen } from "../screens/LoginScreen";
import { HomeScreen } from "../screens/HomeScreen";
import { CreateTeamScreenContainer } from "../features/teams/CreateTeamScreenContainer";
import { JoinTeamScreenContainer } from "../features/teams/JoinTeamScreenContainer";
import { LeaveTeamScreenContainer } from "../features/teams/LeaveTeamScreenContainer";
import { AppStackParamList, AuthStackParamList } from "./types";

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

  return (
    <AppStack.Navigator>
      <AppStack.Screen name="Home" component={HomeScreen} options={{ title: "Inicio" }} />
      <AppStack.Screen
        name="CreateTeam"
        component={CreateTeamScreenContainer}
        options={{ title: "Crear equipo" }}
      />
      <AppStack.Screen
        name="JoinTeam"
        component={JoinTeamScreenContainer}
        options={{ title: "Unirse a equipo" }}
      />
      <AppStack.Screen
        name="LeaveTeam"
        component={LeaveTeamScreenContainer}
        options={{ title: "Salir del equipo" }}
      />
    </AppStack.Navigator>
  );
}
