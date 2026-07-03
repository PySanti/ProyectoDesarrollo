# SP-3i — Backbone RabbitMQ: publisher dual-write, ubicación, re-push de convocatorias, consumidor de humo

- **Slice:** SP-3i (pre-SP-4). Base: SP-3h completo (`02f3bf6`), rama `feature/sp-3-audit`.
- **Servicios:** Operaciones de Sesión (publisher, seam, hub) + Puntuaciones (consumidor de humo) + `contracts/events/` + `docs/05-decisions/` (ADR-0012).
- **Cierra diferimientos:** broker RabbitMQ real (dual-write), audit de ubicación (transporte), replay de `ConvocatoriaCreada` (re-push SignalR al conectar).
- **Decisiones del responsable (brainstorm 2026-07-03):** RabbitMQ.Client crudo (no MassTransit); alcance máximo (publisher + humo + ubicación + re-push); entrega best-effort con outbox transaccional diferido; re-push = SignalR al conectar (no involucra broker).
- **Fuera de alcance:** proyecciones/scoring en Puntuaciones (SP-4), outbox transaccional, publisher RabbitMQ en Identity/Partidas, persistencia de pistas o ubicaciones, retry/confirms de publicación, guard automatizado del doc de eventos, clientes web/móvil.

## 1. Objetivo

Los 16 eventos del seam `ISesionEventsPublisher` viven hoy en No-Op + SignalR; Puntuaciones (SP-4) necesita consumirlos por RabbitMQ. Este slice: (a) define la topología y el envelope canónicos en el contrato; (b) implementa el primer publisher RabbitMQ real del repo (dual-write vía el Composite existente); (c) transporta la ubicación BDT al broker para la auditoría diferida; (d) cierra el hueco UX del convocado offline re-emitiendo sus convocatorias pendientes al conectar; (e) prueba el flujo end-to-end con un consumidor de humo en Puntuaciones que además deja la cola declarada para SP-4.

## 2. Topología y envelope (contrato)

- **Exchange:** `umbral.operaciones-sesion`, tipo `topic`, durable. Convención de precedente: un exchange por servicio productor (`umbral.identity`, `umbral.puntuaciones` cuando existan).
- **Routing key:** `operaciones-sesion.<evento-kebab>.v1`. Ejemplos: `operaciones-sesion.partida-publicada-en-lobby.v1`, `operaciones-sesion.etapa-bdt-ganada.v1`, `operaciones-sesion.ubicacion-actualizada.v1`. La versión va en la routing key y en el envelope; cambios incompatibles de payload → `v2` (nueva key, sin romper consumidores v1).
- **Envelope** (JSON camelCase, `content_type: application/json`):

```json
{
  "eventId": "guid",
  "eventType": "EtapaBDTGanada",
  "version": 1,
  "occurredAt": "datetime (UTC, momento de publicación)",
  "payload": { "…": "el shape documentado por evento en operaciones-sesion-events.md" }
}
```

- **Idempotencia:** el productor no garantiza exactly-once; los consumidores deduplican por `eventId` (regla del catálogo: consumers idempotentes). `eventId` se genera por publicación.
- **Cola de humo:** `puntuaciones.operaciones-sesion.all`, durable, binding `operaciones-sesion.#`. SP-4 podrá declarar colas más finas por routing key.
- **Documentación:** `contracts/events/operaciones-sesion-events.md` gana una sección "Transport (SP-3i)" con exchange, envelope, tabla evento→routing key (los 17), cola de humo, y la política best-effort/outbox-diferido. La sección "## Rule" (que decía que exchanges/keys se definen por un SDD futuro) se reemplaza: este spec es ese SDD. Las notas "No-Op port" por evento se actualizan a "publicado al broker desde SP-3i (best-effort)".

## 3. Publisher RabbitMQ (Operaciones de Sesión)

- **`RabbitMqSesionEventsPublisher : ISesionEventsPublisher`** en `Infrastructure/Services`. RabbitMQ.Client oficial (paquete `RabbitMQ.Client`), sin frameworks.
- Conexión: `IConnection` lazy singleton (se crea al primer publish; `DispatchConsumersAsync` no aplica — solo publica); un `IModel` por operación de publish o canal reutilizado con lock simple — decisión fina en el plan, criterio: simpleza sobre throughput.
- Declara el exchange (idempotente, `ExchangeDeclare` durable) al abrir conexión.
- **Best-effort estricto:** todo el publish envuelto en try/catch; fallo → `LogError` con `eventType`/routing key y **retorna sin lanzar**. Ni el request HTTP ni el scheduler fallan jamás por el broker (defensa doble: el Composite ya aísla por delegado — verificar en plan; el publisher además no lanza por diseño).
- **Config** (`.env` / `appsettings`): `RabbitMq__Host`, `RabbitMq__Port` (default 5672), `RabbitMq__User`/`RabbitMq__Password` (default guest/guest), `RabbitMq__Enabled` (default false). Sin `Enabled=true` + host → el publisher **no se registra** en el Composite y el comportamiento actual queda intacto (NoOp + SignalR).
- **Registro DI:** `Program.cs` — el array del Composite pasa a `[NoOp, SignalR, RabbitMq?]` (el tercero condicional por config).
- Serialización: `System.Text.Json` camelCase + enums como string (consistente con la API).

## 4. Ubicación al broker (audit diferido — transporte)

- **Seam:** método 17 en `ISesionEventsPublisher`: `PublicarUbicacionActualizadaAsync(UbicacionActualizadaEvent evento, CancellationToken ct)`.
- **Evento:** `UbicacionActualizadaEvent(Guid PartidaId, Guid ParticipanteId, double Latitud, double Longitud, DateTime Instante)` en `Application/Interfaces`. **Sin `SesionPartidaId`** (deliberado: el hub no lo tiene en `Context.Items` y no se hace query por cada ubicación ~2s; Puntuaciones resuelve por `PartidaId` si lo necesita). Documentar esta excepción en el contrato.
- **Impls:** NoOp → no-op; **SignalR → `Task.CompletedTask` con comentario** (el relay vivo al grupo operador lo hace el hub directamente — no duplicar el push); Composite → delega como siempre; RabbitMq → publica con routing key `operaciones-sesion.ubicacion-actualizada.v1`; Fake de tests → registra.
- **Hub `EnviarUbicacion`:** tras el relay SignalR actual (intacto, BR-B07: solo grupo operador), dispara el seam con el mismo payload de datos. El hub obtiene `ISesionEventsPublisher` por DI de constructor. Best-effort hereda del publisher; el relay nunca espera al broker para completarse funcionalmente (la llamada es `await` pero el publisher no lanza ni bloquea más allá del publish).
- Volumen asumido: ~1 evento / 2 s / participante BDT activo. Sin confirms, sin batching (YAGNI; si SP-4 sufre, se revisa).

## 5. Re-push de convocatorias pendientes al conectar (SignalR, sin broker)

- **`SesionHub.OnConnectedAsync`:** si el caller es participante (no `Operador`), resuelve `sub` del JWT y despacha **`ObtenerMisConvocatoriasPendientesQuery` vía `ISender`** (MediatR — NO repositorio directo: ADR-0011 sanciona el repositorio solo para membresía de grupos en el handshake de suscripción; esto es entrega de datos y va por la query existente, como hace `MantenimientoSesionesWorker`).
- Por cada convocatoria pendiente devuelta, emite al `Clients.Caller` el mensaje `ConvocatoriaCreada` con el `ConvocatoriaCreadaPayload` existente (mismo shape que el push original — el móvil lo procesa idéntico).
- Errores: try/catch alrededor del re-push; fallo → log warning, la conexión NO se rechaza (el re-push es cortesía, no gate).
- Sin persistencia de entregas ni dedup server-side: recibir la misma convocatoria pendiente en cada reconexión es aceptable (el móvil pinta estado, no acumula).
- `OnConnectedAsync` llama `base.OnConnectedAsync()` al final.

## 6. Consumidor de humo (Puntuaciones)

- **`OperacionesSesionEventsConsumer : BackgroundService`** en `Umbral.Puntuaciones.Api` (o `Infrastructure/Services` con hosted registration — seguir el layout del esqueleto existente; regla graduada aplica).
- Al arrancar (si `RabbitMq__Enabled=true`): conexión, `ExchangeDeclare` idempotente de `umbral.operaciones-sesion`, `QueueDeclare` durable `puntuaciones.operaciones-sesion.all`, `QueueBind` con `operaciones-sesion.#`, consumo con **manual ack**.
- Por mensaje: deserializa el envelope y `LogInformation` estructurado (`eventType`, `eventId`, routing key, `occurredAt`). **Sin DB, sin proyecciones, sin lógica** — es humo + declaración de infraestructura para SP-4. Envelope malformado → `LogWarning` + ack (no requeue infinito).
- Sin broker configurado o conexión fallida → `LogWarning` y el servicio **levanta igual** (el BackgroundService termina/reintenta suave, no tumba el host).
- Config espejo de la del publisher (`RabbitMq__*`).

## 7. ADR-0012 — Entrega best-effort, outbox diferido

`docs/05-decisions/ADR-0012-publicacion-eventos-best-effort.md` (Accepted): la publicación al broker ocurre después de `SaveChanges`, fuera de transacción; un crash entre save y publish pierde el evento. Se acepta porque: (a) Puntuaciones es un modelo de proyección reconstruible; (b) el outbox transaccional (tabla + dispatcher + idempotencia de despacho) duplica el slice y se difiere con criterio de activación explícito (cuando SP-4 materialice datos no-reconstruibles o el volumen lo exija). Referencia el patrón save→publish verificado por la auditoría (D7).

## 8. Testing

- **Unit (Operaciones):** builder de routing keys (kebab + v1, los 17 casos representativos); envelope (camelCase, `eventId` único por publish, `payload` anidado correcto); `RabbitMqSesionEventsPublisher` con conexión fake que lanza → no propaga y loguea; hub re-push (participante con 2 pendientes recibe 2 mensajes `ConvocatoriaCreada` al conectar; sin pendientes → 0; operador → 0; fallo de query → conexión viva); hub `EnviarUbicacion` dispara el seam (fake publisher registra) además del relay; SignalR publisher ubicación = no-op.
- **Unit (Puntuaciones):** parseo del envelope del consumidor (válido → log fields correctos; malformado → warning + ack). Lo conectable a broker real queda en integration.
- **Integration opt-in:** round-trip real publisher→exchange→cola→consumidor con broker de `docker-compose`; el test se **salta** (Skip) si `RABBITMQ_TEST_HOST` no está definido — CI y suites locales no se acoplan a docker. Documentar en el test cómo correrlo.
- **Regresión:** 3 suites de Operaciones verdes (Unit crece desde 327; Integration 28 + condicionales; Contract 48 sin cambios). Puntuaciones: su suite de tests (crear proyectos de test si el esqueleto no los tiene — verificar en plan; si no existen, crearlos es parte del slice con el layout estándar `tests/`).
- **Humo manual documentado:** compose up rabbitmq + ambos servicios; management UI (15672) muestra exchange/cola/tráfico al operar una partida.

## 9. Riesgos y mitigaciones

- **Dual-write sin transacción** → ADR-0012, aceptado y documentado; reconstruible.
- **Hub gana dependencia de `ISender` y del seam** → sigue dentro de los límites ADR-0011 (la membresía sigue por repositorio; datos por MediatR; relay puro se mantiene). El ADR-0011 no necesita cambios: re-push y publish no son "membresía de grupos".
- **Conexión RabbitMQ caída en runtime** → publisher best-effort loguea y sigue; sin reconexión automática sofisticada (lazy re-create al siguiente publish si la conexión murió — detalle en plan). Consumidor: BackgroundService con retry suave.
- **Volumen de ubicación** → sin confirms/batching; medir en SP-4 antes de optimizar.
- **Interface del seam crece a 17** → los 5 impls + fakes de test se actualizan en el mismo commit (lección B13: búsqueda repo-wide de implementaciones).

## 10. Cierre del slice

- Traceability: fila SP-3i (spec/plan/commits/alcance).
- Ledger por tarea; review final whole-branch (opus).
- Post-slice: SP-4 arranca con: contrato de transporte definido, exchange/cola declarados, eventos fluyendo, y el consumidor de humo como esqueleto a reemplazar por el consumidor real de proyecciones.
