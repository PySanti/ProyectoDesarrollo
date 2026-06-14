import React from "react";
import { NavigationContainer } from "@react-navigation/native";
import { AuthProvider } from "./src/auth/AuthProvider";
import { RootNavigator } from "./src/navigation/RootNavigator";
import { SplashScreen } from "./src/screens/SplashScreen";
import { useAppFonts } from "./src/shared/fonts";

export default function App() {
  const fontsLoaded = useAppFonts();

  if (!fontsLoaded) {
    return <SplashScreen />;
  }

  return (
    <AuthProvider>
      <NavigationContainer>
        <RootNavigator />
      </NavigationContainer>
    </AuthProvider>
  );
}
