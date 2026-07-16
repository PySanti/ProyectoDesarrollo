# Privilegio sin rol — el privilegio abre el panel web sin importar quién lo tenga

- **Fecha**: 2026-07-15
- **Autor**: Santiago (decisiones) + Claude Opus 4.8 (redacción)
- **HU**: HU-04 (panel de gobernanza)
- **Estado**: aprobado, pendiente de plan de implementación
- **Depende de**: `2026-07-15-gobernanza-dos-privilegios-design.md` (sub-proyecto 1, completo) y
  `2026-07-15-web-gateada-por-privilegio-design.md` (sub-proyecto 2, completo)
- **Supersede**: D5 y D6 del sub-proyecto 2 (ver §5)

## 1. Problema

El plan original del sub-proyecto 3 era reconstruir nativo en mobile los paneles de gestión de
partidas y equipos, para que un Participante con esos privilegios pudiera usarlos desde el celular.
Medido contra la web (`CreatePartidaPage` 880 líneas, `SesionOperadorPage` 665 + 8 sub-paneles,
`TeamsAdminPage` 665), esto era ~2.635 líneas de UI a reconstruir a mano, sin contar lo nuevo de
autenticación y tiempo real.

Decisión del usuario: no reconstruir nada. **Un Participante con `GestionarPartidas` o
`GestionarEquipos` usa el panel web que ya existe** — el mismo que hoy usan Administrador y
Operador. El privilegio deja de estar atado a "para qué rol se pensó" y pasa a ser, en la práctica,
lo único que importa en las tres capas que hoy todavía miran el rol base antes de mirar el
privilegio: el nav web, el gateway y algunas policies del backend.

## 2. El modelo

**El privilegio autoriza; el rol base no participa.** Ni para dejar entrar, ni para vetar. Esta es
la doctrina que el sub-proyecto 2 ya había escrito para Administrador/Operador (D6); este
sub-proyecto la extiende a los tres roles.

| Privilegio | Abre (igual para cualquier rol que lo tenga) |
|---|---|
| `GestionarPartidas` | Área **Partidas** completa: listado, creación, detalle, consola de operación en vivo, historial |
| `GestionarEquipos` | Área **Equipos** completa: creación, gestión, rendimiento |

**Paridad total.** Un Participante con `GestionarPartidas` ve y opera exactamente lo mismo que un
Operador con ese privilegio — incluida la consola de operación en vivo. No hay una versión
recortada para Participante.

**El área Identidad no se toca.** Sigue siendo exclusiva del rol Administrador, protegida, no es un
privilegio (ver CLAUDE.md, "Roles, permissions & governance"). Nada de este sub-proyecto cambia eso.

**Mobile no cambia.** El privilegio es pura historia del cliente web. El panel de juego del
Participante en mobile sigue exactamente igual — sin mencionar gobernanza, sin enlaces a la web.

## 3. Decisiones

| # | Decisión | Alternativa descartada |
|---|---|---|
| D1 | **Las 4 policies compuestas del backend (rol AND privilegio) pierden el rol.** Quedan sólo-privilegio, en todo el stack — incluido el gateway. | Agregar Participante a la lista de roles aceptados: mantiene el AND como concepto pero lo vuelve inútil (cubre los 3 roles posibles), más código para el mismo resultado. |
| D2 | **Paridad total de experiencia.** El Participante privilegiado ve lo mismo que Operador/Administrador, consola en vivo incluida. | Excluir la consola en vivo para Participante: crea una tercera variante de "qué puede hacer alguien con este privilegio", contra el principio de que el privilegio es el mismo permiso sin importar quién lo tenga. |
| D3 | **Mobile: cero cambios.** | Aviso en el HomeScreen si el participante tiene el privilegio: UI nueva para un caso que ya se resuelve solo yendo a la web: no aporta y es superficie de mantenimiento extra. |
| D4 | **El gateway pasa de `RequireRole(rol base)` a `RequireRole(privilegio)`** en las rutas afectadas (`/partidas`, `/identity/admin/teams`, `/identity/teams` GET), no a `Default`. Sigue siendo una lectura de claims del token, sin consultar a Identity — el mismo contrato que ya cumple el gateway hoy, sólo que mira el claim del privilegio en vez del rol base. | `Default` (cualquier autenticado, como D6 del sub-proyecto 2 había decidido para 2 de estas 3 rutas): con las policies del backend ya sólo-privilegio (D1), un Participante sin ningún permiso llegaría hasta el servicio y recién ahí lo rechazarían. `RequireRole(privilegio)` da el mismo resultado final con la misma complejidad de código, pero corta en el borde — mejor defensa en profundidad sin costo extra. |

## 4. Cambios por capa

### 4.1 Web (`frontend/`)

- `src/shell/navConfig.tsx`: las áreas `partidas` y `equipos` pierden el campo `role`. Sólo
  `permisos` decide si el área aparece. `identidad` no se toca (sigue con `role: "Administrador"`,
  sin `permisos`).
- `src/shell/states.tsx` (`UnauthorizedScreen`): el texto "El panel web es exclusivo para
  administradores y operadores" / "no tiene rol Administrador u Operador" ya no es cierto. Pasa a
  algo como "Esta cuenta no tiene ningún panel disponible" / "no tiene ningún privilegio de
  gestión asignado — pedile a un administrador que te asigne uno, o usa la app móvil para jugar".
- `src/app/App.tsx`: **sin cambios de lógica.** Ya gatea sólo por `areasForRoles(roles,
  permisos).length === 0`, sin ningún chequeo de rol base propio (verificado leyendo el archivo).
  Las rutas de Partidas/Equipos ya usan `have={permisos}`; al abrirse el nav, quedan alcanzables
  para cualquier rol que tenga el permiso.

### 4.2 Gateway (`gateway/`)

- `Program.cs`: agrega policies `GestionarPartidas` y `GestionarEquipos`
  (`RequireRole("GestionarPartidas")` / `RequireRole("GestionarEquipos")` — el privilegio es un
  role claim del token, ADR-0013, así que el mecanismo es idéntico al que ya usa el gateway hoy).
- `appsettings.json`: rutas que cambian de policy —

| Ruta | Hoy | Pasa a |
|---|---|---|
| `/partidas/{**catch-all}` | `OperadorOAdministrador` | `GestionarPartidas` |
| `/identity/admin/teams/{**catch-all}` | `Administrador` | `GestionarEquipos` |
| `/identity/teams` (GET) | `OperadorOAdministrador` | `GestionarEquipos` |

  `/identity/users`, `/identity/governance` siguen en `Administrador` (Identidad no es un
  privilegio). `/identity/teams/{**catch-all}` (no-GET, flujo de equipo propio) sigue en
  `Participante`, sin tocar.

### 4.3 Backend — Identity

- `AdminTeamsController` y `TeamsAdminController` dejan de usar las policies `AdminGestionarEquipos`
  / `OperadorOAdminGestionarEquipos` y pasan a la policy **`GestionarEquipos`** que ya existe en
  `Program.cs` sin uso (`RequireRole("GestionarEquipos")`, sólo-privilegio). Las dos policies AND se
  borran de `Program.cs` — quedan sin ningún `[Authorize]` que las referencie.

### 4.4 Backend — Puntuaciones

- `Program.cs` agrega policies `GestionarEquipos` y `GestionarPartidas` sólo-privilegio, mismo
  patrón que ya usan Partidas y Operaciones de Sesión.
- `EquiposController` pasa de `OperadorOAdminGestionarEquipos` a `GestionarEquipos`.
- `HistorialController` pasa de `OperadorOAdminGestionarPartidas` a `GestionarPartidas`.
- Las dos policies AND se borran.
- `RankingsController` y `ParticipantesController` no se tocan (los consume el móvil, no son áreas
  del panel web).

### 4.5 Backend — Partidas y Operaciones de Sesión

Sin cambios: `PartidasController` y `SesionesController` ya usan policies sólo-privilegio
(`GestionarPartidas`, sin AND) desde el sub-proyecto 2.

## 5. Qué queda superado

Este sub-proyecto reemplaza dos decisiones del spec del sub-proyecto 2
(`2026-07-15-web-gateada-por-privilegio-design.md`):

- **D5** ("el gateway no se toca... el 3 lo abrirá cuando construya los paneles del móvil"): el 3
  ya no construye paneles de móvil; abre el gateway por privilegio en vez de por rol (§4.2, D4 de
  este documento).
- **D6** (parte del gateway: "`/identity/admin/teams` y `/identity/teams` GET pasan a `Default`"):
  pasan a `RequireRole(privilegio)`, no a `Default` — ver D4 de este documento y su justificación.

La parte de D6 sobre el backend (sólo privilegio, no rol AND privilegio, en las policies de
equipos) **se mantiene y se extiende**: este sub-proyecto le suma las dos policies de Puntuaciones
que en el sub-proyecto 2 sí llevaban AND.

## 6. Testing

- **Web**: `navConfig.test.tsx` — un Participante con `GestionarPartidas`/`GestionarEquipos` ve esas
  áreas igual que Admin/Operador. `App.test.tsx` — un Participante con el privilegio entra a
  `/partidas/crear` y `/identidad/equipos`; sin ningún privilegio sigue viendo la pantalla de sin
  acceso (con el texto corregido).
- **Backend (Identity, Puntuaciones)**: por cada policy que pierde el AND, el caso "rol correcto SIN
  privilegio → 403" se mantiene (sigue siendo cierto), pero "privilegio SIN el rol admin/operador →
  403" se invierte a "→ 200" (ya no aplica el AND). Se agrega el caso Participante-con-privilegio →
  200.
- **Gateway**: sí tiene tests (`gateway/tests/Umbral.Gateway.IntegrationTests/GatewayEndpointsTests.cs`,
  con `TestAuthHandler` por `X-Test-Roles`). Los tests que hoy pasan con solo el rol base (ej.
  `Partidas_con_Administrador_pasa_la_politica` con `"Administrador"` a secas) van a empezar a
  fallar cuando la policy deje de mirar el rol: hay que sumarles el privilegio en el header
  (`"Administrador,GestionarPartidas"`) y agregar el caso nuevo (`"Participante,GestionarPartidas"`
  → pasa; rol solo, sin privilegio → 403).
- **Verificación en vivo**: con Docker arriba, darle `GestionarPartidas` a Participante desde
  Gobernanza, loguearse en la web como participante y confirmar que ve "Partidas", entra a "Nueva
  partida" y el panel de creación carga — el mismo patrón de prueba que cerró el sub-proyecto 2.

## 7. Documentación a actualizar

- `CLAUDE.md`, sección "Roles, permissions & governance": la frase "Each opens its whole area in
  whichever client the role uses" queda desactualizada — un privilegio ya no vive en el cliente de
  quien lo tiene, vive siempre en la web.
- `CLAUDE.md`, sección "Clients" / regla de ruteo: agregar la excepción explícita — un Participante
  con `GestionarPartidas` o `GestionarEquipos` usa la web para esas dos áreas, no el móvil.
