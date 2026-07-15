# Web gateada por privilegio

- **Fecha**: 2026-07-15
- **Autor**: Santiago (decisiones) + Claude Opus 4.8 (redacción)
- **HU**: HU-04 (panel de gobernanza)
- **Estado**: aprobado, pendiente de plan de implementación
- **Depende de**: `2026-07-15-gobernanza-dos-privilegios-design.md` (sub-proyecto 1, completado y verificado en vivo)
- **Supersede**: el gateo por rol base de las áreas del nav web

## 1. Problema

**Este es el sub-proyecto que resuelve el síntoma original.** El administrador asigna «Gestionar
partidas» en el panel de gobernanza y **no pasa nada**: el panel de creación de partidas sigue sin
aparecerle.

El sub-proyecto 1 arregló la mitad de abajo: la gobernanza ya es coherente y sobrevive a un
reinicio. Pero el token que la web recibe lleva el privilegio y **la web lo ignora**:

1. **`keycloak.ts` descarta los permisos al parsear el token.** Su mapa `knownRoles` sólo reconoce
   los tres roles base; `GestionarPartidas` llega en el mismo `realm_access.roles` (ADR-0013: los
   permisos son realm roles composite) y se tira a la basura.
2. **El nav y las rutas se abren por rol base**, no por privilegio. `App.tsx` exige `Operador` para
   `/partidas`; `navConfig.tsx` abre el área Partidas a `["Operador", "Administrador"]`.

El backend ya autoriza por permiso funcional. La web no. **El panel escribe permisos que el cliente
no lee** — esa es la brecha que cierra este sub-proyecto.

## 2. El modelo

Del sub-proyecto 1, sin cambios:

| Privilegio | Abre |
|---|---|
| `GestionarPartidas` | Área **Partidas**: `Partidas` + `Nueva partida` |
| `GestionarEquipos` | Área **Equipos**: `Creación de equipos` + `Gestión de equipos` + `Rendimiento de equipos` |

**El privilegio abre el área entera, consulta incluida.** Sin el privilegio, no aparece nada de esa
área: ni en el menú ni por URL directa.

El área **Identidad** no es un privilegio: viene con el rol `Administrador` y está protegida.

**Defaults (ya aplicados en producción por el sub-proyecto 1):** Administrador → `GestionarEquipos`;
Operador → `GestionarPartidas`; Participante → ninguno.

> **Consecuencia visible y buscada:** por defecto el **Operador deja de ver el área Equipos** y el
> **Administrador deja de ver el área Partidas**. Es exactamente la regla pedida: sin el privilegio
> de gestión de X, nada de X aparece en su panel.

## 3. Decisiones

| # | Decisión | Alternativa descartada |
|---|---|---|
| D1 | Las **áreas** del nav y sus rutas se abren por **privilegio**, no por rol base. | Sólo los botones de escritura por privilegio: dejaría la consulta abierta, contra la regla («si un rol no tiene el privilegio de gestión X, no debe aparecer nada de X»). |
| D2 | Quien no tenga **ninguna área** ve una pantalla **«sin accesos»** con su nombre y el botón de cerrar sesión. | Shell vacío: entra al panel y no ve nada, sin explicación. |
| D3 | Las policies del backend son **rol base AND permiso funcional**. Ver §5, R1: es una decisión de seguridad, no de estilo. | Sólo permiso: abre escalada de privilegios contra los puertos de servicio, que están expuestos. |
| D4 | **Se arregla el token viejo tras el refresh.** `AuthProvider.refresh()` pasa a devolver el `AuthUser` completo re-parseado, no sólo el string del token. | Dejarlo: tras cambiar privilegios en el panel, el usuario tendría que cerrar sesión para que surtan efecto. Bug preexistente, pero los privilegios cambian mucho más que los roles, así que ahora duele. |
| D5 | **El gateway no se toca.** `/partidas` sigue en `OperadorOAdministrador`. | Abrirlo a cualquiera con el privilegio: el Participante no tiene UI de gestión hasta el sub-proyecto 3, así que abriría una ruta que nadie puede usar. El 3 lo abrirá cuando construya los paneles del móvil. |

## 4. Cambios por capa

### 4.1 `frontend/src/auth/keycloak.ts` — leer los permisos del token

Hoy `extractRoles` filtra por el mapa `knownRoles` y descarta todo lo demás. Se añade un segundo
mapa y un segundo extractor:

```ts
export interface AuthUser {
  username: string;
  /** Roles base: Administrador / Operador / Participante. */
  roles: string[];
  /** Privilegios funcionales del rol (el permiso autoriza, no el rol). */
  permisos: string[];
  token: string;
}
```

Los permisos viajan en el mismo `realm_access.roles` que los roles base (ADR-0013: son realm roles
composite y Keycloak los expande solo). Se extraen a un campo **aparte**, no se mezclan.

> **Por qué separados y no una sola lista:** `AppShell.tsx:44` hace `roles.join(" · ")` y lo muestra
> en la barra superior. Mezclarlos le enseñaría «Administrador · GestionarEquipos» al usuario.

Recuperable de `2fabefd` (revertido en `60ce104`), que ya lo implementó así.

### 4.2 `keycloak.ts` + `useSessionRefresh.ts` + `App.tsx` — el token viejo (D4)

Hoy el refresh conserva los roles y permisos antiguos:

```ts
// App.tsx:60 — sólo cambia el string; roles y permisos quedan congelados del login.
onToken: (token) => setAuthState((prev) => ({ ...prev.user, token }))
```

Cambia el contrato para que el token nuevo se re-parsee entero:

- `AuthProvider.refresh(): Promise<AuthUser>` (era `Promise<string>`).
- `useSessionRefresh`'s `onToken: (user: AuthUser) => void` (era `(token: string) => void`).
- `App.tsx` pasa a `onToken: (user) => setAuthState({ status: "ready", user })`.

`sessionRefreshCore` **no cambia**: sólo necesita el booleano de éxito.

### 4.3 `frontend/src/shell/navConfig.tsx` — áreas por privilegio

`NavAreaDef` gana `permisos?: readonly string[]`. El área se abre si el rol base **y** el privilegio
coinciden:

- `identidad`: rol `Administrador`, **sin** privilegio.
- `partidas`: `permisos: ["GestionarPartidas"]`.
- `equipos`: `permisos: ["GestionarEquipos"]`.

El `permisos` a nivel de item de «Nueva partida» desaparece: es redundante cuando el área entera ya
lo exige.

`landingPath(roles, permisos)` pasa a devolver `string | null`. `null` = ninguna área (D2). Sin esto,
un Operador sin `GestionarPartidas` aterrizaría en `/partidas`, la ruta lo rebotaría a su landing, y
el landing sería `/partidas`: **bucle de redirección infinito**.

### 4.4 `frontend/src/app/App.tsx` — rutas y pantalla sin accesos

- El guardia `RequireRole` se generaliza: `have` (credenciales del usuario) contra `need` (lo que la
  ruta exige), sirviendo para roles o privilegios.
- Rutas de partidas (`/partidas`, `/partidas/:id`, `/sesion`, `/historial`) → `GestionarPartidas`.
- Rutas de equipos (`/equipos`, `/identidad/equipos`, `/puntuaciones/equipos`) → `GestionarEquipos`
  (y `/identidad/equipos` conserva además su rol `Administrador`).
- El chequeo actual `if (!roles.includes("Administrador") && !roles.includes("Operador"))` se
  sustituye por `if (areasForRoles(roles, permisos).length === 0)`. Una sola condición cubre los dos
  casos: el participante que entra a la web (0 áreas por rol) y el operador sin privilegios (0 áreas
  por privilegio).
- `puedeOperar` (`permisos.includes("GestionarPartidas")`) queda **siempre true** dentro del área
  Partidas, porque el área ya lo exige. Se conserva el prop para no arrastrar a `PartidasListPage`,
  `PartidaDetailPage` y `SesionOperadorPage` y sus tests a este cambio. Ver §7.

### 4.5 `AppShell.tsx` / `NavRail.tsx`

Reciben `permisos: string[]` y `NavRail` llama `areasForRoles(roles, permisos)`. Recuperable de
`2fabefd`.

### 4.6 Backend — las policies que faltan

| Servicio | Controller | Hoy | Pasa a |
|---|---|---|---|
| Partidas | `PartidasController` GET (`/partidas`, `/partidas/{id}`) | *(sin policy)* | `GestionarPartidas` |
| Identity | `AdminTeamsController` | `AdminOnly` | `Administrador` **AND** `GestionarEquipos` |
| Identity | `TeamsAdminController` | `OperadorOAdministrador` | `(Operador\|Administrador)` **AND** `GestionarEquipos` |
| Puntuaciones | `EquiposController` (rendimiento) | `[Authorize]` | `(Operador\|Administrador)` **AND** `GestionarEquipos` |
| Puntuaciones | `HistorialController` | `Roles = "Operador,Administrador"` | `(Operador\|Administrador)` **AND** `GestionarPartidas` |

**No se tocan:** `RankingsController` y `ParticipantesController` de Puntuaciones (`[Authorize]`) —
los consume el móvil. `SesionesController` de Operaciones de Sesión — su gateo por
`GestionarPartidas` ya es correcto.

El comentario de `TeamsAdminController` («Vive fuera de `TeamsController` porque la policy de clase
`GestionarEquipos` es aditiva y esos roles no tienen ese permiso funcional») queda obsoleto: con los
defaults nuevos el Administrador **sí** tiene `GestionarEquipos`. Actualizarlo.

## 5. Riesgos

| # | Riesgo | Mitigación |
|---|---|---|
| R1 | **Escalada de privilegios.** Si `AdminTeamsController` exigiera sólo `GestionarEquipos`, cualquier rol al que el panel le diera ese privilegio podría borrar equipos ajenos llamando al **puerto 5001 directamente**, saltándose el filtro por rol del gateway. Los puertos de servicio están expuestos en el compose. | D3: policies **rol AND permiso**. El rol delimita el ámbito, el privilegio habilita el CRUD. Test de contrato por cada policy compuesta. |
| R2 | El fix del token (D4) toca el contrato de `AuthProvider` y `useSessionRefresh`, que tienen tests propios. | Cambio de tipo, no de lógica: `refresh()` ya llama a Keycloak; sólo re-parsea lo que ya tiene. `sessionRefreshCore` no se toca. |
| R3 | Un Operador sin `GestionarPartidas` entra en bucle de redirección contra su propio landing. | §4.3: `landingPath` devuelve `null` y `App.tsx` corta con la pantalla «sin accesos» antes de montar el router. |
| R4 | Un usuario con el área abierta en pantalla pierde el privilegio; tras el refresh, el nav se recalcula y el área desaparece bajo sus pies. | Aceptado: es el comportamiento correcto (D4). El backend ya devolvería 403 de todas formas. |

## 6. Verificación

1. Tests del nav: un área no aparece sin su privilegio; aparece con él, sin mirar el rol base.
2. Tests de ruta: acceso directo por URL sin el privilegio redirige al landing.
3. Test de la pantalla «sin accesos» para un Operador sin privilegios.
4. Test del refresh: un token nuevo con privilegios distintos **cambia** los del estado (D4).
5. Tests de contrato de cada policy compuesta: rol sin privilegio → 403; privilegio sin rol → 403;
   ambos → 200. **Los tres casos**, o el AND no está probado.
6. **Prueba en vivo — el síntoma original**: asignar «Gestionar partidas» al Administrador en el
   panel, cerrar sesión, volver a entrar, y **ver aparecer el área Partidas con «Nueva partida»**.
   Esto es lo que originó todo el trabajo.
7. **Prueba en vivo — la simétrica**: quitarle «Gestionar equipos» al Administrador y ver
   desaparecer el área Equipos.

## 7. Alcance: lo que NO entra

- **El gateway** (D5). `/partidas` sigue en `OperadorOAdministrador`. Lo abrirá el sub-proyecto 3.
- **El prop `puedeOperar`** de `PartidasListPage`/`PartidaDetailPage`/`SesionOperadorPage`. Queda
  siempre `true` y por tanto es código muerto, pero eliminarlo arrastra tres páginas y sus tests a un
  cambio que no lo necesita. Anotado como deuda.
- **Los paneles nativos del móvil**: sub-proyecto 3.
- **`ParticiparEnPartidas`**: sigue fijo al rol, fuera del panel. Decidido en el sub-proyecto 1.

## 8. Notas de estado

- El sub-proyecto 1 está completo y verificado en vivo (`b84b5e8`..`de37ae3`). Los defaults nuevos ya
  están aplicados en el entorno del usuario.
- El trabajo web previo se revirtió en `60ce104` (revierte `2fabefd`). `keycloak.ts`, `AppShell.tsx` y
  `NavRail.tsx` son **recuperables casi tal cual**; `App.tsx` y `navConfig.tsx` deben rehacerse
  ampliados, porque aquel intento sólo gateaba «Nueva partida» y el modelo nuevo gatea el área entera.
- La base de datos del usuario tiene hoy `(1,1)` — `Administrador → GestionarPartidas` — insertada a
  mano para verificar el guardia de la migración. Conviene quitarla desde el panel antes de la
  verificación §6.6, o el síntoma original ya estaría «arreglado» por accidente y la prueba no
  probaría nada.
