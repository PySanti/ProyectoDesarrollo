# SP-4b — Puntuaciones: ranking consolidado de partida + rendimiento de equipo

- **Slice:** SP-4b (segundo sub-slice de SP-4, descomposición aprobada en el spec SP-4a). Base: SP-4a completo (`a9b1892`), rama `feature/sp-4b-consolidado` creada desde `feature/sp-4a-puntuaciones` (SP-4a **no** integrada a develop por decisión del responsable).
- **Servicio:** Puntuaciones (`services/puntuaciones`, `umbral_puntuaciones`) + `contracts/http/puntuaciones-api.md`. **Sin cambios** en `contracts/events/operaciones-sesion-events.md`.
- **Decisiones del responsable (brainstorm 2026-07-05):** consolidado disponible **solo** con partida `Terminada` (409 en otro estado); participación = tener ≥1 marcador en la partida (sin eventos nuevos); superficie HTTP = 2 endpoints (consolidado de partida + rendimiento de equipo); materialización = **calcular al leer** (enfoque A de SP-4a; materializar-al-finalizar y híbrido-con-caché descartados).
- **Fuera de alcance:** SignalR de ranking en vivo y `RankingConsolidadoCalculado` como push (**SP-4c**), auditoría/historial y ubicaciones (**SP-4d**), publisher RabbitMQ propio de Puntuaciones y outbox (criterio de activación en ADR-0012), cableado de clientes web/móvil (SP-5), historial de nombres de equipo (Identity, RF-43).

## 1. Objetivo

Sobre las proyecciones de SP-4a (`partidas_proyectadas`, `juegos_proyectados`, `marcadores`), exponer por HTTP el **ranking consolidado** de una partida terminada (RF-45, RB-40, HU-50) y el **rendimiento histórico de un equipo** (RF-44, HU-49): por cada partida por equipos terminada en la que participó, su posición en el consolidado y si la ganó. Además se salda la deuda de concurrencia de SP-4a (`xmin` en `marcadores`).

## 2. Reglas de dominio (RF-45 / RF-44)

### Ganador por juego

Dentro de una partida, cada juego lo gana el competidor con **más puntos** en ese juego; empate → **menor tiempo acumulado** en ese juego; si el empate persiste en ambas claves → **ese juego no otorga victoria** (nadie suma "juego ganado").

### Ranking consolidado

Por competidor de la partida se agregan: `juegosGanados` (conteo de juegos ganados según la regla anterior), `puntosTotales` (suma de `PuntosAcumulados` de todos sus marcadores de la partida) y `tiempoTotalMs` (suma de `TiempoAcumuladoMs`). Orden:

1. `juegosGanados DESC`
2. `puntosTotales DESC`
3. `tiempoTotalMs ASC`

Empate exacto en las **tres** claves → los empatados **comparten `posicion`** y la siguiente salta (1, 2, 2, 4 — mismo patrón que el ranking nativo de SP-4a).

`gano = (posicion == 1)`. Si dos competidores comparten la posición 1, **ambos** ganaron la partida (el SRS no desambigua; se documenta esta interpretación, consistente con las posiciones compartidas).

### Participación (limitación aceptada)

Un competidor "participó" en una partida si tiene **≥1 marcador** en ella. Competidores inscritos que nunca anotaron **no aparecen** ni en el consolidado ni en el rendimiento: no existe evento de inscripción individual en el broker y el ciclo de vida de convocatorias es incompleto para este fin. Herencia best-effort de ADR-0012; documentado en el contrato HTTP.

## 3. Cálculo (Application, puro, sin estado)

`CalculadorRankingConsolidado` — clase estática pura en `Application/Handlers/Queries/` (junto a `RankingCalculator` de SP-4a):

- **Entrada:** los marcadores de la partida (solo eso; no requiere `juegos_proyectados`).
- Agrupa por `Marcador.JuegoId` para resolver el ganador de cada juego (tolerante a juegos no proyectados por pérdida de `JuegoActivado`: cada grupo de marcadores define un juego a efectos del cálculo; un juego sin marcadores simplemente no otorga victoria a nadie).
- Devuelve `IReadOnlyList<EntradaRankingConsolidadoDto>` ya ordenada y con posiciones compartidas.

El **cálculo se hace al leer** en cada GET (enfoque A, aprobado): inmune al desorden best-effort — un evento de scoring que llegue después de `PartidaFinalizada` mejora la próxima lectura, nunca congela un ranking incompleto. RF-44 exige "sin duplicar el cálculo de puntajes": el rendimiento de equipo **reusa este mismo calculador** por partida; no existe una segunda implementación.

## 4. Superficie HTTP (vía gateway `/puntuaciones/*`)

### `GET /puntuaciones/partidas/{partidaId}/ranking-consolidado`

```json
{
  "partidaId": "guid",
  "generadoEn": "datetime (UTC)",
  "entradas": [
    { "posicion": 1, "competidorId": "guid", "tipoCompetidor": "Participante | Equipo",
      "juegosGanados": 2, "puntosTotales": 45, "tiempoTotalMs": 23456 }
  ]
}
```

- Partida desconocida en la proyección → **404** (`PartidaNoEncontradaException`, nueva en `Application/Exceptions/`).
- Partida conocida pero no `Terminada` (`Lobby`/`Iniciada`/`Cancelada`) → **409** (`PartidaNoTerminadaException`, nueva; `ExceptionHandlingMiddleware` añade el mapeo a `Conflict`). El SRS define el consolidado **al finalizar** (RF-45, HU-50); no se sirve consolidado provisional.
- Partida `Terminada` sin marcadores → `200` con `entradas: []`.

### `GET /puntuaciones/equipos/{equipoId}/rendimiento`

```json
{
  "equipoId": "guid",
  "partidas": [
    { "partidaId": "guid", "fechaFin": "datetime", "posicion": 1, "gano": true }
  ]
}
```

- Partidas con `Modalidad = Equipo` y `Estado = Terminada` donde el equipo tiene ≥1 marcador, ordenadas por `fechaFin DESC`.
- Equipo sin participaciones (o `equipoId` desconocido) → `200` con `partidas: []` (RF-44 describe un historial, no un recurso: la lista vacía es la respuesta natural y el móvil no distingue "equipo nuevo" de "equipo inexistente").
- `posicion`/`gano` salen del mismo `CalculadorRankingConsolidado` aplicado a los marcadores de cada partida.

### Reglas

- Queries MediatR: `ObtenerRankingConsolidadoQuery(PartidaId)` y `ObtenerRendimientoEquipoQuery(EquipoId)`; handlers en `Application/Handlers/Queries/`; DTOs en `Application/DTOs/` (`RankingConsolidadoResponse`, `EntradaRankingConsolidadoDto`, `RendimientoEquipoResponse`, `RendimientoPartidaDto`).
- Consolidado en `RankingsController` (ruta existente `partidas/{partidaId}`); rendimiento en **`EquiposController` nuevo**. Ambos heredan `ControllerBase` del framework, despachan por `ISender`, sin lógica de negocio, **con controller unit tests** (obligatorios).
- Autorización: misma postura de SP-4a — autenticación en el gateway, lectura para cualquier rol autenticado, sin `[Authorize]` en el servicio (hardening → SP-4c).

## 5. Persistencia (sin tabla nueva, sin cambio de esquema)

Métodos nuevos en `IProyeccionesRepository` (Domain) + implementación EF (`Infrastructure/Persistence/`):

- `GetMarcadoresDePartidaAsync(partidaId)` — todos los marcadores de la partida.
- `GetPartidasTerminadasConMarcadorDeEquipoAsync(equipoId)` — `PartidaProyectada`s con `Modalidad = Equipo`, `Estado = Terminada` y ≥1 marcador del equipo (`CompetidorId = equipoId`, `TipoCompetidor = Equipo`), ordenadas `FechaFin DESC` (join en una sola query).

### Deuda SP-4a saldada: concurrencia en `marcadores`

- `Marcador` configura **`xmin` como token de concurrencia** (`UseXminAsConcurrencyToken()`, columna de sistema de PostgreSQL — sin cambio de esquema; la migración generada solo actualiza el model snapshot).
- En el pipeline por mensaje del `OperacionesSesionEventsConsumer`: `DbUpdateConcurrencyException` → **un reintento** con scope nuevo (el dedup por `eventId` es transaccional con el upsert, así que el primer intento fallido no dejó rastro); si el reintento también falla → `LogWarning` + ack (best-effort ADR-0012, mismo criterio que el resto de ramas warn+ack del worker).
- Se retira la anotación de deuda correspondiente en `services/puntuaciones/service-context.md`.

## 6. Estructura graduada

Sin cambios de doctrina: las piezas nuevas caen en las carpetas mandadas por CLAUDE.md ya existentes (`Queries/`, `DTOs/`, `Handlers/Queries/`, `Exceptions/`, `Api/Controllers/`, `Infrastructure/Persistence/`). Sin comandos nuevos (el slice no proyecta nada nuevo).

## 7. Testing (TDD por tarea)

- **Unit — calculador:** ganador por juego (más puntos; desempate por tiempo; empate exacto → juego sin ganador); agregación multi-juego; orden por las 3 claves; posiciones compartidas (1, 2, 2, 4); `gano` con posición 1 compartida; partida sin marcadores → lista vacía; tolerancia a juego no proyectado.
- **Unit — handlers:** consolidado 404 (partida desconocida) y 409 por cada estado no terminal (`Lobby`/`Iniciada`/`Cancelada`); rendimiento filtra por modalidad/estado/equipo, ordena por `fechaFin DESC`, lista vacía; ambos delegan en el calculador.
- **Unit — controllers:** `RankingsController` (acción nueva) y `EquiposController` con mock de `ISender` (patrón SP-4a).
- **Integration:** end-to-end contra la DB (patrón de la suite existente): proyectar eventos → leer consolidado y rendimiento; **scoring tardío**: `PartidaFinalizada` primero, `EtapaBDTGanada` después → la relectura del consolidado lo refleja; conflicto de concurrencia simulado no aplica en integración single-writer (la rama de reintento queda cubierta por el criterio warn+ack del worker, deuda de unit tests del worker se mantiene anotada).
- **Contract:** shape de los 2 endpoints nuevos.
- **Regresión:** suite SP-4a completa (66/66) sigue verde; Operaciones de Sesión no se toca.

## 8. Contratos y documentación

- `contracts/http/puntuaciones-api.md`: +2 endpoints (método, path por gateway, shapes, códigos, limitación de participación por marcador); Status actualizado (SP-4b registrado; SignalR → SP-4c, auditoría → SP-4d).
- `services/puntuaciones/service-context.md`: estado SP-4b; deuda `xmin` retirada; deudas restantes intactas (retención `eventos_procesados` → SP-4d; `[Authorize]`/hardening → SP-4c; unit tests de ramas del worker).
- `docs/04-sdd/traceability-matrix.md`: fila SP-4b (fuentes: RF-44, RF-45, RB-40/41, HU-49, HU-50).
- **Sin cambios** en `contracts/events/operaciones-sesion-events.md`: la cola y los 7 bindings de SP-4a quedan intactos.

## 9. Riesgos y mitigaciones

- **Competidores con 0 puntos invisibles** → decisión aceptada (participación = marcador); documentada en contrato y traceability; si el dominio lo exige más adelante, requerirá eventos de inscripción nuevos (slice futuro, fuera de SP-4).
- **Scoring tardío tras `PartidaFinalizada`** → cálculo al leer: la siguiente lectura lo incorpora; test de integración lo fija.
- **Empate total en posición 1** → ambos `gano = true`; interpretación documentada (sección 2).
- **Costo de recomputar el consolidado por lectura** (y por partida en el rendimiento) → trivial a escala académica; si SP-4c lo requiriera con frecuencia de push, ahí se mediría (sin caché en 4b, YAGNI).
- **Concurrencia de upserts en `marcadores`** → `xmin` + reintento único + warn+ack; la asunción single-instance deja de ser necesaria para la corrección.

## 10. Cierre del slice

- Ledger por tarea; review final whole-branch del rango de commits SP-4b.
- Traceability + contratos actualizados (sección 8).
- Post-slice: SP-4c (SignalR de ranking en vivo) dispone del consolidado calculable y de las queries nativas; SP-4d cubre auditoría/historial.
