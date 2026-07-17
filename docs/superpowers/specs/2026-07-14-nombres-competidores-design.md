# Nombres de competidores en pantallas de operador y participante (design)

Fecha: 2026-07-14
Origen: verificación a pedido del equipo sobre qué identifica el operador en sus pantallas. Hallazgo:
ninguna superficie de sesión, ranking, geolocalización, pistas ni historial muestra nombres — solo
GUIDs, la mayoría recortados a 8 caracteres.

## Problema

`competidorId` viaja como `Guid` desde Operaciones de Sesión hasta Puntuaciones y llega a los
clientes sin ningún nombre asociado. Los contratos no lo llevan: `contracts/http/puntuaciones-api.md`
define las entradas de ranking solo con `competidorId` + `tipoCompetidor`, y lo mismo ocurre con
lobby, inscripciones y geolocalización en Operaciones de Sesión.

Consecuencia operativa concreta: un operador que entrega una pista elige entre `a3f81b2c` y
`7d0e4f91` sin saber a quién corresponde (`PistasPanel.tsx:57`).

La causa no es de UI. Los nombres viven en Identity y ningún servicio puede leer su base
(frontera dura, CLAUDE.md). Falta una vía de resolución.

## Alcance

**Refinamiento transversal de usabilidad.** No mapea a una `HU-xx` única: corrige superficies ya
implementadas de lobby, ranking en vivo, ranking consolidado, pistas, geolocalización BDT e
historial de partida. No añade capacidad de negocio, no cambia reglas de dominio ni el cálculo de
ningún ranking.

**Fuera de alcance:**

- Nombres de **partida** y de **juego** (`RendimientoEquipoPage.tsx:118`,
  `ConvocatoriasScreen.tsx:78`, `HistorialPartidaPage.tsx:137`). Pertenecen al servicio Partidas,
  no a Identity: necesitan otro endpoint en otro servicio y son un slice propio.
- Cualquier cambio a eventos RabbitMQ, proyecciones de Puntuaciones o contratos de Puntuaciones.

## Decisiones tomadas en brainstorming

1. **Nombre real completo** (`Usuario.Nombre`), igual para operador y participante. No se introduce
   alias ni `NombreVisible`: sin campo nuevo en el dominio y sin HU nueva. Se acepta que en modalidad
   `Individual` un participante vea el nombre real de los demás competidores en el ranking.
2. **Nombre actual siempre**, en todas las superficies incluidas las históricas. Un consolidado de
   una partida vieja muestra el nombre vigente hoy, no el que el equipo tenía al jugar. Se acepta
   explícitamente la divergencia con el historial de nombres de equipo (BR-E11), que sigue existiendo
   como capacidad propia y no se toca.
3. **Enfoque A — endpoint de directorio en Identity.** Descartados: denormalizar nombres en eventos
   (contradice la decisión 2 por construcción, y exigiría registrar el payload hoy indefinido de
   `UsuarioCreado`, añadir eventos de renombrado y hacer backfill) y agregar en el gateway (viola
   "el gateway no posee lógica de dominio", CLAUDE.md).
4. **Registro como refinamiento transversal** en `SPECS-LIST.md`, citando las HU afectadas, en vez
   de inventar un número de HU.

### Caveat aceptado

Cualquier usuario autenticado puede resolver el nombre de un GUID que ya conozca. No habilita
enumerar el directorio — los GUIDs no son adivinables y la respuesta solo devuelve lo pedido — pero
es una relajación real frente al estado actual, donde `/identity/users/**` está cerrado a
`Administrador` en el gateway. Aceptada como consecuencia directa de la decisión 1.

## Hechos verificados en código

Anclan el diseño; no repetir la verificación al implementar.

- `Usuario.UsuarioId` es `Guid`; `Usuario.KeycloakId` es `string`
  (`Umbral.IdentityService.Domain/Entities/Usuario.cs:8-9`).
- `ParticipanteEquipo.UsuarioId` es un `Guid` que guarda **el sub de Keycloak**, no el `UsuarioId`
  local. `ListarEquiposQueryHandler` ya resuelve nombres parseando `Usuario.KeycloakId` a `Guid`, y
  cae a `""` cuando no hay fila local. Este slice reusa ese patrón.
- `competidorId` es `Guid` y viaja junto a `TipoCompetidor` (`Participante`/`Equipo`) en los DTOs de
  Puntuaciones (`RankingJuegoResponse`, `RankingConsolidadoResponse`, `MarcadorResponse`). El
  resolvedor despacha por ese enum, no adivina.
- Clave de búsqueda: sub de Keycloak contra `Usuario.KeycloakId` para participantes; `equipoId` para
  equipos.
- `TeamsAdminController` es el precedente de un controlador que sale de la policy de clase de
  `TeamsController` porque `GestionarEquipos` es aditiva y los roles necesarios no lo tienen.

## Arquitectura

### Servicios tocados

- **Identity** (único backend): endpoint de directorio de nombres.
- **Web** y **Móvil**: módulo de resolución + sustitución en las superficies listadas.
- **Gateway**: sin cambios de código. Solo documentación.

### Backend — `POST /identity/directory/names`

`DirectoryController` nuevo, `[Route("identity/directory")]`, `[Authorize]` a secas (autenticado,
sin exigir rol). Controlador propio y no dentro de `UsersController` porque ese está bajo
`AdminOnly` — mismo razonamiento que ya documenta `TeamsAdminController`.

```jsonc
// POST /identity/directory/names
{ "participanteIds": ["guid", ...], "equipoIds": ["guid", ...] }

// 200
{
  "participantes": [{ "participanteId": "guid", "nombre": "María González" }],
  "equipos":       [{ "equipoId": "guid", "nombreEquipo": "Los Cazadores" }]
}
```

`POST` y no `GET` por el largo del query string: un ranking de 50 competidores son ~1.850 caracteres
de GUIDs, incómodamente cerca del límite práctico de URL. Sigue siendo una **query** en CQRS
(`ResolverNombresQuery` + `ResolverNombresQueryHandler`): no muta estado.

Piezas, siguiendo la estructura estándar del servicio:

| Capa | Archivo |
|---|---|
| `Api/Controllers/` | `DirectoryController.cs` |
| `Application/Queries/` | `ResolverNombresQuery.cs` |
| `Application/Handlers/Queries/` | `ResolverNombresQueryHandler.cs` |
| `Application/DTOs/` | `NombresResponse.cs` (+ items de participante y equipo) |
| `Application/Validators/` | tope de 200 ids por request, contando `participanteIds` + `equipoIds` **sumados** |

El handler reusa `IUsuarioRepository` e `IEquipoRepository`. Ninguna lectura cruzada de bases.

**Un id que no resuelve se omite de la respuesta.** Divergencia deliberada del precedente de
`ListarEquiposQuery`, que devuelve `""`: omitirlo deja que el cliente caiga al GUID corto, más útil
que una celda vacía.

### Gateway

Sin cambios de código. `/identity/{**catch-all}` (Order 2) ya aplica `Default (autenticado)`, que es
justo la política que este endpoint necesita, y las rutas de `Administrador`/`Participante` son
Order 1 y no lo interceptan. Solo se documenta la fila en la matriz de `contracts/http/gateway-api.md`.

### Clientes

Dos módulos nuevos por cliente, una responsabilidad cada uno:

| Cliente | Archivo | Responsabilidad |
|---|---|---|
| web | `frontend/src/api/directoryApi.ts` | solo el fetch, como los demás `*Api.ts` |
| web | `frontend/src/features/shared/useNombres.ts` | hook con caché y fallback |
| móvil | `mobile/src/features/shared/directoryApi.js` | fetch (convención `*Api.js` por feature) |
| móvil | `mobile/src/features/shared/useNombres.js` | hook con caché y fallback |

Contrato del hook:

```ts
const nombreDe = useNombres({ participanteIds, equipoIds }, accessToken);
// nombreDe(id) → "María González"  |  "a3f81b2c" si no resuelve
```

El fallback vive dentro del hook: **ninguna pantalla necesita saber que la resolución puede fallar**.
Piden un nombre y siempre reciben algo pintable.

**La caché es incremental, no un fetch por pantalla.** Un `Map` a nivel de módulo guarda lo resuelto;
el efecto compara los ids pedidos contra la caché y pide solo los faltantes, troceando en lotes de
200 (sumando ambas listas, igual que el validador). No es optimización prematura sino requisito
funcional: en la sesión en vivo aparecen
competidores nuevos por push de SignalR (inscripciones, entradas de ranking), y un hook que
resolviera solo al montar los dejaría como GUID para siempre.

### Superficies a sustituir

| Cliente | Archivo | Qué cambia |
|---|---|---|
| web | `SesionOperadorPage.tsx:462,483,507,523` | lobby individual, lobby equipos e inscritos (hoy GUID completo, sin recortar) |
| web | `runtimeShared.tsx:58` | ranking en vivo de Trivia y BDT |
| web | `ConsolidadoPanel.tsx:105` | ranking consolidado |
| web | `GeoMapPanel.tsx:45` | etiquetas del mapa BDT |
| web | `PistasPanel.tsx:57` | `<option>` del selector de destinatario |
| web | `HistorialPartidaPage.tsx:138,139` | columnas participante y equipo |
| móvil | `liveShared.tsx:35` | ranking en vivo |
| móvil | `ConvocatoriasScreen.tsx:79` | equipo de la convocatoria |

En móvil la entrada propia del ranking ya se resalta contra `miSub` (`PartidaLiveScreen.tsx`): se
muestra "Tú" en esa fila y el nombre real en las demás.

## Manejo de errores

**Principio rector: la resolución de nombres nunca puede romper la operación.** Si Identity está
caído o la red falla, el hook se queda con los GUID cortos y la pantalla sigue funcionando entera.
Un operador debe poder arrancar la partida, entregar pistas y ver el ranking aunque el directorio no
responda. Degradar a lo que se ve hoy es aceptable; bloquear la sesión no. El fallo se traga en el
hook y no burbujea a un error de pantalla.

| Caso | Comportamiento |
|---|---|
| Identity caído / error de red | GUID corto, sin error visible, pantalla operativa |
| Usuario dado de baja o equipo eliminado | ausente en la respuesta → GUID corto |
| `401` | lo maneja el flujo de refresh cliente↔Keycloak ya existente |
| Lote > 200 ids | el hook trocea antes de pedir; el `400` del validador es red de seguridad del servidor, no un caso que el cliente deba alcanzar |

## Testing

| Nivel | Qué cubre |
|---|---|
| Identity unit | `ResolverNombresQueryHandler`: resuelve ambos tipos, omite ids desconocidos, tolera `KeycloakId` no parseable a `Guid` (caso real, ya contemplado en `ListarEquiposQueryHandler`) |
| Identity controller unit | `DirectoryController` despacha por MediatR sin lógica propia (obligatorio por las directivas) |
| Identity integration | `401` sin token · `200` con rol `Participante` · `200` con `Operador` · `400` con 201 ids |
| Contract | forma de la respuesta contra `contracts/http/identity-api.md` |
| Web (vitest) | `useNombres`: no repite fetch de ids cacheados; pide solo los faltantes al llegar competidores nuevos por push. Es la lógica con riesgo real |
| Móvil (`node --test`) | resolvedor y fallback |

**El test de permisos que importa es que un `Participante` pueda llamar al endpoint.** Es
contraintuitivo en este repo, donde todo `/identity/users/**` es `AdminOnly`, y sin un test explícito
es el tipo de cosa que alguien endurece "arreglando" y rompe el móvil en silencio.

Los tests actuales de `ConsolidadoPanel`, `runtimeShared` y las demás pantallas listadas asertan hoy
el GUID recortado y hay que actualizarlos. Es un cambio de comportamiento legítimo y no toca ningún
`data-testid`, `label` ni rol ARIA — lo que protege la regla de rediseño del CLAUDE.md.

## Documentación a actualizar

- `contracts/http/identity-api.md` — sección nueva para el directorio de nombres.
- `contracts/http/gateway-api.md` — fila en la matriz (documentación; sin cambio de código).
- `docs/04-sdd/SPECS-LIST.md` — fila del slice como refinamiento transversal, citando las HU afectadas.
- `docs/04-sdd/traceability-matrix.md`.
