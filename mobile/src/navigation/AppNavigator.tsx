import React from 'react';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import LoginScreen from '../features/auth/screens/LoginScreen';
import TriviaGamesListScreen from '../features/trivia/screens/TriviaGamesListScreen';

export type RootStackParamList = {
  Login: undefined;
  TriviaGamesList: undefined;
};

const Stack = createNativeStackNavigator<RootStackParamList>();

export default function AppNavigator() {
  return (
    <Stack.Navigator screenOptions={{ headerShown: false }}>
      <Stack.Screen name="Login" component={LoginScreen} />
      <Stack.Screen
        name="TriviaGamesList"
        component={TriviaGamesListScreen}
        options={{ headerShown: true, headerTitle: 'Trivia', headerTintColor: '#2563EB' }}
      />
    </Stack.Navigator>
  );
}
