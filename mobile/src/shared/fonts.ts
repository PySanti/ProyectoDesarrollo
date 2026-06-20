import { useFonts } from 'expo-font';
import { SpaceGrotesk_600SemiBold, SpaceGrotesk_700Bold } from '@expo-google-fonts/space-grotesk';
import {
  Inter_400Regular,
  Inter_500Medium,
  Inter_600SemiBold,
  Inter_700Bold,
} from '@expo-google-fonts/inter';
import { JetBrainsMono_500Medium, JetBrainsMono_600SemiBold } from '@expo-google-fonts/jetbrains-mono';

/**
 * Carga las fuentes de marca (Space Grotesk / Inter / JetBrains Mono) que referencia
 * `theme.ts > fonts`. Devuelve `true` cuando están listas. El gate vive en `App.tsx`:
 * mientras carga se muestra el Splash para evitar el "flash" de fuente del sistema.
 */
export function useAppFonts(): boolean {
  const [loaded] = useFonts({
    SpaceGrotesk_600SemiBold,
    SpaceGrotesk_700Bold,
    Inter_400Regular,
    Inter_500Medium,
    Inter_600SemiBold,
    Inter_700Bold,
    JetBrainsMono_500Medium,
    JetBrainsMono_600SemiBold,
  });
  return loaded;
}
