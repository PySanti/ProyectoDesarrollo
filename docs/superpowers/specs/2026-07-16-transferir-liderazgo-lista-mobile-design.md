# Transferir liderazgo por lista (mobile) — diseño

## Problema

`TransferLeadershipScreen` (mobile) pide el `userId` (GUID) del nuevo líder por texto libre.
Es poco usable y expone un identificador técnico a un participante final. Además el
contenedor (`TransferLeadershipScreenContainer.tsx`) nunca pasa la lista de integrantes al
componente, así que hoy el flujo de selección por lista ni siquiera está activo: el usuario
siempre ve el fallback de texto libre.

## Modelo

1. Al entrar a la pantalla, se pide `GET /identity/teams/mine` (mismo endpoint y forma que ya
   usa `TeamPanelScreen`, vía `loadMyTeam` de `teamPanelApi.js`) para traer los `participantes`
   del equipo activo, frescos.
2. Lista elegible = `participantes` sin el líder actual (`usuarioId === currentUserId`).
3. Si la lista elegible queda vacía → texto **"No hay integrantes en el equipo"**.
4. Si hay integrantes, se listan mostrando **solo el nombre** (`nombre`), sin id ni otro dato.
5. Tocar una fila abre un modal de confirmación: "¿Confirmás transferir el liderazgo a
   **{nombre}**?" con botones "Transferir liderazgo" (confirma) y "Cancelar" (cierra el modal
   sin acción).
6. Confirmar llama `PATCH /identity/teams/leadership` (ya existente, sin cambios de contrato)
   con el `usuarioId` de la fila seleccionada. Éxito o error se muestra debajo de la lista,
   igual que hoy.

## Decisiones

**D1 — La pantalla trae su propia lista de integrantes (no depende de quién la abrió).**
Se evaluó pasar la lista ya cargada por `TeamPanelScreen` vía parámetros de navegación
(cero fetch extra) contra que la pantalla la busque sola (un fetch extra, pero funciona
igual sin importar desde dónde se navegue). Se eligió la segunda: reutiliza el mismo
`loadMyTeam` ya usado y testeado por el panel, y mantiene la pantalla autosuficiente —
mismo criterio que ya usan otras pantallas de equipo (p. ej. `InviteMemberScreen`, que
también trae su propia lista).

**D2 — Confirmación por modal, reutilizando el patrón existente.**
Se usa el componente nativo `Modal` de React Native con los primitivos `Card`/`AppText`/
`Button` de `shared/ui`, igual que `SessionExpiryModal.tsx` — no se introduce ninguna
librería ni patrón nuevo.

**D3 — Se reescribe la pantalla como TSX simple, se elimina el controller JS viejo.**
El patrón `TransferLeadershipScreenController.js` (React.createElement + componentes
inyectados) existía para poder testear sin renderer de React Native. La pantalla más
reciente del panel de equipo (`TeamPanelScreen.tsx`, ya aprobada) usa TSX directo sin ese
patrón, y no lleva test de render — la lógica de negocio se testea aparte, en el módulo de
flow. Se sigue el mismo criterio acá: `TransferLeadershipScreenController.js` y su test
(`TransferLeadershipScreenController.test.js`) se eliminan; la lógica pura sigue viviendo y
testeándose en `transferLeadershipFlow.js`.

**D4 — Se elimina `validateNewLeaderUserId`.**
Validaba formato GUID de texto libre. Con selección por lista el `usuarioId` siempre viene
de un integrante ya cargado — no hay entrada de texto que validar.

## Componentes

- `mobile/src/features/teams/transferLeadershipFlow.js`: mantiene `getEligibleLeaderMembers`
  (ahora solo excluye al líder actual por `usuarioId`) y `submitTransferLeadership` (llama a
  `transferLeadershipApi.js`, sin cambios). Elimina `validateNewLeaderUserId`.
- `mobile/src/features/teams/TransferLeadershipScreen.tsx`: reescrita en TSX. Estados:
  carga inicial, error de carga (+ reintentar), lista elegible vacía, lista de filas
  (nombre), modal de confirmación, envío en curso, éxito/error del envío.
- `mobile/src/features/teams/TransferLeadershipScreenContainer.tsx`: agrega
  `currentUserId={session.user.sub}` a los props que ya pasa.
- Elimina `mobile/src/features/teams/TransferLeadershipScreenController.js`.

## Manejo de errores

- Fallo al cargar el equipo (`GET /identity/teams/mine`): mensaje de error + reintentar,
  mismo patrón que `TeamPanelScreen`/`InvitationsScreen`.
- 404 al cargar (sin equipo activo, caso borde si el equipo se disolvió mientras se
  navegaba): mensaje "No perteneces a ningún equipo activo." (copy ya existente).
- Fallo al confirmar transferencia (404/409/401/403/network): mensaje de error debajo de la
  lista, igual que hoy (`transferLeadershipApi.js` no cambia sus mapeos de status).

## Testing

- `mobile/tests/transferLeadershipFlow.test.js`: reescrito. Casos: `getEligibleLeaderMembers`
  excluye al líder actual; lista vacía cuando el único integrante es el líder;
  `submitTransferLeadership` arma el PATCH correcto y mapea 404/409. Se elimina el test de
  `validateNewLeaderUserId`.
- Se elimina `mobile/tests/TransferLeadershipScreenController.test.js` (el componente que
  testeaba deja de existir). Nota: ese test hoy falla igual, porque afirmaba que
  "HU-07" aparecía en pantalla — texto ya removido en un fix previo.
- Sin test de render nuevo para la pantalla TSX — mismo criterio que `TeamPanelScreen.tsx`
  (ya aprobado sin ese test): la lógica de negocio queda cubierta en `transferLeadershipFlow.js`.
- `npm run typecheck` en mobile.

## Fuera de alcance

- No cambia el contrato HTTP de `GET /identity/teams/mine` ni de
  `PATCH /identity/teams/leadership`.
- No cambia reglas de negocio de transferencia de liderazgo (sigue siendo solo el líder
  quien puede transferir, backend sigue siendo la autoridad).
- No se toca ninguna otra pantalla de equipo.
