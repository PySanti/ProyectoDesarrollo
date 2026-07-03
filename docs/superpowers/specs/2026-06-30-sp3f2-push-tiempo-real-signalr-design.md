# SP-3f-2 — Push en tiempo real (SignalR) en Operaciones de Sesión

- **Fecha:** 2026-06-30
- **Slice:** SP-3f-2 (segunda parte de SP-3f, tras SP-3f-1 concurrencia + barridos)
- **Servicio dueño:** Operaciones de Sesión
- **Cliente objetivo:** backend (el consumo web/móvil del hub es un follow-up aparte)
- **Estado:** Diseño aprobado — pendiente de plan de implementación
- **Rama:** `feature/code-migration-SP-3`

## Contexto y fuentes

CLAUDE.md / AGENTS.md establecen que las actualizaciones en vivo orientadas al
usuario (lobby, estados, timers, ranking, etapas, pistas, geolocalización,
resultados) viajan por **SignalR/WebSockets a través del gateway**, y que el
gateway hace passthrough de WebSockets. Hoy la experiencia en vivo se obtiene por
**polling** (`GET /operaciones-sesion/mi-sesion`, SP-3e) porque el puerto de
eventos de dominio está conectado a `NoOpSesionEventsPublisher` (el broker
RabbitMQ real está diferido a su propio slice antes de SP-4).

Este slice añade el canal de **push** desde Operaciones de Sesión, sin depender
del broker, colgándose del puerto de eventos que los handlers ya invocan.

Hallazgos de contexto que fijan el diseño:

- `ISesionEventsPublisher` (Application/Interfaces) ya enumera los 13 momentos
  runtime; los handlers ya lo invocan. Hoy → `NoOpSesionEventsPublisher`.
- El gateway YARP hace passthrough de WebSockets automáticamente
  (`Program.cs`); la ruta `/operaciones-sesion/{**catch-all}` ya existe con
  policy `Default` (usuario autenticado) → un hub bajo ese prefijo queda
  proxeado y con JWT validado por el gateway.
- `MiSesionDto` y los DTOs participant-safe (`PreguntaActualDto`,
  `EtapaActualDto`, `OpcionPublicaDto`) ya existen y son la base de los payloads.
- **Convención de ruteo:** los controllers de Operaciones usan `[Route("")]`
  (hostean service-local: `partidas/...`, `mi-sesion`), a diferencia de Partidas
  (`[Route("partidas")]`). El hub se monta con la **misma convención
  service-local** que ya usan los controllers de Operaciones.

## Alcance

**En alcance (backend, Operaciones de Sesión):**

- Hub SignalR (`SesionHub`) con suscripción por partida.
- Broadcaster que implementa el puerto `ISesionEventsPublisher` existente y
  emite a SignalR.
- Composite que hace fan-out a `[NoOp, SignalR]` detrás del puerto.
- Autorización del hub y handshake WebSocket (JWT por query `access_token`) en
  el servicio **y** en el gateway.
- Contrato (sección "Realtime / SignalR" en el contrato HTTP) + tests.

**Fuera de alcance (diferido):**

- **Ranking en vivo** → Puntuaciones (SP-4); Operaciones no calcula ni proyecta
  ranking (rompería fronteras).
- **Cableado de clientes** (React web operador / React Native móvil
  participante) → follow-up.
- **Broker RabbitMQ real** → slice propio antes de SP-4 (cuando aterrice, se
  suma como otra implementación del puerto dentro del composite).
- **Targeting por participante** (mensajes a una conexión/usuario concretos);
  lo personal sigue por pull `/mi-sesion`.
- **Pistas y geolocalización** BDT → sub-slices posteriores de SP-3f.
- **Modalidad Equipo** → slice-E.

## Arquitectura

Patrón: **componer sobre el puerto de eventos existente**. Cero cambios en los
handlers (ya llaman a `ISesionEventsPublisher`).

| Pieza | Capa | Responsabilidad |
|---|---|---|
| `SesionHub` | Api | Punto de conexión SignalR; método `SuscribirAPartida`; gestión del grupo `partida:{id}`. |
| `SignalRSesionEventsPublisher` | Api | Implementa `ISesionEventsPublisher` (13 métodos) vía `IHubContext<SesionHub>`; mapea cada evento a un broadcast del grupo. |
| `CompositeSesionEventsPublisher` | Infrastructure | Implementa `ISesionEventsPublisher`; hace fan-out a una colección de publicadores (`[NoOp, SignalR]`). |
| `NoOpSesionEventsPublisher` | Infrastructure | Sin cambios; se mantiene como seam del broker futuro y para tests. |

**Dirección de dependencia:** Hub y `SignalRSesionEventsPublisher` viven en Api
(SignalR es un concern de ASP.NET Core; Api ya depende de Application e
Infrastructure). El composite vive en Infrastructure y depende solo del puerto
(sin referencia a SignalR). La registración del composite ocurre en la
composición DI del Api, que puede ver ambas implementaciones concretas.

**DI:** registrar `NoOpSesionEventsPublisher` y `SignalRSesionEventsPublisher`
como concretos; registrar `ISesionEventsPublisher` → `CompositeSesionEventsPublisher`
construido con ambos. Los handlers inyectan `ISesionEventsPublisher` y obtienen
el composite. Lifetimes consistentes con el registro actual (`AddScoped`);
`IHubContext<SesionHub>` es singleton e inyectable en scoped.

## Contrato del hub

- **Ruta (service-local):** `MapHub<SesionHub>("hubs/sesion")` → ruta
  gateway-facing `/operaciones-sesion/hubs/sesion` (consistente con el resto de
  filas de Operaciones en el contrato).
- **Cliente → servidor:**
  - `SuscribirAPartida(Guid partidaId)` — autoriza al llamante (rol `Operador`
    **o** participante con inscripción en esa partida) y añade la conexión al
    grupo `partida:{id}`. Rechaza si no cumple.
  - `DesuscribirDePartida(Guid partidaId)` — opcional; quita del grupo.
- **Servidor → cliente** (nombres de método PascalCase que el cliente escucha):

  | Mensaje | Payload (campos clave) |
  |---|---|
  | `PartidaEnLobby` | `partidaId` |
  | `PartidaIniciada` | `partidaId` |
  | `JuegoActivado` | `partidaId`, `juegoId`, `orden`, `tipoJuego` |
  | `PartidaCancelada` | `partidaId`, `motivo?` |
  | `PartidaFinalizada` | `partidaId` |
  | `PreguntaActivada` | `partidaId`, `juegoId`, `preguntaId`, `orden`, `enunciado`, `opciones` (participant-safe, sin flag de correcta), `fechaLimiteUtc` |
  | `PreguntaCerrada` | `partidaId`, `juegoId`, `preguntaId` |
  | `EtapaActivada` | `partidaId`, `juegoId`, `etapaId`, `orden`, `descripcion`, `fechaLimiteUtc` |
  | `EtapaCerrada` | `partidaId`, `juegoId`, `etapaId` |
  | `EtapaGanada` | `partidaId`, `juegoId`, `etapaId` |

- **Reglas de payload:** reutilizan DTOs participant-safe existentes donde
  aplique; llevan identidad + estado + deadline. **Nunca** puntos acumulados ni
  ranking (eso es SP-4). `EtapaGanada` no incluye `Puntaje` (dato de
  scoring/ranking → SP-4).
- **Ciclo de vida del cliente:** el cliente hace `GET /mi-sesion` (snapshot
  inicial) → conecta al hub → `SuscribirAPartida` → recibe deltas. No hay
  snapshot-on-connect (evita carreras; el pull ya cubre el estado inicial).
- **Granularidad:** un único grupo `partida:{id}`. Todos los eventos del slice
  son partida-wide. El estado personal (p. ej. `yaRespondioPreguntaActual`)
  sigue por pull `/mi-sesion`.

## Mapeo evento → mensaje

`SignalRSesionEventsPublisher` implementa los 13 métodos del puerto:

- **Difunde (10):** `PartidaPublicadaEnLobby`, `PartidaIniciada`,
  `JuegoActivado`, `PartidaCancelada`, `PartidaFinalizada`,
  `PreguntaTriviaActivada`, `PreguntaTriviaCerrada`, `EtapaBDTActivada`,
  `EtapaBDTCerrada`, `EtapaBDTGanada`.
- **No-op deliberado (3), documentado en código:** `RespuestaTriviaValidada`,
  `PuntajeTriviaIncrementado`, `TesoroQRValidado` — son per-participante o
  scoring-adjacentes; su efecto consolidado emerge por las transiciones de
  estado (`...Cerrada` / `EtapaGanada`), y los puntos/ranking pertenecen a SP-4.

## Timers

Estrategia **deadline-timestamp**: `PreguntaActivada` / `EtapaActivada` llevan
`fechaLimiteUtc` (hora de servidor); el cliente cuenta localmente. Un mensaje
por activación, sin tormenta de ticks. El cierre real lo disparan los barridos de
SP-3f-1 (o el avance del operador) y se notifica con `PreguntaCerrada` /
`EtapaCerrada`.

**Sub-tarea de soporte:** si `PreguntaTriviaActivadaEvent` /
`EtapaBDTActivadaEvent` aún no cargan el deadline, extenderlos de forma aditiva
(añadir `FechaLimiteUtc`) para que el broadcaster lo pueda emitir sin consultar
estado adicional.

## Autorización y WebSocket

- `SesionHub` lleva `[Authorize]`.
- El handshake WebSocket transporta el JWT por query `access_token` (convención
  SignalR para WS, no por header `Authorization`). Hay que cablear
  `JwtBearerEvents.OnMessageReceived` para leer `access_token` de la query en el
  path del hub, **en el servicio Operaciones** y **en el gateway** (ruta del
  hub). El passthrough de WS de YARP ya es automático.
- `SuscribirAPartida` valida pertenencia: `Operador` por rol, o participante con
  inscripción en la partida (reutiliza la consulta de participación existente).

## Estrategia de pruebas

- **Unit:**
  - `SignalRSesionEventsPublisher`: con un fake de `IHubContext` (sin Moq, fakes
    a mano) verificar grupo destino (`partida:{id}`), nombre de método y forma
    del payload por cada uno de los 10 eventos difundidos; y que los 3 no-op no
    emiten.
  - `CompositeSesionEventsPublisher`: fan-out a todos los publicadores internos
    (cada uno recibe la llamada; un fallo de uno no anula el otro — o se define
    el comportamiento de error explícitamente).
  - `SesionHub.SuscribirAPartida`: con fakes de `Groups`/`Context`, verifica add
    al grupo y rechazo cuando el llamante no es `Operador` ni está inscrito.
  - Helper de nombre de grupo.
- **Contract:** sección "Realtime / SignalR" en
  `contracts/http/operaciones-sesion-api.md` (URL del hub, mensajes,
  payloads, auth WS) + un test que asevere que las constantes de nombres de
  mensaje del código coinciden con lo documentado.
- **Gap documentado:** el camino WebSocket-a-través-de-YARP no queda cubierto por
  integration tests (igual que los gaps de InMemory en SP-3f-1); se verifica por
  inspección y por los unit/contract tests.

## Deuda pre-existente (fuera de alcance)

Los controllers de Operaciones usan `[Route("")]` (sin prefijo
`operaciones-sesion`), mientras YARP reenvía el path completo sin transform. Por
el gateway, `/operaciones-sesion/...` no resuelve contra rutas service-local sin
prefijo — un desajuste que afecta a **todos** los endpoints de Operaciones por
igual (hoy solo cubierto por tests directos-al-servicio), no solo al hub. Se
documenta, no se corrige en este slice. El hub sigue la convención service-local
existente para no romper ese estado.

## Riesgos y decisiones abiertas

- **Extensión de eventos para `fechaLimiteUtc`:** aditiva; revisar que no rompa
  a consumidores existentes (hoy solo el No-Op, que ignora el contenido).
- **Comportamiento de error del composite:** si una implementación interna
  lanza, definir si se continúa con las demás (recomendado: aislar por
  publicador para que SignalR no tumbe el path del handler, y viceversa cuando
  llegue el broker).
- **Lifetimes:** confirmar que `SignalRSesionEventsPublisher` (scoped) tomando
  `IHubContext` (singleton) no genera captive dependency.

## Esquema de tareas (se detalla en writing-plans)

1. Constantes de mensajes + `SesionHub` (grupo, `SuscribirAPartida` con
   autorización) + tests.
2. `SignalRSesionEventsPublisher` (10 difunden / 3 no-op) + tests.
3. `CompositeSesionEventsPublisher` + tests.
4. Extender eventos de activación con `FechaLimiteUtc` (si falta) + ajustes.
5. DI: `AddSignalR`, `MapHub`, registro del composite, JWT `access_token` por
   query en el servicio.
6. Gateway: JWT `access_token` por query en la ruta del hub (segundo código
   base, mínimo).
7. Contrato (sección Realtime/SignalR) + contract test.
8. Fila de traceability SP-3f-2 (carve-out: se escribe, no se commitea).
