# SP-3f-3 — Geolocalización BDT (relay al operador vía SignalR)

- **Fecha:** 2026-07-01
- **Slice:** SP-3f-3 (cuelga del seam SignalR de SP-3f-2; hub alcanzable vía gateway tras SP-3g)
- **Servicio dueño:** Operaciones de Sesión
- **Cliente objetivo:** backend
- **Estado:** Diseño aprobado — pendiente de plan de implementación
- **Rama:** `feature/code-migration-SP-3`

## Contexto y regla de dominio

`CLAUDE.md` / dominio: la **geolocalización es obligatoria en un `JuegoBDT` activo**
(BR-B07). El participante autoriza en el móvil (RNF-19) y su app envía la ubicación
**~cada 2 segundos al operador** (RNF-15: sin bloquear; RNF-17: el canal en tiempo
real incluye geolocalización). El operador la consume en su mapa de operación. Los
participantes **no** ven ubicaciones ajenas.

SP-3f-2 dejó el `SesionHub` con dos métodos client→servidor (`SuscribirAPartida`,
`DesuscribirDePartida`), grupos `partida:{id}`, y difusión servidor→cliente de 10
transiciones de estado. SP-3g dejó el hub alcanzable a través del gateway bajo el
prefijo `operaciones-sesion/hubs/sesion`. SP-3f-3 añade el **canal de ubicación**
sobre ese mismo hub.

Estado actual relevante (`SesionHub.cs`):
- `SuscribirAPartida(Guid partidaId)`: rama operador (bypass, `IsInRole("Operador")`)
  y rama participante (lee claim `sub` → `participanteId`,
  `GetByParticipanteActivoAsync`, verifica `sesion.PartidaId == partidaId`). Ambas
  ramas terminan en `Groups.AddToGroupAsync(..., GrupoPartida(partidaId), ...)`.
  **Hoy no guarda nada en `Context.Items`.**
- `SesionRealtimeMessages`: 10 consts vía `nameof` + `GrupoPartida(Guid) => "partida:{id}"`.
- `SesionRealtimePayloads`: 10 records.
- DI: `TimeProvider.System` registrado como singleton en la capa Application
  (`Application/DependencyInjection.cs`), inyectable en el hub para server-stampear.

## Enfoque elegido — relay puro, sin cambio de dominio

El participante emite su ubicación por un método hub client→servidor; el servidor la
**reenvía** en tiempo real solo al operador de esa partida. No se persiste, no se
emite evento de dominio, no se toca ninguna entidad. Estado transitorio puro.

Decisiones cerradas en brainstorming:
- **Transporte:** método hub client→servidor `EnviarUbicacion` (no HTTP; encaja en el
  canal WS de tiempo real, coherente con `Suscribir/Desuscribir`).
- **Persistencia:** ninguna (solo reenvío). No hay `last-known`, no hay escritura DB.
- **Targeting:** grupo operador-scoped `operador:partida:{id}`; los operadores se
  auto-añaden en `SuscribirAPartida`. Garantiza que solo el operador recibe.

Alternativas descartadas:
- **Endpoint HTTP por tick:** rompe RNF-15 (una request+respuesta HTTP cada 2s por
  participante), fuera del canal de tiempo real.
- **Persistir cada tick / last-known:** lectura+escritura DB por tick, contra RNF-15;
  la persistencia/auditoría de ubicación es de otra capa (audit vía broker, diferida).
- **Difundir al grupo `partida:{id}`:** filtraría ubicaciones a los participantes
  (viola BR-B07: solo el operador ve el mapa).

## Cambios de producción (servicio Operaciones)

Todos en `Api/Realtime/` (más DI del hub). Cero cambios de Domain/Application/Infra
salvo inyección de `TimeProvider` ya disponible.

1. **`SesionRealtimeMessages`:**
   - `public const string UbicacionActualizada = nameof(UbicacionActualizada);`
   - `public static string GrupoOperadorPartida(Guid partidaId) => $"operador:partida:{partidaId}";`

2. **`SesionRealtimePayloads`:**
   - `public sealed record UbicacionParticipantePayload(Guid PartidaId, Guid ParticipanteId, double Latitud, double Longitud, DateTime TimestampUtc);`

3. **`SesionHub`:**
   - Inyectar `TimeProvider` en el ctor (junto a `ISesionPartidaRepository`).
   - Claves de `Context.Items` como consts privadas (p. ej. `"partidaId"`,
     `"participanteId"`).
   - **`SuscribirAPartida` extendido:**
     - Rama **operador:** además de `GrupoPartida`, añadir a
       `GrupoOperadorPartida(partidaId)` (para recibir `UbicacionActualizada`).
     - Rama **participante:** tras validar, guardar `partidaId` y `participanteId` en
       `Context.Items` (para que `EnviarUbicacion` no re-lea DB por tick). Sigue en
       `GrupoPartida` únicamente (NO en el grupo operador).
   - **Nuevo `EnviarUbicacion(double latitud, double longitud)`:**
     - Recupera `partidaId`+`participanteId` de `Context.Items`; si faltan (no
       suscrito como participante, u operador) → `HubException`.
     - Valida rango: `latitud ∈ [-90, 90]`, `longitud ∈ [-180, 180]`; fuera de
       rango → `HubException`.
     - `TimestampUtc` = `_timeProvider.GetUtcNow().UtcDateTime` (server-stamped).
     - `Clients.Group(GrupoOperadorPartida(partidaId)).SendAsync(UbicacionActualizada,
       new UbicacionParticipantePayload(...), Context.ConnectionAborted)`.

## Contrato del hub

- **Cliente→servidor** `EnviarUbicacion(latitud, longitud)` — solo participante;
  requiere `SuscribirAPartida` previo (la partida se toma de la conexión, **no** como
  parámetro → no se puede reportar por otra partida). Coords validadas.
- **Servidor→cliente** `UbicacionActualizada`
  `{ partidaId, participanteId, latitud, longitud, timestampUtc }` — entregado **solo**
  al grupo operador. Los participantes nunca lo reciben.
- **`SuscribirAPartida`** ahora también añade operadores al grupo operador (cambio de
  comportamiento aditivo; las suscripciones de estado de SP-3f-2 no cambian).

## Validación / privacidad / no-bloqueo

- **Sin lectura DB por tick:** `EnviarUbicacion` solo lee `Context.Items` (memoria de
  conexión). Cumple RNF-15 ("cada 2s sin bloquear").
- **Sin chequeo server-side de tipo-de-juego por tick:** el móvil solo transmite
  durante un BDT activo (BR-B07/RNF-19, enforced en cliente). Un gate estricto
  server-side exigiría lectura por tick → simplificación deliberada, documentada.
- **Privacidad (BR-B07):** el grupo operador-scoped garantiza que solo el operador
  recibe; el participante emisor no está en ese grupo.
- **Auth:** `[Authorize]` de clase cubre el método. Operadores quedan naturalmente
  bloqueados de emitir (no tienen `Context.Items` de participante).
- **Reconexión:** al reconectar y re-suscribirse, `Context.Items` se re-puebla
  (misma conexión nueva → nuevo `SuscribirAPartida`). Coherente con el modelo de
  SP-3f-2 (los grupos se re-crean por conexión).

## Estrategia de pruebas

- **UnitTests `SesionHubTests`** (extender; fakes a mano, sin Moq):
  - `EnviarUbicacion` con participante suscrito → invoca `SendAsync` al grupo
    `operador:partida:{id}` con `UbicacionActualizada` y payload de coords + timestamp
    del `FakeTimeProvider`.
  - Coords fuera de rango (lat > 90, lat < -90, lng > 180, lng < -180) → `HubException`,
    sin `SendAsync`.
  - Sin suscripción previa (sin `Context.Items`) → `HubException`.
  - Operador llamando `EnviarUbicacion` → `HubException` (no tiene Items de
    participante).
  - `SuscribirAPartida` **operador** → añadido a `partida:{id}` **y**
    `operador:partida:{id}`.
  - `SuscribirAPartida` **participante** → `Context.Items` seteados + solo
    `partida:{id}` (NO en el grupo operador).
  - Extender los fakes existentes: `FakeGroupManager` (ya registra grupos),
    `FakeHubCallerContext` (exponer `Items` mutable), añadir captura de `SendAsync`
    en el fake de `IHubCallerClients`/`IClientProxy` para el grupo operador,
    inyectar `FakeTimeProvider` (o `TimeProvider` de test) para timestamp
    determinista.
- **ContractTests / doc↔constantes:** añadir `UbicacionActualizada` a la lista de
  mensajes que el test de contrato verifica contra el doc.
- **Gap documentado:** el relay WS end-to-end no es testeable en el harness (misma
  clase que los gaps WS/e2e de SP-3f-2 y SP-3g); cubierto por unit tests del hub.

## Contrato y documentación

- `contracts/http/operaciones-sesion-api.md`, sección Realtime:
  - Añadir `EnviarUbicacion(latitud, longitud)` a la tabla client→servidor.
  - Añadir `UbicacionActualizada` a la tabla servidor→cliente, marcado
    **operador-only**, con el shape del payload.
  - Nota de privacidad (solo el operador recibe) y de no-persistencia (relay).
- Fila de traceability SP-3f-3 (carve-out: se **escribe, no se commitea**).
- Al cerrar el slice, actualizar/crear memoria del canal de ubicación si aporta.

## Fuera de alcance / forward-looking

- **Persistencia / last-known / historial de ubicación** — otra capa; audit de
  ubicación (`TipoEventoHistorial.Ubicacion`) va vía broker RabbitMQ (diferido).
- **`GeolocalizacionAutorizada`** (toggle de permiso en `ParticipanteBDT`) — se
  materializa cuando aterrice el modelo de participación BDT; el permiso móvil
  (RNF-19) es client-side.
- **Gate server-side de tipo-de-juego por tick** — diferido (requeriría lectura por
  tick; contra RNF-15).
- **Cableado de clientes** (móvil emisor / mapa web del operador) — clients, no este
  slice backend.
- **Modalidad Equipo** — la ubicación es por participante; la agregación por equipo,
  si aplica, es trabajo del slice-E.

## Riesgos

- **`Context.Items` sin precedente en el servicio:** patrón estándar de SignalR
  (estado por conexión), pero primer uso aquí. Mitigación: claves como consts,
  cubierto por unit tests (participante puebla Items; operador no; falta de Items →
  HubException).
- **Doble grupo por operador:** el operador queda en `partida:{id}` **y**
  `operador:partida:{id}`. Sin solape de mensajes (los de estado van a `partida:{id}`,
  la ubicación a `operador:partida:{id}`), pero `DesuscribirDePartida` hoy solo quita
  de `partida:{id}` → añadir la remoción del grupo operador para operadores (evita
  fuga tras desuscripción en la misma conexión). Se detalla en writing-plans.
- **Falta de gate de tipo-de-juego:** un cliente malicioso podría emitir ubicación en
  una partida no-BDT; impacto acotado (solo el operador de esa partida lo ve, sin
  persistencia). Aceptado como simplificación; endurecible luego con el gate diferido.

## Descomposición tentativa (se detalla en writing-plans)

1. `SesionRealtimeMessages` (const + helper grupo operador) + `SesionRealtimePayloads`
   (record ubicación) — mecánico.
2. `SesionHub`: inyectar `TimeProvider`, `Context.Items` en `SuscribirAPartida`,
   auto-join operador al grupo operador (+ remoción en `DesuscribirDePartida`),
   nuevo `EnviarUbicacion` con validación y relay + unit tests.
3. Contrato (`operaciones-sesion-api.md` sección Realtime) + test doc↔constantes +
   fila de traceability (carve-out).
