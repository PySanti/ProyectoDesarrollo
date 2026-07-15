# Gobernanza: modelo de dos privilegios

- **Fecha**: 2026-07-15
- **Autor**: Santiago (decisiones) + Claude Opus 4.8 (redacción)
- **HU**: HU-04 (panel de gobernanza)
- **Estado**: aprobado, pendiente de plan de implementación
- **Supersede**: `2026-07-04-sp5b-gobernanza-backend-design.md` §9 (sin reconciliación al arranque)
- **Compatible con**: `ADR-0013` (permisos funcionales como composites de Keycloak). No lo contradice.

## 1. Problema

Asignar «Gestionar partidas» al Administrador desde el panel no surte efecto. Tres problemas
encadenados:

1. **La gobernanza no sobrevive a un reinicio.** `umbral-realm.json` declara los composites por
   defecto y `keycloak-config` lo reaplica en cada `docker compose up`
   (`IMPORT_CACHE_ENABLED: "false"`, deliberado para reparar deriva). El panel escribe en
   `permisos_rol`, Keycloak lo pierde en el siguiente arranque, y el token nunca lleva el permiso.
2. **Los clientes autorizan por rol base, no por permiso.** El backend ya exige permisos funcionales;
   la web y el móvil gatean por rol. El panel escribe permisos que los clientes no leen.
3. **El panel ofrece un permiso que no gobierna nada útil.** `ParticiparEnPartidas` es asignable a
   cualquier rol, pero sólo el rol `Participante` tiene cliente donde jugar. Marcarlo para un
   Administrador no hace nada visible.

Este documento cubre el **sub-proyecto 1** de tres. Ver §7.

## 2. El modelo

**El realm declara lo fijo. La base de datos gobierna lo variable.** No se solapan, así que no
pelean por los mismos composites.

### Gobernable: dos privilegios, por rol

| Privilegio | Abre |
|---|---|
| `GestionarPartidas` | Área **Partidas**: `Partidas` + `Nueva partida` |
| `GestionarEquipos` | Área **Equipos**: `Creación de equipos` + `Gestión de equipos` + `Rendimiento de equipos` |

El privilegio abre el área **en el panel que corresponda al rol**: web para Administrador/Operador,
móvil para Participante (sub-proyecto 3).

### No gobernable: viene con el rol

| Capacidad | Concedida por | Razón |
|---|---|---|
| Área Identidad (usuarios + gobernanza) | Rol `Administrador` | Protegida. Si fuera gobernable, quitársela al rol Administrador cerraría el sistema sin llave: nadie podría volver a repartir privilegios. |
| Participar en partidas (jugar) | Composite fijo `Participante → ParticiparEnPartidas`, declarado en el realm | El permiso sigue existiendo en el dominio, pero **nadie puede moverlo**. Desde el panel es indistinguible de que no existiera. |
| Mi equipo (crear, invitar, liderar, transferir, salir) | Rol `Participante` | Es su función principal según el SRS: «crea equipos o se une a ellos por invitación, y puede ser líder». |

### Defaults

| Rol | Privilegios gobernables |
|---|---|
| Administrador | `GestionarEquipos` |
| Operador | `GestionarPartidas` |
| Participante | ninguno |

## 3. Decisiones

| # | Decisión | Alternativa descartada |
|---|---|---|
| D1 | `ParticiparEnPartidas` **se saca del panel y se fija** al rol `Participante` en el realm. No se elimina del dominio. | Eliminarlo del dominio: 37 archivos, incluidos SRS, modelo de dominio, diagrama de clases y el ADR-0013 aceptado, más las policies del runtime que puede estar usando el compañero. Funcionalmente indistinguible desde el panel, así que no compensa. |
| D2 | `GestionarEquipos` gobierna **sólo los paneles de administrar equipos ajenos**. El flujo propio del participante no requiere privilegio. | Un solo privilegio para ambos: dejaría al Participante sin poder crear equipos por defecto, contra el SRS. |
| D3 | El área **Identidad** depende del rol Administrador y está siempre visible. | Un tercer privilegio «Gestión de identidad»: permitiría cerrarse fuera del sistema. |
| D4 | La migración **resetea** los privilegios gobernables a los defaults nuevos. | Conservar lo asignado durante las pruebas: el estado real no coincidiría con el default definido. |
| D5 | **El realm sólo declara los composites fijos**; los gobernables los pone el reconciliador desde la DB. | (A) Dejar el realm declarando todo y que Identity restaure lo que `keycloak-config` borra: funciona, pero los dos sistemas siguen peleando. (C) Modo *managed* de config-cli: requiere verificar soporte en 6.5.1. |
| D6 | El permiso `ParticiparEnPartidas` **no es asignable por API**, no sólo oculto en la UI. El validador lo rechaza. | Ocultarlo sólo en el panel: un `PUT` a mano podría moverlo y romper el gameplay. |

## 4. Cambios por capa

### 4.1 Identity: distinguir gobernable de fijo

`PermisoFuncional` **no cambia**: conserva sus tres valores. Se añade la noción de qué es gobernable:

```csharp
public static class PermisosGobernables
{
    public static readonly IReadOnlySet<PermisoFuncional> Todos =
        new HashSet<PermisoFuncional> { PermisoFuncional.GestionarPartidas, PermisoFuncional.GestionarEquipos };
}
```

Consumidores:
- `ActualizarPermisosRolCommandValidator` rechaza cualquier permiso fuera del conjunto (D6).
- El reconciliador itera **sólo** sobre este conjunto (§4.4). Crítico: si iterase el enum completo,
  vería que la DB no declara `ParticiparEnPartidas` y **lo borraría de Keycloak**, tumbando el
  gameplay.

### 4.2 Base de datos

Reset de los privilegios gobernables, **ejecutado exactamente una vez**:

```sql
DELETE FROM permisos_rol;
INSERT INTO permisos_rol (rol, permiso) VALUES (1, 2), (2, 1);
-- Administrador->GestionarEquipos, Operador->GestionarPartidas, Participante->nada
```

`permisos_rol` pasa a contener **sólo lo gobernable**. La fila `(3, 3)`
(Participante→ParticiparEnPartidas) desaparece: ese composite pasa a vivir en el realm (§4.3).

El guardia es obligatorio. Hoy el seed usa `WHERE NOT EXISTS (SELECT 1 FROM permisos_rol)`, que no
sirve aquí porque la tabla ya tiene datos. Se añade una tabla `migraciones_aplicadas (nombre)` y el
reset corre sólo si su nombre no está registrado. **Sin ese guardia el `DELETE` correría en cada
arranque y borraría toda asignación hecha desde el panel** — un bug peor que el original.

### 4.3 Realm de Keycloak (`infra/keycloak/import/umbral-realm.json`)

| Rol | `composites` antes | `composites` después |
|---|---|---|
| Administrador | (ninguno) | (ninguno) |
| Operador | `["GestionarPartidas"]` | (ninguno) |
| Participante | `["GestionarEquipos", "ParticiparEnPartidas"]` | `["ParticiparEnPartidas"]` |

Los roles `GestionarPartidas`, `GestionarEquipos` y `ParticiparEnPartidas` siguen existiendo como
realm roles. Lo que cambia es quién los tiene declarado.

Resultado: `keycloak-config` reaplica el realm y **restaura el composite fijo** (que es lo deseado)
sin tocar los gobernables, porque ya no los declara. El conflicto desaparece de raíz.

### 4.4 Identity: reconciliador

Se recupera `PermisosRolKeycloakReconciler` de `backup/gobernanza-santiago` (`aa8085b`), verificado
en vivo. Ajustes:
- Itera `PermisosGobernables.Todos`, no el enum completo (§4.1). 3 roles × 2 permisos = 6 llamadas.
- Se conserva el `depends_on: keycloak-config` del compose: el reconciliador escribe sobre roles que
  `keycloak-config` debe haber creado antes.

### 4.5 Identity: el flujo de equipos del móvil

`TeamsController` (crear mi equipo, invitar, aceptar, transferir liderazgo, salir) y
`TeamInvitationsController` pasan de exigir `GestionarEquipos` a exigir el rol `Participante`.

**Este cambio está arrastrado por el nuevo default y no puede posponerse** (ver R1): el Participante
deja de tener `GestionarEquipos`, así que si la policy no cambia a la vez, pierde los equipos.

### 4.6 Panel web

- `GovernancePage.tsx:16-18`: la lista pasa de 3 a 2 permisos.
- `identityApi.ts:168`: el tipo **TypeScript** `PermisoFuncional` pierde `"ParticiparEnPartidas"`.

> **Ojo con el nombre.** `PermisoFuncional` designa dos cosas distintas: el **enum de C#**
> (`Domain/Enums`), que **conserva** sus tres valores porque el permiso sigue existiendo en el
> dominio (§4.1), y el **tipo de TypeScript** del cliente web, que sólo modela **lo que el panel
> puede gobernar** y por eso baja a dos. No es una contradicción: son capas distintas con el mismo
> nombre. Al implementar, considerar renombrar el tipo TS a `PermisoGobernable` para que el nombre
> no mienta.

El contrato HTTP no cambia de forma: `GET /identity/governance/roles` y
`PUT /identity/governance/roles/{rol}/permisos` siguen igual, con un valor menos en el conjunto
aceptado.

### 4.7 Sin cambios

- **Enum `PermisoFuncional`**, `SesionesController` y sus 10 policies de `ParticiparEnPartidas`.
- **SRS, modelo de dominio, diagrama de clases, ADR-0013, contratos.** El permiso sigue existiendo y
  el rol `Participante` lo sigue teniendo: la documentación sigue siendo cierta.
- **Gateway.** `/operaciones-sesion` sigue en `Default`: esa ruta la usan participantes (jugar) y
  operadores (operar la sesión).

## 5. Riesgos

| # | Riesgo | Mitigación |
|---|---|---|
| R1 | El Participante pierde `GestionarEquipos` (§4.2) mientras `TeamsController` aún lo exige → **el móvil pierde los equipos** (403 en crear/invitar/aceptar). | §4.2 y §4.5 viajan en el **mismo commit**. Verificación en vivo. |
| R2 | El reconciliador itera el enum completo, no ve `ParticiparEnPartidas` en la DB y **lo borra de Keycloak** → el gameplay se cae entero. | §4.1: itera `PermisosGobernables.Todos`. Test explícito de que el reconciliador **nunca** toca el composite fijo. |
| R3 | El `DELETE FROM permisos_rol` sin guardia borraría la gobernanza en cada arranque. | Tabla `migraciones_aplicadas` (§4.2). Test de que un segundo arranque respeta lo asignado después. |
| R4 | Un `PUT` a mano asigna `ParticiparEnPartidas` a otro rol y descuadra el modelo. | D6: el validador lo rechaza. Test de contrato con 400. |

## 6. Verificación

1. Tests del reconciliador (del respaldo), **más uno nuevo**: no toca el composite fijo (R2).
2. Test del validador: `PUT` con `ParticiparEnPartidas` → 400 (R4).
3. Tests de `TeamsController` con el rol `Participante`.
4. Panel web con 2 privilegios.

> **El guardia de la migración (R3) no se puede cubrir con un test automatizado.** Los
> `IntegrationTests` usan `UseInMemoryDatabase`, que no ejecuta SQL crudo ni bloques `DO $$`. Un test
> ahí no probaría nada. Se verifica en vivo (punto 6b), que es la única prueba que lo ejercita de
> verdad. Cubrirlo con un test exigiría Testcontainers, fuera del alcance de este sub-proyecto.
5. **Prueba en vivo, obligatoria**: arrancar, asignar un privilegio en el panel, reiniciar con
   `docker compose -f infra/docker-compose.yml --env-file .env up -d`, confirmar que el privilegio
   **sobrevive** y que el token lo lleva. Es exactamente lo que hoy falla.
6. **Prueba en vivo del guardia (R3)**: insertar una fila en `permisos_rol`, reiniciar Identity, y
   confirmar que **sigue ahí**. Si desaparece, la migración está borrando la gobernanza en cada
   arranque y hay que parar.
7. **Prueba en vivo del gameplay**: un participante entra al móvil, crea un equipo y juega. Descarta
   R1 y R2, que son los dos fallos que romperían el móvil entero.

## 7. Alcance: lo que NO entra

Sub-proyecto 1 de tres. Los otros dos van en specs propias:

| # | Sub-proyecto | Contenido |
|---|---|---|
| **2** | **Web gateada por privilegio** | Áreas y rutas del nav por privilegio, pantalla «sin accesos», y las policies restantes: paneles web de equipos (`AdminTeamsController`, `TeamsAdminController`, `EquiposController` de Puntuaciones) a `GestionarEquipos`; GET de Partidas a `GestionarPartidas`. Incluye rehacer la extracción de permisos del token (`keycloak.ts`), revertida con `2fabefd`. **Es el que resuelve el síntoma original.** |
| **3** | **Paneles de gestión nativos en el móvil** | Un Participante con privilegios opera partidas y equipos desde el móvil. Medido: ~3.840 líneas de producción + ~1.840 de tests a reescribir en React Native (`CreatePartidaPage` 880, `SesionOperadorPage` 665, `TeamsAdminPage` 665, entre otros), sin reutilizar código web (DOM y CSS frente a `View`/`StyleSheet`), más `react-native-maps`. Decidido con los números delante. |

Orden: **1 → 2 → 3**. El 3 depende del 1. El 2 es el que resuelve hoy el síntoma que originó todo.

### Nota sobre las policies compuestas (sub-proyecto 2)

Los permisos son **aditivos, no sustitutivos**. Al alinear las policies del sub-proyecto 2 hay que
usar **rol base AND permiso**, no sólo el permiso: los servicios están expuestos en puertos propios
(5001, 5010…), así que una policy de sólo permiso podría saltarse el filtro por rol del gateway.
Ejemplo concreto: `AdminTeamsController` con sólo `GestionarEquipos` dejaría a cualquiera con ese
privilegio borrar equipos ajenos llamando al puerto 5001 directamente.

## 8. Notas de estado

- Trabajo previo aparcado en `backup/gobernanza-santiago` (`aa8085b`): el reconciliador verificado en
  vivo y un intento de gateo del nav por permiso.
- `feature/fixes-santiago` tiene la gobernanza revertida a cero (`60ce104` revierte `2fabefd`).
  Frontend en verde: 245/245.
- La base de datos actual tiene el Administrador con `GestionarPartidas` y `ParticiparEnPartidas`,
  asignados durante las pruebas. El reset de §4.2 los limpia.
