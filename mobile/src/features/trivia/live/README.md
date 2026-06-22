# Maqueta — Partida de Trivia en vivo (G2)

Esta carpeta es una **maqueta navegable** del flujo de juego en vivo (cuenta regresiva, reacción,
puntaje con count-up, ranking). Existe porque el backend de **ejecución sincronizada** (push de la
pregunta activa en tiempo real) **todavía no existe**: permite ver y probar el registro de juego sin
backend, y sirve de **plantilla de integración**.

## Cómo está armado

```
TriviaLivePlayScreen      ← UI inmersiva. Depende SOLO de la interfaz LiveTriviaSource.
        ▲ source
TriviaLivePlayScreenContainer ← inyecta la fuente. HOY: createMockLiveTriviaSource().
        ▲ implements
liveTriviaTypes.ts        ← la interfaz LiveTriviaSource + formas de datos (EL CONTRATO).
mockLiveTriviaSource.ts   ← implementación mock (guion de 3 preguntas).
```

La pantalla no sabe de dónde salen los datos. Para conectarla al backend **no se toca la pantalla**:
se crea una nueva implementación de `LiveTriviaSource` y se cambia **una línea** en el container.

## Pasos para integrar con el backend real

1. **Implementar `BackendLiveTriviaSource implements LiveTriviaSource`** (nuevo archivo en esta carpeta),
   recibiendo `apiBaseUrl`, `token`, `partidaId`.
2. **Mapear cada método** (detalle en la cabecera de `liveTriviaTypes.ts`):
   - `onQuestion` → **lo que falta**: suscribirse al hub SignalR del runtime de Trivia en `Operaciones de Sesion` y emitir la
     pregunta activa cuando el operador la abre; emitir `null` al cierre de la partida. El `limiteSegundos`
     y la deadline son **autoritativos del servidor** (sincronizar la cuenta regresiva a esa deadline, no
     correr un reloj local como hace el mock).
   - `submit` → `answerTriviaQuestion()` (ya existe, HU-26).
   - `questionResult` → `getTriviaQuestionResult()` (ya existe, HU-28).
   - `finalScore` → `getTriviaScore()` (ya existe, HU-29) + endpoint/evento de **ranking de Trivia**
     (`PuntajeAcumulado` / `RankingTriviaActualizado`).
   - `advance` → probablemente **no-op** (el servidor empuja la siguiente pregunta dirigido por el operador).
3. **Cambiar la línea del container** `TriviaLivePlayScreenContainer.tsx`:
   `const source = useMemo(() => new BackendLiveTriviaSource(apiBaseUrl, token, partidaId), [...])`.
4. **Quitar los accesos "demo"**: el botón "Jugar partida en vivo (demo)" del Lobby y "Probar partida en
   vivo (demo)" de los estados vacío/error de `TriviaGamesListScreen`. La ruta `TriviaLivePlay` pasa a
   alcanzarse desde el inicio real de la partida.
5. Borrar `mockLiveTriviaSource.ts` cuando ya no se use.

## Reglas que se respetan

- **No cambia contratos ni reglas** (`contracts/`, business rules, HUs). El mock imita las formas reales
  (`TriviaAnswerResponse`, `TriviaQuestionResultResponse`, `TriviaScoreResponse`) para que el mapeo sea 1:1.
- Esto es **Trivia** (ranking por puntaje). El ranking de **BDT** también es point-based bajo la
  doctrina actual: ordena por puntos acumulados de etapas ganadas y desempata por menor tiempo
  acumulado de esas etapas; `EtapasGanadas` es solo dato informativo. No reutilizar este ranking ahí.
- Mobile = solo `Participante` / `Líder de equipo`.
