import React from "react";
import { SafeAreaProvider } from "react-native-safe-area-context";
import { NavigationContainer } from "@react-navigation/native";
import { AuthProvider } from "./src/auth/AuthProvider";
import { RootNavigator } from "./src/navigation/RootNavigator";
import { SplashScreen } from "./src/screens/SplashScreen";
import { useAppFonts } from "./src/shared/fonts";

export default function App() {
  const fontsLoaded = useAppFonts();

  return (
    <SafeAreaProvider>
      {!fontsLoaded ? (
        <SplashScreen />
      ) : (
        <AuthProvider>
          <NavigationContainer>
            <RootNavigator />
          </NavigationContainer>
        </AuthProvider>
      )}
    </SafeAreaProvider>
  );
}
