# Diseño — Bloque 2b: UI web de creación/configuración de partida multi-juego

Fecha: 2026-07-07
Rama: `feature/bloque-2` (encima del Bloque 2a, `267b19b`)
Fuente: auditoría de cobertura (Bloque 2) · HU-45 (crear partida multi-juego) · HU-13 (juego Trivia con preguntas) · HU-28 (juego BDT con etapas) — lado configuración/web
Contrato: `contracts/http/partidas-config.md` (SP-2, autoridad; **no se modifica**)

## Contexto y problema

El operador hoy no puede crear una partida multi-juego: las pantallas viejas (`trivia/crear` → POST `/api/trivia-games` del trivia-game-service; `bdt/crear` → bdt-game-service; `trivia/formularios/nuevo` → concepto "formulario" ya eliminado de la doctrina) crean juegos sueltos estilo-viejo y no pueden expresar una `Partida` con secuencia de `Juego`s. El servicio **Partidas** (nuevo) expone la configuración completa vía gateway desde SP-2/SP-5a y no tiene ningún consumidor web (`partidasApi` no existe).

## Decisiones (confirmadas con el usuario)

1. **Retiro de la config vieja en este slice:** rutas + páginas + tests de `CreateTriviaGamePage`, `CreateTriviaFormPage`, `CreateBdtGamePage`. Las pantallas de operación viejas (`trivia/operar`, `bdt/partidas`) quedan intactas hasta 2c. Durante la transición el operador ya no crea juegos estilo-viejo (aceptado).
2. **Todo local, envío al final:** el wizard arma la partida completa en el navegador y al confirmar encadena los POSTs (header → juego 1 → juego 2…). Sin partidas parciales por abandono; el fallo a mitad se maneja con reintento de los restantes (el contrato no tiene DELETE/PUT).
3. **Alcance de páginas:** lista (GET /partidas) + wizard de creación + detalle de solo lectura (GET /partidas/{id}). Base que 2c reutilizará para publicar/operar.
4. **Estructura:** wizard de pasos en una sola ruta con estado en un reducer local (descartadas: página única con acordeones — inmanejable con preguntas/etapas anidadas y sin paso de revisión; rutas por paso — ceremonia sin beneficio en un flujo lineal).

## Diseño

### 1. Cliente API — `frontend/src/api/partidasApi.ts` (nuevo)

Espejo del contrato, base `VITE_GATEWAY_BASE_URL` con el patrón de `identityApi.ts` (`resolveBaseUrl` con strip de trailing slash, error tipado con `statusCode`, `fetchImpl` inyectable para tests):

- `createPartida(payload, token)` → POST `/partidas` → `{ partidaId }`
- `addJuegoTrivia(partidaId, payload, token)` → POST `/partidas/{id}/juegos/trivia` → `{ juegoId }`
- `addJuegoBdt(partidaId, payload, token)` → POST `/partidas/{id}/juegos/bdt` → `{ juegoId }`
- `getPartida(partidaId, token)` → GET `/partidas/{id}`
- `getPartidas(token)` → GET `/partidas`

Tipos TS espejo de los payloads del contrato; enums como strings (`Individual|Equipo`, `Manual|Automatico|ManualYAutomatico`, `Trivia|BusquedaDelTesoro`).

### 2. Páginas y rutas (App.tsx, guard de rol `Operador` como las existentes)

| Ruta | Componente | Qué muestra |
|---|---|---|
| `partidas` | `PartidasListPage` | tabla (nombre, modalidad, modo inicio, nº juegos, estado — `estado: null` se muestra "Sin publicar"), botón "Nueva partida" |
| `partidas/crear` | `CreatePartidaPage` | wizard 3 pasos |
| `partidas/:partidaId` | `PartidaDetailPage` | solo lectura: header + juegos ordenados; Trivia con preguntas/opciones (marca la correcta), BDT con área y etapas |

**Retiros en el mismo slice:** rutas `trivia/crear`, `trivia/formularios/nuevo`, `bdt/crear`; archivos `CreateTriviaGamePage.tsx`, `CreateTriviaFormPage.tsx`, `CreateBdtGamePage.tsx` y sus `.test.tsx`; de `triviaApi.ts` las funciones que queden sin consumidor (`createTriviaGame`, `createTriviaForm`, `getTriviaForms`), y de `bdtApi.ts` (`createBdtGame`, `decodeBdtExpectedQrImage`) **solo si** el grep confirma cero consumidores restantes. Enlaces de navegación (shell/menú) actualizados a `partidas`.

### 3. Wizard `CreatePartidaPage` (estado local, `useReducer`)

- **Paso 1 — Datos de partida:** nombre, modalidad, modo de inicio, `tiempoInicio` (visible y requerido sii modo ≠ `Manual`; null en `Manual`), mínimos/máximos (`max ≥ min ≥ 1`). Validación de usabilidad espejo del contrato; el backend sigue siendo la autoridad.
- **Paso 2 — Juegos:** lista ordenada; "Agregar juego" elige tipo y abre editor inline. Trivia: preguntas con patrón visual `question-card` (texto, ≥2 opciones, radio de correcta única, puntaje > 0, tiempo límite > 0). BDT: `areaBusqueda` (texto no vacío) + etapas (texto QR esperado no vacío, puntaje > 0, tiempo > 0). Reordenar y eliminar juegos libre mientras es local; `orden` (de juego y de etapa) se deriva de la posición — contiguo desde 1, nunca editable a mano. Mínimo 1 juego para avanzar.
- **Paso 3 — Revisión:** resumen completo de header + juegos; botón "Crear partida".

Clases del design system implementado (`docs/02-project-context/design/design-system.md`): `.page`, `.form-section`, `.question-card`, `.create-head`, `.create-actions`, `.notice`, `.pill`, etc. — reuso, no inventar primitivas nuevas.

### 4. Envío encadenado y errores

Al confirmar: `createPartida` → por cada juego en orden su POST. Estado por juego visible (pendiente/enviando/ok/error). Reglas:

- Un juego que respondió 201 nunca se re-envía (evita 409 por `orden` duplicado).
- Si un POST falla (400/409/red): se detiene la cadena, se muestra el mensaje del backend y un botón "Reintentar restantes" que continúa sobre el mismo `partidaId` desde el primer juego no enviado.
- Éxito completo → navegar a `partidas/{partidaId}` (detalle).
- 401/403 → mismo tratamiento de sesión que el resto de la app web.

### 5. Fuera de alcance

- Publicar/lobby/inicio/operación en vivo (2c — Operaciones de Sesión).
- Edición o borrado de partidas/juegos (el contrato no lo expone).
- Mobile (2d/2e), rankings/historial (2f), SignalR.
- Cambios en `contracts/`, backend, dominio, gateway: **cero**.
- Retiro de las pantallas de operación viejas y de los servicios trivia/bdt legacy (2c / Bloque 3).

## Verificación

1. Suites frontend verdes (`npm test`, `npm run build`); tests nuevos: `partidasApi` (URLs/headers/errores), wizard (validaciones por paso, flujo feliz mockeado, fallo parcial + reintento sin re-enviar los 201), lista y detalle (render con fixtures del contrato).
2. E2E manual con stack vivo (gate del slice): partidas + gateway + identity corriendo; login operador → crear partida con 1 Trivia (2 preguntas) + 1 BDT (2 etapas) → detalle la muestra completa; `GET /partidas` la lista.
3. CI del PR en verde.

## Criterios de aceptación

- Operador crea una partida `Individual` o `Equipo` con N juegos mezclados Trivia/BDT en orden, todo vía `:5080/partidas`.
- Fallo parcial reproducible (p.ej. matar partidas a mitad) deja la UI en estado de reintento y el reintento completa la partida sin duplicar juegos.
- Rutas y páginas de config viejas eliminadas; `trivia/operar` y `bdt/partidas` siguen funcionando igual.
- Cero referencias nuevas a `VITE_TRIVIA_API_BASE_URL`/`VITE_BDT_API_BASE_URL` (las existentes de operación quedan).
- Cero cambios en contratos, backend, dominio.
