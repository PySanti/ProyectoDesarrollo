# SP-4a — Puntuaciones: consumidor real, proyecciones de scoring y rankings nativos

- **Slice:** SP-4a (primer sub-slice de SP-4). Base: SP-3i completo (`0a8ff82`), rama `develop`.
- **Servicio:** Puntuaciones (`services/puntuaciones`, `umbral_puntuaciones`) + `contracts/http/puntuaciones-api.md` + `contracts/events/operaciones-sesion-events.md` (nota de cola).
- **Decisiones del responsable (brainstorm 2026-07-04):** SP-4 se parte en sub-slices 4a→4d; 4a consume scoring + ciclo de vida (7 eventos); superficie HTTP = ranking por juego + marcador propio; materialización = agregados con ranking calculado al leer (enfoque A; B ranking-materializado y C event-store descartados).
- **Fuera de alcance:** ranking consolidado y team-performance (**SP-4b**), SignalR de ranking en vivo (**SP-4c**), auditoría/historial y ubicaciones (**SP-4d**), publisher RabbitMQ propio de Puntuaciones, outbox transaccional (criterio de activación en ADR-0012), cableado de clientes web/móvil (SP-5).

## 0. Descomposición de SP-4 (aprobada)

| Sub-slice | Contenido | Depende de |
|---|---|---|
| **SP-4a** (este spec) | Consumidor RabbitMQ real + proyecciones de scoring + rankings nativos Trivia/BDT + queries HTTP | SP-3i |
| SP-4b | Ranking consolidado de partida (juegos ganados → puntos → tiempo) + queries de rendimiento de equipo | SP-4a |
| SP-4c | SignalR de ranking en vivo (hub Puntuaciones tras el gateway; `RankingTriviaActualizado`, `RankingBDTActualizado`, `RankingConsolidadoCalculado`) | SP-4a (4b para consolidado) |
| SP-4d | Proyección de auditoría/historial (`EventoHistorial`/`RegistroAuditoria`, incl. `UbicacionActualizada`) | SP-4a |

Cada sub-slice tiene su propio ciclo spec → plan → implementación, como SP-3.

## 1. Objetivo

Reemplazar el consumidor de humo de SP-3i por el consumidor real de proyecciones: Puntuaciones persiste marcadores por (juego, competidor) a partir de los eventos de Operaciones de Sesión y expone por HTTP el ranking nativo de cada juego (Trivia y BDT, ambos por puntos acumulados con tie-break por menor tiempo) y el marcador propio de un competidor. Primer modelo de lectura real del servicio, reconstruible (ADR-0012).

## 2. Consumo RabbitMQ

### Cola y bindings

- **Cola nueva:** `puntuaciones.operaciones-sesion.proyecciones`, durable, bindings **solo** a las 7 routing keys que 4a procesa:
  `operaciones-sesion.partida-publicada-en-lobby.v1`, `partida-iniciada.v1`, `juego-activado.v1`, `partida-cancelada.v1`, `partida-finalizada.v1`, `puntaje-trivia-incrementado.v1`, `etapa-bdt-ganada.v1`.
- **La cola de humo `puntuaciones.operaciones-sesion.all` se elimina** (`QueueDelete` idempotente al arrancar el consumidor): su binding `operaciones-sesion.#` acumularía ubicaciones (~1 evento/2 s por participante BDT activo) sin consumidor. El contrato de eventos actualiza la nota "Smoke queue (SP-3i)" → cola de proyecciones (SP-4a).
- `RabbitMqConsumerOptions` ajusta sus defaults (`Queue`, y `Binding` pasa a la lista fina); config espejo `RabbitMq__*` intacta; sin `Enabled=true` + host el consumidor no arranca y el servicio levanta igual (comportamiento actual).

### Pipeline por mensaje

- `OperacionesSesionEventsConsumer` (`Api/Workers`) conserva el patrón de conexión/reintento suave (retry 30 s) y manual ack.
- Por mensaje: `EnvelopeReader` parsea el envelope → mapa `eventType` → **comando MediatR de proyección** → despacho con un `IServiceScope` por mensaje (`ISender` scoped). El worker no toca la DB; toda la lógica vive en `Application/Handlers/Commands/`.
- Comandos (7): `ProyectarPartidaPublicadaCommand`, `ProyectarPartidaIniciadaCommand`, `ProyectarJuegoActivadoCommand`, `ProyectarPartidaCanceladaCommand`, `ProyectarPartidaFinalizadaCommand`, `ProyectarPuntajeTriviaCommand`, `ProyectarEtapaBdtGanadaCommand`. Cada uno lleva `EventId`, `OccurredAt` y el payload documentado en `contracts/events/operaciones-sesion-events.md`.

### Idempotencia y fallos

- **Dedup por `eventId`** (exigido por el contrato de transporte): tabla `eventos_procesados`; cada handler inserta el `eventId` en el **mismo `SaveChanges`** que su upsert. Evento ya procesado → retorno sin efecto.
- Envelope malformado o `eventType` desconocido → `LogWarning` + ack (precedente SP-3i; sin requeue infinito).
- Excepción del handler → `LogError` + ack: sin poison-loop; la proyección es best-effort y reconstruible (ADR-0012).
- Broker caído → el servicio levanta igual; reintento suave.

## 3. Modelo de datos (`umbral_puntuaciones`, EF Core + migración)

| Tabla | Clave | Contenido |
|---|---|---|
| `partidas_proyectadas` | `PartidaId` | `SesionPartidaId`, `Modalidad` (`Individual`/`Equipo`), `Estado` (`Lobby`/`Iniciada`/`Cancelada`/`Terminada`, espejo de `EstadoPartida`), `FechaInicio?`, `FechaFin?` |
| `juegos_proyectados` | `JuegoId` | `PartidaId`, `Orden`, `TipoJuego` (`Trivia`/`BusquedaDelTesoro`) |
| `marcadores` | (`JuegoId`, `CompetidorId`) | `PartidaId`, `TipoCompetidor` (`Participante`/`Equipo`), `PuntosAcumulados`, `TiempoAcumuladoMs`, `UnidadesGanadas` |
| `eventos_procesados` | `EventId` | `EventType`, `OccurredAt`, `ProcesadoAt` |

- **Identidad dual (regla slice-E):** `CompetidorId = equipoId ?? participanteId`. En `Equipo` los puntos se acreditan al equipo (una fila por equipo); en `Individual`, al participante. El autor real del evento no se persiste en 4a (auditoría → SP-4d).
- **Sin FKs duras entre proyecciones:** un marcador puede llegar antes que su juego proyectado (best-effort, sin garantía de orden). Las queries hacen join tolerante; los eventos de ciclo de vida crean **stubs** si la fila no existe aún (p. ej. `PartidaFinalizada` sin `PartidaPublicadaEnLobby` previa crea la partida con los datos disponibles).
- **Acumulación conmutativa:** el orden de llegada de los eventos de scoring no altera el resultado final (sumas + dedup).
- **Dominio:** entidades de proyección en `Domain/` con invariantes mínimos (puntos/tiempos nunca negativos; acumulación encapsulada en métodos tipo `AcreditarPuntaje(puntos, tiempoMs)`); interfaces de repositorio en `Domain/`; implementaciones EF en `Infrastructure/Persistence/`.

### Semántica por evento

| Evento | Efecto |
|---|---|
| `PartidaPublicadaEnLobby` | Upsert partida (`Lobby`, `Modalidad`, `SesionPartidaId`) |
| `PartidaIniciada` | Estado → `Iniciada`, `FechaInicio` |
| `PartidaCancelada` | Estado → `Cancelada`, `FechaFin = fechaCancelacion` |
| `PartidaFinalizada` | Estado → `Terminada`, `FechaFin` |
| `JuegoActivado` | Upsert juego (`PartidaId`, `Orden`, `TipoJuego`) |
| `PuntajeTriviaIncrementado` | Upsert marcador: `Puntos += puntaje`, `Tiempo += tiempoRespuestaMs`, `Unidades += 1` (preguntas ganadas) |
| `EtapaBDTGanada` | Upsert marcador: `Puntos += puntaje`, `Tiempo += tiempoResolucionMs`, `Unidades += 1` (etapas ganadas — **solo informativo**, nunca clave de orden) |

## 4. Superficie HTTP (vía gateway `/puntuaciones/*`)

### `GET /puntuaciones/partidas/{partidaId}/juegos/{juegoId}/ranking`

```json
{
  "juegoId": "guid",
  "tipoJuego": "Trivia | BusquedaDelTesoro",
  "generadoEn": "datetime (UTC)",
  "entradas": [
    { "posicion": 1, "competidorId": "guid", "tipoCompetidor": "Participante | Equipo",
      "puntos": 30, "tiempoAcumuladoMs": 12345, "unidadesGanadas": 3 }
  ]
}
```

- **Orden:** `puntos DESC, tiempoAcumuladoMs ASC` — misma regla para Trivia y BDT (doctrina: BDT por puntos de etapas ganadas; `EtapasGanadas`/`unidadesGanadas` es informativo).
- Empate exacto en ambas claves → los empatados **comparten `posicion`** (la siguiente posición salta: 1, 2, 2, 4).
- Juego desconocido en la proyección → `404`. Juego conocido sin marcadores → `200` con `entradas: []`.
- El ranking se **calcula al leer** (ORDER BY sobre `marcadores`); no se materializan posiciones (enfoque A aprobado). SP-4c empujará por SignalR el resultado de esta misma query tras cada evento de scoring.

### `GET /puntuaciones/partidas/{partidaId}/juegos/{juegoId}/marcadores/{competidorId}`

Marcador propio: `{ competidorId, tipoCompetidor, puntos, tiempoAcumuladoMs, unidadesGanadas, posicion }` (posición calculada con la misma regla de orden/empates). Sin marcador para ese competidor → `404`.

### Reglas

- Controllers en `Api/Controllers`, heredan `ControllerBase`, despachan por MediatR (`ObtenerRankingJuegoQuery`, `ObtenerMarcadorQuery`), sin lógica de negocio.
- Autorización: JWT validado (gateway + servicio, defensa en profundidad); **cualquier rol autenticado puede leer** — el ranking es visible para operador y participantes. Sin permiso funcional específico (queries de lectura).
- Middleware de excepciones existente se mantiene; `404` vía excepción de aplicación (`Exceptions/`) mapeada por el middleware.
- La ruta `/puntuaciones/*` ya existe en el gateway (ADR-0009) — solo se verifica.

## 5. Estructura graduada

Sin cambios de doctrina: `Application/` gana `Commands/`, `Queries/`, `DTOs/`, `Handlers/Commands/`, `Handlers/Queries/`, `Interfaces/`, `Validators/`, `Exceptions/` según lo que el slice use (el set completo mandado por CLAUDE.md); repositorios EF en `Infrastructure/Persistence/`; el worker permanece en `Api/Workers` (hosted service, precedente SP-3i).

## 6. Testing (TDD por tarea)

- **Unit:** handlers de proyección (acumulación, dedup por `eventId`, identidad dual equipo/participante, stubs por desorden, invariantes de dominio); handler de ranking (orden, empates compartiendo posición, tie-break por tiempo, `unidadesGanadas` no ordena); mapeo envelope→comando del worker (eventType desconocido → warning + ack); **controller unit tests** (obligatorios, mock `ISender`, patrón de los servicios hermanos).
- **Integration:** proyección end-to-end contra la DB con el patrón de la suite existente del servicio; round-trip RabbitMQ **opt-in** (Skip sin `RABBITMQ_TEST_HOST`, precedente SP-3i).
- **Contract:** shape de los 2 endpoints.
- **Regresión:** las suites existentes de Puntuaciones (Health + EnvelopeReader) siguen verdes; suites de Operaciones no se tocan.

## 7. Contratos y documentación

- `contracts/http/puntuaciones-api.md`: registrar los 2 endpoints (método, path por gateway, shapes, códigos).
- `contracts/events/operaciones-sesion-events.md`: sección Transport — la cola de humo se reemplaza por `puntuaciones.operaciones-sesion.proyecciones` con sus 7 bindings y consumidor real (SP-4a); nota de que el resto de eventos sigue sin consumidor en Puntuaciones hasta 4b/4d.
- `services/puntuaciones/service-context.md`: estado SP-4a.
- `docs/04-sdd/traceability-matrix.md`: fila SP-4a.

## 8. Riesgos y mitigaciones

- **Pérdida de eventos (best-effort)** → ADR-0012 vigente: proyección reconstruible; marcadores pueden quedar incompletos ante pérdida puntual — aceptado; el consolidado (4b) hereda el mismo criterio.
- **Desorden/llegada parcial** → stubs + acumulación conmutativa + joins tolerantes; tests de desorden lo fijan.
- **Duplicados del broker** → dedup por `eventId` transaccional con el upsert.
- **Borrado de la cola de humo con mensajes** → aceptable: eran logs de humo sin proyección; documentado en el contrato.
- **Volumen** → 7 bindings finos excluyen ubicaciones; sin confirms/batching (se mide en 4c/4d si hace falta).

## 9. Cierre del slice

- Ledger por tarea; review final whole-branch del rango de commits SP-4a.
- Traceability + contratos actualizados (sección 7).
- Post-slice: SP-4b arranca con partidas/juegos/marcadores proyectados y queries de ranking nativas operativas.
