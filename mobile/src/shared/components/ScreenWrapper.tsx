import React from 'react';
import { SafeAreaView, StyleSheet, ViewStyle } from 'react-native';
import { screenStyles } from '../styles';

interface Props {
  children: React.ReactNode;
  style?: ViewStyle;
}

export default function ScreenWrapper({ children, style }: Props) {
  return (
    <SafeAreaView style={[styles.container, style]}>
      {children}
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: screenStyles.safeArea as ViewStyle,
});
