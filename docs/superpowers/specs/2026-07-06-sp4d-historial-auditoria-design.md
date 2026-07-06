# SP-4d — Puntuaciones: proyección de auditoría/historial + historial del participante

- **Slice:** SP-4d (cuarto y último sub-slice de SP-4, descomposición aprobada en el spec SP-4a). Base: SP-4c completo (`7b8b0ee`), rama `feature/sp-4d-historial` creada desde `feature/sp-4c-signalr-ranking` (la serie SP-4 sigue sin integrarse a develop por decisión del responsable).
- **Servicio:** Puntuaciones (`services/puntuaciones`, `umbral_puntuaciones`) + `contracts/http/puntuaciones-api.md` + `contracts/events/operaciones-sesion-events.md` (nota de la segunda cola). **Sin cambios** en gateway ni en los eventos/payloads publicados por Operaciones de Sesión.
- **Decisiones del responsable (brainstorm 2026-07-06):** alcance de eventos = **los 17** del contrato, con `UbicacionActualizada` **muestreada** (máx. 1 por participante por minuto; completas y sin-ubicaciones descartados); almacenamiento = **tabla genérica** `eventos_historial` + un GET paginado (filas tipadas y endpoints-por-tipo descartados); transporte = **segunda cola dedicada** con binding `#` (ampliar la cola de proyecciones descartado); deudas saldadas en este slice = retención/índice de `eventos_procesados` **y** `ArgumentException`→400 con log (unit tests de ramas del worker SP-4a queda anotada); autorización del historial de partida = **solo `Operador`/`Administrador`**; **HU-27 incluida** (historial de partidas jugadas del participante con puntuación y posición).
- **Fuera de alcance:** eventos de Identity/Partidas (Identity no publica al broker → invitaciones de equipo fuera del historial, limitación documentada), historial de nombres de equipo (RF-43, Identity), publisher RabbitMQ propio de Puntuaciones y outbox (ADR-0012), cableado de clientes web/móvil (SP-5), purga del historial mismo (RB-31 lo exige visible), unit tests de las ramas warn+ack del worker de la era SP-4a (deuda anotada).

## 1. Objetivo

Materializar en Puntuaciones el historial/auditoría de partida (`EventoHistorial`, HU-43, RF-12, RB-15) consumiendo **todos** los eventos de Operaciones de Sesión por una cola dedicada, y exponer dos lecturas: el **historial de una partida** para el operador (HU-43) y el **historial único de partidas jugadas** del participante con su puntuación y posición (HU-27, RF-24). Además se saldan dos deudas: retención/índice de `eventos_procesados` (SP-4a) y `ArgumentException`→400 sin log (middleware). Con esto la serie SP-4 queda completa.

## 2. Proyección `eventos_historial` (tabla nueva — este slice SÍ migra esquema)

Entidad `EventoHistorial` en `Domain/Entities/`:

| Columna | Tipo | Notas |
|---|---|---|
| `Id` | `long` identity | PK |
| `EventId` | `Guid` | **índice único** — la propia fila es el registro de dedup del historial (sin segunda tabla de dedup: reusar `eventos_procesados` chocaría con el consumidor de proyecciones, que ya registra los mismos `EventId`) |
| `PartidaId` | `Guid` | índice compuesto `(PartidaId, OccurredAt)` |
| `JuegoId` | `Guid?` | cuando el evento lo trae |
| `TipoEvento` | `string` (máx. 64) | nombre del evento del contrato (p. ej. `EtapaBDTGanada`) |
| `OccurredAt` | `DateTime` (UTC) | orden natural del historial |
| `ParticipanteId` | `Guid?` | **autor real** del evento — salda la nota de SP-4a "el autor no se persiste en 4a (auditoría → SP-4d)" |
| `EquipoId` | `Guid?` | equipo acreditado/destino cuando aplica |
| `Detalle` | `jsonb` | resto del payload resumido (orden, motivo, correcta, puntaje, texto de pista, lat/lon, etc.) |

- **Muestreo de ubicaciones (al escribir):** una `UbicacionActualizada` se descarta si ya existe una fila `UbicacionActualizada` del mismo `(PartidaId, ParticipanteId)` con `OccurredAt` a menos de **60 segundos** de la nueva. Carrera entre consumidores concurrentes que cuele una muestra extra: aceptable (best-effort, el muestreo es una cota de volumen, no un invariante).
- **El historial no depende de `partidas_proyectadas` para escribir**: eventos llegados antes de que la partida se proyecte igual se registran (sin garantía de orden entre las dos colas). La partida proyectada solo se exige en el GET del operador (404).
- Migración EF `SP4dHistorial`: tabla + índices + el índice de `ProcesadoAt` en `eventos_procesados` (sección 5).

## 3. Consumidor dedicado (segunda cola)

- Cola **`puntuaciones.operaciones-sesion.historial`**, durable, binding **`operaciones-sesion.#`** al exchange existente `umbral.operaciones-sesion`. La cola de proyecciones (`puntuaciones.operaciones-sesion.proyecciones`, 7 bindings) y su pipeline SP-4c **no se tocan** — cero riesgo de regresión y cada cola avanza a su ritmo (el volumen de ubicaciones no frena los pushes de ranking).
- `HistorialEventsConsumer` (segundo `BackgroundService` en `Api/Workers/`, mismo esqueleto que el consumidor de proyecciones: reconexión con reintento a 30 s, `DispatchConsumersAsync`, ack-siempre sin poison-loop, reusa `EnvelopeReader`). Opciones en una sección de configuración propia (`RabbitMqHistorialOptions`: mismas credenciales, su cola y binding).
- `HistorialEventMapper` (en `Api/Workers/`): mapea los **17** tipos del contrato a un único comando genérico `ProyectarEventoHistorialCommand(EventId, TipoEvento, OccurredAt, PartidaId, JuegoId?, ParticipanteId?, EquipoId?, DetalleJson)` extrayendo por tipo los ids y el resumen de payload; tipo desconocido → `null` (warn + ack, paridad con el mapper de proyecciones).
- `ProyectarEventoHistorialCommandHandler` (Application): consulta existencia por `EventId` (duplicado → no-op), aplica el muestreo de ubicaciones e inserta la fila. La carrera residual del check-then-insert la cubre el índice único: una `DbUpdateException` por violación de unicidad se trata como "ya registrado" (éxito, sin reintento — el duplicado ES el resultado correcto).
- Best-effort ADR-0012: fallo persistente → `LogError` + ack; el historial es reconstruible reprocesando eventos.

## 4. Superficie HTTP (vía gateway `/puntuaciones/*`)

### `GET /puntuaciones/partidas/{partidaId}/historial` — HU-43 (operador, web)

Query params: `limit` (default **100**, máx. **500**), `offset` (default 0), `tipo` (opcional, filtra por `TipoEvento` exacto).

```json
{
  "partidaId": "guid",
  "total": 245,
  "entradas": [
    { "occurredAt": "datetime", "tipoEvento": "EtapaBDTGanada", "juegoId": "guid|null",
      "participanteId": "guid|null", "equipoId": "guid|null", "detalle": { } }
  ]
}
```

- Orden fijo `occurredAt ASC` (relato cronológico de la partida). `total` = conteo con el filtro aplicado, para paginar.
- Partida desconocida en `partidas_proyectadas` → **404** (`PartidaNoEncontradaException`, reuso de SP-4b). Partida conocida sin eventos → 200 con `entradas: []`.
- **Autorización: `[Authorize(Roles = "Operador,Administrador")]`** — primer check por rol dentro de Puntuaciones (el historial expone respuestas, pistas y ubicaciones de todos). Participante autenticado → **403**. `RoleClaimType` ya es `"roles"` en la config JWT del servicio.

### `GET /puntuaciones/participantes/{participanteId}/historial-partidas` — HU-27 (participante, móvil)

```json
{
  "participanteId": "guid",
  "partidas": [
    { "partidaId": "guid", "modalidad": "Individual | Equipo", "fechaFin": "datetime",
      "equipoId": "guid|null", "puntosTotales": 45, "posicion": 1, "gano": true,
      "juegos": [ { "juegoId": "guid", "orden": 1, "tipoJuego": "Trivia", "puntos": 20 } ] }
  ]
}
```

- **Participación:** partidas `Terminadas` donde (a) el participante tiene marcador propio (`TipoCompetidor = Participante`, modalidad Individual), **o** (b) su `equipoId` en esa partida se resuelve del historial — cualquier `EventoHistorial` de la partida con su `ParticipanteId` y `EquipoId` no nulos **excluyendo `ConvocatoriaCreada`** (los eventos de acción de juego que autoró: respuestas, puntajes, tesoros, etapas; `ConvocatoriaRespondida` no sirve porque su payload no trae `equipoId`, y `ConvocatoriaCreada` se excluye para no listar convocados que rechazaron) — **y ese equipo tiene ≥1 marcador** en la partida (sin marcador no hay posición calculable; misma regla "participación = marcador" de SP-4b). En (b) la puntuación/posición mostrada es **la del equipo**.
- `posicion`/`gano`/`puntosTotales` salen del mismo `CalculadorRankingConsolidado` de SP-4b aplicado a los marcadores de cada partida (RF-24 con la regla RF-44 de "sin duplicar el cálculo"); `juegos` sale de `juegos_proyectados` + el marcador del competidor por juego (`puntos` 0 si no anotó en ese juego).
- Orden `fechaFin DESC`. Participante sin partidas (o id desconocido) → 200 con `partidas: []` (paridad con rendimiento de equipo SP-4b).
- **Limitación documentada (herencia best-effort):** un integrante de equipo que jamás autoró una acción de juego en la partida no la verá listada; las partidas `Canceladas` no aparecen (RB-30: sus resultados parciales no cuentan como resultado final — el relato completo vive en el historial del operador).
- Autorización: cualquier rol autenticado (paridad con marcador propio y rendimiento de equipo; sin permiso funcional específico).

## 5. Deudas saldadas

- **Retención de `eventos_procesados` (SP-4a):** índice en `ProcesadoAt` (misma migración) + `PurgaEventosProcesadosService` (`BackgroundService` con timer **cada 24 h**, primera pasada ~1 min tras arrancar) que borra filas con `ProcesadoAt` anterior a la retención configurable (`Retencion:EventosProcesadosDias`, **default 30**). El dedup solo necesita cubrir la ventana de redelivery del broker; 30 días sobra. **No** purga `eventos_historial` (RB-31) ni afecta al dedup por `EventId` del historial (índice único propio).
- **Middleware (`ArgumentException`→400 sin log, SP-4a):** `ExceptionHandlingMiddleware` añade `LogWarning` (mensaje + path) antes de responder 400. Sin cambio de contrato.

## 6. Estructura graduada

Sin carpetas nuevas fuera de doctrina: entidad e interfaz de repositorio en `Domain/`, comando/handler/queries/DTOs/excepciones en las carpetas mandadas de `Application/`, EF en `Infrastructure/Persistence/`, consumidor/mapper/purga en `Api/Workers/`. Dos controllers nuevos por recurso: `HistorialController` (GET de historial de partida, con el `[Authorize]` por roles) y `ParticipantesController` (GET de historial-partidas) — ambos `ControllerBase`, despachan por `ISender`, sin lógica de negocio, **con unit tests obligatorios**.

## 7. Testing (TDD por tarea)

- **Unit — mapper:** los 17 tipos producen el comando correcto (ids y detalle por tipo); tipo desconocido → null.
- **Unit — handler historial:** inserta con todos los campos; `EventId` duplicado no inserta segunda fila; muestreo (misma partida+participante a <60 s se descarta, a ≥60 s o distinto participante/partida se guarda; otros tipos no se muestrean).
- **Unit — queries:** historial de partida (orden ASC, paginación, filtro `tipo`, 404, `total`); historial-partidas (individual por marcador, equipo por historial, posición/gano del calculador reusado, juegos con puntos, orden `fechaFin DESC`, lista vacía, cancelada excluida).
- **Unit — controllers:** atributos de autorización ([Authorize] con roles en historial de partida) + despacho por `ISender`.
- **Unit — middleware:** `ArgumentException` → 400 **y** `LogWarning` emitido.
- **Unit — purga:** borra lo anterior a la retención, conserva lo reciente.
- **Integration:** consumir eventos (por comandos, patrón de la suite) → GET historial con orden/paginación/filtro/muestreo E2E; historial-partidas E2E individual y equipo; purga con fechas viejas; regresión completa SP-4a/4b/4c.
- **Contract:** shapes de los 2 endpoints; **401** sin token y **403** con rol Participante en el historial de partida (el `TestAuthHandler` de ambos proyectos de test se extiende con header `X-Test-Roles` → claims de rol).

## 8. Contratos y documentación

- `contracts/http/puntuaciones-api.md`: +2 endpoints (shapes, códigos, autorización por rol del historial, limitaciones de participación e invitaciones); Status → serie SP-4 completa.
- `contracts/events/operaciones-sesion-events.md`: en Transport, nota de la **segunda cola** `puntuaciones.operaciones-sesion.historial` (binding `#`, consumidor de historial, misma doctrina best-effort). Payloads intactos.
- `services/puntuaciones/service-context.md`: estado SP-4d; deudas **retiradas**: retención/índice de `eventos_procesados` y `ArgumentException`→400 sin log; queda anotada solo la de unit tests de ramas del worker SP-4a; pending de la serie: **vacío** (SP-4 completa; clientes → SP-5).
- `docs/04-sdd/traceability-matrix.md`: fila SP-4d (fuentes: RF-12, RF-24, RF-35, RF-37, RB-15, RB-30, RB-31, HU-27, HU-43).

## 9. Riesgos y mitigaciones

- **Volumen de ubicaciones** → muestreo 1/min por participante al escribir + binding en cola separada (no compite con proyecciones/pushes).
- **Doble dedup entre colas** → el historial dedup-ea por índice único de `EventId` en su propia tabla; jamás toca `eventos_procesados` (que pertenece al consumidor de proyecciones).
- **Miembro de equipo invisible en HU-27** → limitación documentada (sección 4); si el dominio lo exige, requerirá eventos de inscripción/membresía nuevos (slice futuro).
- **Invitaciones de equipo ausentes del historial (HU-43)** → Identity no publica eventos; limitación documentada en contrato y traceability.
- **Crecimiento de `eventos_historial`** → aceptado (es el registro exigido por RB-31); el muestreo acota el único flujo de alto volumen; si un día hiciera falta archivado, sería un slice de operación, no de dominio.
- **Purga borrando dedup aún útil** → retención 30 días ≫ ventana real de redelivery; configurable por entorno.

## 10. Cierre del slice

- Ledger por tarea en `.superpowers/sdd/progress.md` (archivar el de SP-4c); review final whole-branch del rango SP-4d.
- Traceability + contratos actualizados (sección 8).
- Post-slice: **serie SP-4 completa**. Siguiente: SP-5 (cableado de clientes web/móvil a HTTP + SignalR de Puntuaciones).
