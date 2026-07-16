# Transferir liderazgo por lista (mobile) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reemplazar el input de texto libre (userId) de "Transferir liderazgo" (mobile) por
una lista de integrantes seleccionable con modal de confirmación.

**Architecture:** La pantalla (`TransferLeadershipScreen.tsx`) se reescribe en TSX puro (sin
el patrón `Controller.js` viejo), trae su propia lista de integrantes vía el mismo
`GET /identity/teams/mine` que ya usa el panel de equipo, y reutiliza sin cambios el endpoint
`PATCH /identity/teams/leadership` y el orquestador `submitTransferLeadershipFromScreen`
existente. La lógica pura de filtrado/envío queda en `transferLeadershipFlow.js`, testeada
con `node --test` (sin renderer de React Native), igual criterio que `TeamPanelScreen.tsx`.

**Tech Stack:** React Native + Expo (TSX), `node --test` para tests, primitivos de
`shared/ui` (`AppText`, `Button`, `Card`, `Notice`, `ScreenHeader`) y `Modal` nativo de RN.

## Global Constraints

- No cambia el contrato HTTP de `GET /identity/teams/mine` ni de `PATCH /identity/teams/leadership`.
- Cada fila de integrante muestra **solo el nombre** (`nombre`), sin id ni otro dato.
- Si no hay integrantes elegibles, el texto exacto es: `No hay integrantes en el equipo`.
- El modal de confirmación usa el mismo patrón que `SessionExpiryModal.tsx` (RN `Modal` +
  `Card`/`AppText`/`Button` de `shared/ui`), sin librerías nuevas.
- No se toca ninguna otra pantalla de equipo ni el backend.

---

### Task 1: Simplificar `transferLeadershipFlow.js` (quitar validación de texto libre)

**Files:**
- Modify: `mobile/src/features/teams/transferLeadershipFlow.js`
- Test: `mobile/tests/transferLeadershipFlow.test.js`

**Interfaces:**
- Consumes: `transferTeamLeadership(apiBaseUrl, token, nuevoLiderUserId, fetchImpl)` de
  `./transferLeadershipApi.js` (sin cambios, ya existe).
- Produces (para el Task 2): `getEligibleLeaderMembers(members = [], currentLeaderUserId)` →
  `Array<{ usuarioId?: string, userId?: string, nombre: string, esLider: boolean }>` (sin
  cambios de firma); `submitTransferLeadership({ apiBaseUrl, token, nuevoLiderUserId,
  fetchImpl })` → `Promise<{ ok: true, data } | { ok: false, type, message }>` (misma forma
  de retorno que hoy, pero ya sin el paso previo de validar formato GUID de texto libre).

- [ ] **Step 1: Reescribir `transferLeadershipFlow.js` sin `validateNewLeaderUserId`**

```js
import { transferTeamLeadership } from "./transferLeadershipApi.js";

export function getEligibleLeaderMembers(members = [], currentLeaderUserId) {
  return members.filter((member) => {
    const userId = member.userId ?? member.usuarioId;
    return userId && userId !== currentLeaderUserId && member.esLider !== true;
  });
}

export async function submitTransferLeadership({ apiBaseUrl, token, nuevoLiderUserId, fetchImpl }) {
  try {
    return await transferTeamLeadership(apiBaseUrl, token, nuevoLiderUserId, fetchImpl);
  } catch {
    return {
      ok: false,
      type: "error",
      message: "Ocurrio un error inesperado al transferir el liderazgo.",
    };
  }
}
```

- [ ] **Step 2: Reescribir `mobile/tests/transferLeadershipFlow.test.js`**

```js
import test from "node:test";
import assert from "node:assert/strict";
import {
  getEligibleLeaderMembers,
  submitTransferLeadership,
} from "../src/features/teams/transferLeadershipFlow.js";

const leaderUserId = "11111111-1111-1111-1111-111111111111";
const targetUserId = "22222222-2222-2222-2222-222222222222";

test("getEligibleLeaderMembers should exclude current leader", () => {
  const members = [
    { usuarioId: leaderUserId, nombre: "Lider", esLider: true },
    { usuarioId: targetUserId, nombre: "Nuevo lider", esLider: false },
  ];

  const result = getEligibleLeaderMembers(members, leaderUserId);

  assert.equal(result.length, 1);
  assert.equal(result[0].usuarioId, targetUserId);
});

test("getEligibleLeaderMembers should return empty when leader is the only member", () => {
  const members = [{ usuarioId: leaderUserId, nombre: "Lider", esLider: true }];

  const result = getEligibleLeaderMembers(members, leaderUserId);

  assert.equal(result.length, 0);
});

test("submitTransferLeadership should call PATCH leadership endpoint", async () => {
  let requestedUrl;
  let requestedBody;
  const result = await submitTransferLeadership({
    apiBaseUrl: "https://api.test",
    token: "token",
    nuevoLiderUserId: targetUserId,
    fetchImpl: async (url, options) => {
      requestedUrl = url;
      requestedBody = options.body;
      return {
        ok: true,
        status: 200,
        json: async () => ({
          equipoId: "33333333-3333-3333-3333-333333333333",
          liderAnteriorUserId: leaderUserId,
          nuevoLiderUserId: targetUserId,
          equipoEstado: "Activo",
        }),
      };
    },
  });

  assert.equal(requestedUrl, "https://api.test/identity/teams/leadership");
  assert.deepEqual(JSON.parse(requestedBody), { nuevoLiderUserId: targetUserId });
  assert.equal(result.ok, true);
  assert.equal(result.data.nuevoLiderUserId, targetUserId);
});

test("submitTransferLeadership should map 404 and 409 errors", async () => {
  const notFound = await submitTransferLeadership({
    apiBaseUrl: "https://api.test",
    token: "token",
    nuevoLiderUserId: targetUserId,
    fetchImpl: async () => ({ ok: false, status: 404 }),
  });
  const conflict = await submitTransferLeadership({
    apiBaseUrl: "https://api.test",
    token: "token",
    nuevoLiderUserId: targetUserId,
    fetchImpl: async () => ({ ok: false, status: 409 }),
  });

  assert.equal(notFound.type, "notFound");
  assert.equal(conflict.type, "conflict");
});
```

- [ ] **Step 3: Correr los tests**

Run: `cd mobile && node --test tests/transferLeadershipFlow.test.js`
Expected: 4 tests pasan, 0 fallos.

- [ ] **Step 4: Commit**

```bash
git add mobile/src/features/teams/transferLeadershipFlow.js mobile/tests/transferLeadershipFlow.test.js
git commit -m "refactor(mobile): transferLeadershipFlow ya no valida texto libre de userId"
```

---

### Task 2: Reescribir la pantalla como lista + modal de confirmación

**Files:**
- Modify: `mobile/src/features/teams/TransferLeadershipScreen.tsx` (reescritura completa)
- Modify: `mobile/src/features/teams/TransferLeadershipScreenContainer.tsx`
- Delete: `mobile/src/features/teams/TransferLeadershipScreenController.js`
- Delete: `mobile/tests/TransferLeadershipScreenController.test.js`

**Interfaces:**
- Consumes (de Task 1): `getEligibleLeaderMembers(members, currentLeaderUserId)` de
  `./transferLeadershipFlow.js`.
- Consumes (ya existentes, sin cambios): `loadMyTeam(apiBaseUrl, token, fetchImpl?)` de
  `./teamPanelApi.js` → `Promise<{ ok: true, data: { equipoId, nombreEquipo, estado,
  participantes: Array<{ usuarioId, nombre, esLider }> } | null } | { ok: false, message }>`;
  `submitTransferLeadershipFromScreen({ apiBaseUrl, token, nuevoLiderUserId, onTransferred,
  setLoading, setErrorMessage, setSuccessMessage })` de `./transferLeadershipScreenModel.js`
  (ya wired a `submitTransferLeadership` por default, arma mensaje de éxito y llama
  `onTransferred`).
- Produces: `TransferLeadershipScreen` sigue exportándose igual (mismo nombre, mismo archivo)
  así que `RootNavigator.tsx` y la ruta `"TransferLeadership"` no cambian.

- [ ] **Step 1: Reescribir `TransferLeadershipScreen.tsx`**

```tsx
import React, { useCallback, useEffect, useState } from "react";
import { ActivityIndicator, Modal, Pressable, SafeAreaView, ScrollView, StyleSheet, View } from "react-native";
import { AppText, Button, Card, Notice, ScreenHeader } from "../../shared/ui";
import { colors, radius, spacing } from "../../shared/theme";
import { loadMyTeam } from "./teamPanelApi.js";
import { getEligibleLeaderMembers } from "./transferLeadershipFlow.js";
import { submitTransferLeadershipFromScreen } from "./transferLeadershipScreenModel.js";

type Miembro = { usuarioId: string; nombre: string; esLider: boolean };

type TransferLeadershipScreenProps = {
  apiBaseUrl: string;
  token: string;
  currentUserId: string;
  onTransferred?: (result: unknown) => void;
};

export function TransferLeadershipScreen({
  apiBaseUrl,
  token,
  currentUserId,
  onTransferred,
}: TransferLeadershipScreenProps) {
  const [loadingTeam, setLoadingTeam] = useState(true);
  const [teamError, setTeamError] = useState<string | null>(null);
  const [participantes, setParticipantes] = useState<Miembro[]>([]);
  const [selectedMember, setSelectedMember] = useState<Miembro | null>(null);
  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const loadTeam = useCallback(async () => {
    setLoadingTeam(true);
    setTeamError(null);
    const result = await loadMyTeam(apiBaseUrl, token);
    if (!result.ok) {
      setTeamError(result.message ?? "No se pudo cargar tu equipo.");
      setLoadingTeam(false);
      return;
    }
    if (result.data === null) {
      setTeamError("No pertenecés a ningún equipo activo.");
      setLoadingTeam(false);
      return;
    }
    setParticipantes(result.data.participantes as Miembro[]);
    setLoadingTeam(false);
  }, [apiBaseUrl, token]);

  useEffect(() => {
    loadTeam();
  }, [loadTeam]);

  const eligibleMembers = getEligibleLeaderMembers(participantes, currentUserId) as Miembro[];

  async function handleConfirm() {
    if (!selectedMember) {
      return;
    }
    await submitTransferLeadershipFromScreen({
      apiBaseUrl,
      token,
      nuevoLiderUserId: selectedMember.usuarioId,
      onTransferred,
      setLoading,
      setErrorMessage,
      setSuccessMessage,
    });
    setSelectedMember(null);
  }

  return (
    <SafeAreaView style={styles.safe}>
      <ScrollView contentContainerStyle={styles.content}>
        <ScreenHeader title="Transferir liderazgo" subtitle="Elegí quién será el nuevo líder del equipo." />
        {errorMessage ? <Notice variant="error">{errorMessage}</Notice> : null}
        {successMessage ? <Notice variant="success">{successMessage}</Notice> : null}

        {loadingTeam ? (
          <ActivityIndicator color={colors.primaryBright} size="large" />
        ) : teamError ? (
          <View style={styles.group}>
            <Notice variant="error">{teamError}</Notice>
            <Button label="Reintentar" variant="secondary" onPress={loadTeam} />
          </View>
        ) : eligibleMembers.length === 0 ? (
          <Card>
            <AppText variant="body" color={colors.muted}>
              No hay integrantes en el equipo
            </AppText>
          </Card>
        ) : (
          <Card>
            <View style={styles.memberList}>
              {eligibleMembers.map((member) => (
                <Pressable
                  key={member.usuarioId}
                  accessibilityRole="button"
                  onPress={() => setSelectedMember(member)}
                  style={styles.memberRow}
                >
                  <AppText variant="body">{member.nombre}</AppText>
                </Pressable>
              ))}
            </View>
          </Card>
        )}
      </ScrollView>

      <Modal
        visible={!!selectedMember}
        transparent
        animationType="fade"
        onRequestClose={() => setSelectedMember(null)}
      >
        <View style={styles.backdrop}>
          <Card style={styles.modalCard}>
            <AppText variant="bodyStrong">
              ¿Confirmás transferir el liderazgo a {selectedMember?.nombre}?
            </AppText>
            <Button label="Transferir liderazgo" onPress={handleConfirm} loading={loading} disabled={loading} />
            <Button label="Cancelar" variant="secondary" onPress={() => setSelectedMember(null)} disabled={loading} />
          </Card>
        </View>
      </Modal>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: { flex: 1, backgroundColor: colors.bg },
  content: { padding: spacing.xl, gap: spacing.lg },
  group: { gap: spacing.sm },
  memberList: { gap: spacing.xs },
  memberRow: {
    minHeight: 48,
    justifyContent: "center",
    paddingVertical: spacing.sm,
    paddingHorizontal: spacing.sm,
    borderRadius: radius.md,
  },
  backdrop: {
    flex: 1,
    justifyContent: "center",
    padding: spacing.xl,
    backgroundColor: "rgba(0,0,0,0.55)",
  },
  modalCard: {
    gap: spacing.md,
  },
});
```

- [ ] **Step 2: Actualizar `TransferLeadershipScreenContainer.tsx`**

```tsx
import React from "react";
import { StyleSheet, Text } from "react-native";
import { useAuth } from "../../auth/AuthProvider";
import { mobileEnv } from "../../config/env";
import { TransferLeadershipScreen } from "./TransferLeadershipScreen";

export function TransferLeadershipScreenContainer() {
  const { session } = useAuth();

  if (!session) {
    return <Text style={styles.message}>Sesion no disponible.</Text>;
  }

  return (
    <TransferLeadershipScreen
      apiBaseUrl={mobileEnv.gatewayApiBaseUrl}
      token={session.token}
      currentUserId={session.user.sub}
    />
  );
}

const styles = StyleSheet.create({
  message: {
    margin: 20,
    color: "#b91c1c",
  },
});
```

- [ ] **Step 3: Eliminar el controller viejo y su test**

```bash
git rm mobile/src/features/teams/TransferLeadershipScreenController.js
git rm mobile/tests/TransferLeadershipScreenController.test.js
```

- [ ] **Step 4: Typecheck**

Run: `cd mobile && npm run typecheck`
Expected: 0 errores.

- [ ] **Step 5: Correr toda la suite mobile**

Run: `cd mobile && npm test`
Expected: todos los tests pasan (incluye los de `transferLeadershipFlow.test.js` del Task 1;
`TransferLeadershipScreenController.test.js` ya no existe, no corre).

- [ ] **Step 6: Commit**

```bash
git add mobile/src/features/teams/TransferLeadershipScreen.tsx mobile/src/features/teams/TransferLeadershipScreenContainer.tsx
git commit -m "feat(mobile): transferir liderazgo por lista de integrantes con modal de confirmación"
```

---

## Self-Review Notes

- **Cobertura del spec:** D1 (fetch propio vía `loadMyTeam`) → Task 2 Step 1 (`loadTeam`). D2
  (modal con primitivos existentes) → Task 2 Step 1 (`Modal`/`Card`/`Button`). D3 (TSX sin
  controller, sin test de render) → Task 2 Steps 1 y 3. D4 (elimina validación de texto
  libre) → Task 1. Texto exacto "No hay integrantes en el equipo" → Task 2 Step 1. Fila
  muestra solo nombre → Task 2 Step 1 (`<AppText variant="body">{member.nombre}</AppText>`,
  sin otro dato).
- **Placeholders:** ninguno, todo el código de cada step es completo y pegable.
- **Consistencia de tipos:** `Miembro` (`usuarioId`, `nombre`, `esLider`) coincide con la
  forma real de `participantes` que devuelve `GET /identity/teams/mine`
  (`contracts/http/identity-api.md`) y con lo que ya consume `TeamPanelScreen.tsx`.
  `getEligibleLeaderMembers` sigue aceptando `usuarioId` (fallback a `userId` por
  compatibilidad con los tests existentes de Task 1) — mismo campo en ambos tasks.
