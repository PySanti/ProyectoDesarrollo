# Puntuaciones HTTP Contract

## Status

Endpoints registrados (6): ranking nativo por juego + marcador propio (SP-4a), ranking
consolidado de partida + rendimiento de equipo (SP-4b), e historial de partida (operador/HU-43)
+ historial de partidas jugadas del participante (HU-27) (SP-4d), servidos por las proyecciones
alimentadas por RabbitMQ (best-effort, ADR-0012), más el hub SignalR de ranking en vivo (SP-4c).
Serie SP-4 (SP-4a/4b/4c/4d) completa.

## Access Path

Requests enter through the YARP gateway (`/puntuaciones/*` → servicio Puntuaciones, reenvío puro).

## Endpoint Registry

| Capability | Method | Gateway path | Owning service | Status |
|---|---|---|---|---|
| Ranking nativo de un juego | GET | `/puntuaciones/partidas/{partidaId}/juegos/{juegoId}/ranking` | Puntuaciones | Registered (SP-4a) |
| Marcador propio en un juego | GET | `/puntuaciones/partidas/{partidaId}/juegos/{juegoId}/marcadores/{competidorId}` | Puntuaciones | Registered (SP-4a) |
| Ranking consolidado de la partida | GET | `/puntuaciones/partidas/{partidaId}/ranking-consolidado` | Puntuaciones | Registered (SP-4b) |
| Rendimiento histórico de un equipo | GET | `/puntuaciones/equipos/{equipoId}/rendimiento` | Puntuaciones | Registered (SP-4b) |
| Historial de una partida (operador) | GET | `/puntuaciones/partidas/{partidaId}/historial` | Puntuaciones | Registered (SP-4d) |
| Historial de partidas jugadas de un participante | GET | `/puntuaciones/participantes/{participanteId}/historial-partidas` | Puntuaciones | Registered (SP-4d) |

## `GET /puntuaciones/partidas/{partidaId}/juegos/{juegoId}/ranking`

Ranking nativo del juego (Trivia y BDT usan la misma regla: puntos acumulados DESC, tiempo
acumulado ASC; `unidadesGanadas` — preguntas/etapas ganadas — es informativo, nunca clave de
orden). Empate exacto en ambas claves comparte `posicion` (1, 2, 2, 4). Calculado al leer.

- `200`:

```json
{
  "juegoId": "guid",
  "tipoJuego": "Trivia | BusquedaDelTesoro",
  "generadoEn": "datetime (UTC)",
  "entradas": [
    {
      "posicion": 1,
      "competidorId": "guid",
      "tipoCompetidor": "Participante | Equipo",
      "puntos": 30,
      "tiempoAcumuladoMs": 12345,
      "unidadesGanadas": 3
    }
  ]
}
```

- `404` `{ "message": "..." }`: el juego no existe en la proyección o no pertenece a la partida.
- Juego de una partida con competidores → `200` con todos ellos, a `0` mientras nadie haya anotado
  (empatados en posición 1). `entradas: []` solo si la partida no tiene participaciones proyectadas
  ni marcadores.
- `competidorId` sigue la identidad dual slice-E: participante en `Individual`, equipo en `Equipo`.

## `GET /puntuaciones/partidas/{partidaId}/juegos/{juegoId}/marcadores/{competidorId}`

Marcador propio de un competidor con su posición actual (misma regla de orden/empates).

- `200`:

```json
{
  "competidorId": "guid",
  "tipoCompetidor": "Participante | Equipo",
  "puntos": 10,
  "tiempoAcumuladoMs": 1500,
  "unidadesGanadas": 1,
  "posicion": 2
}
```

- `404` `{ "message": "..." }`: juego desconocido, o el competidor no está en la partida (ni
  participación proyectada ni marcador). Quien se inscribió y aún no anotó recibe `200` con
  `puntos: 0` y su posición, no `404`.

## `GET /puntuaciones/partidas/{partidaId}/ranking-consolidado`

Ranking consolidado de una partida **terminada** (RF-45): ordena por juegos ganados DESC,
puntos totales DESC, tiempo total ASC. Ganador de cada juego = más puntos con desempate por
menor tiempo; empate exacto → ese juego no otorga victoria. Empate exacto en las tres claves
comparte `posicion` (1, 2, 2, 4). Calculado al leer.

- `200`:

```json
{
  "partidaId": "guid",
  "generadoEn": "datetime (UTC)",
  "entradas": [
    {
      "posicion": 1,
      "competidorId": "guid",
      "tipoCompetidor": "Participante | Equipo",
      "juegosGanados": 2,
      "puntosTotales": 45,
      "tiempoTotalMs": 23456
    }
  ]
}
```

- `404` `{ "message": "..." }`: la partida no existe en la proyección.
- `409` `{ "message": "..." }`: la partida no está `Terminada` (el consolidado se calcula al finalizar; sin consolidado provisional).
- Partida terminada sin participaciones ni marcadores → `200` con `entradas: []`.
- **Participación = inscripción aceptada**, no haber anotado: los competidores que nunca puntuaron
  aparecen con `0` y en la última posición. Se proyecta desde `InscripcionAceptada`
  (`participaciones_proyectadas`). El universo es `participaciones ∪ marcadores`: si se perdiera
  `InscripcionAceptada` (best-effort ADR-0012), el marcador prueba que el competidor jugó.
  Las partidas anteriores al slice de 2026-07-15 no tienen participaciones proyectadas (sin
  backfill) y conservan el comportamiento previo.

## `GET /puntuaciones/equipos/{equipoId}/rendimiento`

Rendimiento histórico de un equipo (RF-44/HU-49): por cada partida por equipos **terminada**
donde el equipo tiene participación proyectada (inscripción aceptada), anotara o no, su posición
en el ranking consolidado y si la ganó
(`gano` = posición 1; si comparten la posición 1, ambos ganaron). Ordenado por `fechaFin`
descendente. Reusa el mismo cálculo del consolidado (RF-44: sin duplicar el cálculo).

- `200`:

```json
{
  "equipoId": "guid",
  "partidas": [
    { "partidaId": "guid", "fechaFin": "datetime", "posicion": 1, "gano": true }
  ]
}
```

- Equipo sin participaciones (o desconocido) → `200` con `partidas: []`.

## `GET /puntuaciones/partidas/{partidaId}/historial`

Relato cronológico de todos los eventos de la partida (HU-43): los **17** tipos del contrato de
Operaciones de Sesión, incluyendo respuestas, pistas y ubicaciones de todos los participantes.
Proyectado por el consumidor dedicado de historial (ver `contracts/events/operaciones-sesion-events.md`
§ Transport). Query params: `limit` (default 100, máx. 500), `offset` (default 0), `tipo`
(opcional, filtra por `TipoEvento` exacto).

- `200`:

```json
{
  "partidaId": "guid",
  "total": 245,
  "entradas": [
    {
      "occurredAt": "datetime (UTC)",
      "tipoEvento": "EtapaBDTGanada",
      "juegoId": "guid | null",
      "participanteId": "guid | null",
      "equipoId": "guid | null",
      "detalle": {},
      "juegoOrden": "int | null",
      "tipoJuego": "Trivia | BusquedaDelTesoro | null"
    }
  ]
}
```

- Orden fijo `occurredAt ASC`. `total` = conteo con el filtro `tipo` aplicado (para paginar).
- `juegoOrden` y `tipoJuego` acompañan a `juegoId`, unidos desde la proyección `JuegoProyectado`.
  Son `null` en eventos de partida (`PartidaIniciada`, `PartidaFinalizada`), que no tienen juego, y
  también cuando el `juegoId` existe pero su proyección falta (lag / evento perdido) — el cliente
  distingue ambos casos: `—` para el primero, GUID corto para el segundo. **`Juego` no tiene nombre
  en el dominio**: la etiqueta legible ("Juego 1 · Trivia") la compone el cliente.
- `400` `{ "message": "..." }`: `limit` fuera de `[1, 500]` u `offset` negativo.
- `404` `{ "message": "..." }`: la partida no existe en `partidas_proyectadas`. Partida conocida sin
  eventos → `200` con `entradas: []` (el historial no depende de la proyección para escribirse).
- `401` sin token; `403` con rol `Participante` (autorización **solo `Operador`/`Administrador`** —
  primer endpoint de Puntuaciones con chequeo de rol propio).
- **Notas:** `UbicacionActualizada` llega muestreada (máx. 1 por participante por minuto,
  descartada al escribir); las invitaciones de equipo no aparecen (Identity no publica al broker —
  limitación documentada, HU-43).

## `GET /puntuaciones/participantes/{participanteId}/historial-partidas`

Historial único de partidas jugadas por un participante, terminadas, con su puntuación y posición
(HU-27, RF-24). Reusa el mismo `CalculadorRankingConsolidado` de SP-4b (RF-44: sin duplicar el
cálculo).

- `200`:

```json
{
  "participanteId": "guid",
  "partidas": [
    {
      "partidaId": "guid",
      "modalidad": "Individual | Equipo",
      "fechaFin": "datetime",
      "equipoId": "guid | null",
      "puntosTotales": 45,
      "posicion": 1,
      "gano": true,
      "juegos": [
        { "juegoId": "guid", "orden": 1, "tipoJuego": "Trivia", "puntos": 20 }
      ]
    }
  ]
}
```

- Orden `fechaFin DESC`. Participante sin partidas (o id desconocido) → `200` con `partidas: []`
  (paridad con rendimiento de equipo, SP-4b).
- `401` sin token; cualquier rol autenticado (sin permiso funcional específico).
- **Participación:** inscripción aceptada propia (`Individual`) o **convocatoria aceptada** a un
  equipo con participación en la partida (`Equipo`), proyectadas desde `InscripcionAceptada` y
  `ConvocatoriaCreada`/`ConvocatoriaRespondida`. No se exige haber anotado ni haber autorado ninguna
  acción de juego: la limitación previa del integrante pasivo queda retirada. En el caso `Equipo` la
  puntuación/posición mostrada es la del equipo.
- **Limitaciones documentadas:** las partidas `Canceladas` no aparecen (RB-30 — sus resultados
  parciales no cuentan como resultado final).

## SignalR — ranking en vivo (SP-4c)

Hub: `puntuaciones/hubs/ranking` (vía gateway, ruta `/puntuaciones/*`; el token JWT viaja en el
query string `access_token` durante la negociación WebSocket — el gateway no reescribe paths).

Métodos del cliente:

| Método | Parámetros | Comportamiento |
|---|---|---|
| `SuscribirAPartida` | `partidaId: guid` | Une la conexión al grupo de la partida. `HubException("Partida no proyectada.")` si la partida no existe en las proyecciones (el cliente reintenta al recibir `PartidaEnLobby` de Operaciones de Sesión). |
| `DesuscribirDePartida` | `partidaId: guid` | Remueve la conexión del grupo. |

Mensajes servidor→cliente (payloads = shapes HTTP ya documentados en este contrato; enums como
string, camelCase):

| Mensaje | Disparador (evento proyectado) | Payload |
|---|---|---|
| `RankingTriviaActualizado` | `PuntajeTriviaIncrementado` | El shape de `GET .../juegos/{juegoId}/ranking` |
| `RankingBDTActualizado` | `EtapaBDTGanada` | El shape de `GET .../juegos/{juegoId}/ranking` |
| `RankingConsolidadoCalculado` | `PartidaFinalizada` | El shape de `GET .../ranking-consolidado` |

La difusión es best-effort (ADR-0012): un push perdido no se reintenta; los GET HTTP son la fuente
recuperable. Scoring tardío tras `PartidaFinalizada` re-difunde el ranking nativo del juego, no el
consolidado (la relectura HTTP lo incorpora).

## Autorización

La autenticación se exige en el gateway (policy `Default` sobre `/puntuaciones/*`) **y** en el
servicio (defensa en profundidad): `[Authorize]` a nivel de clase en `RankingsController`,
`EquiposController` y `ParticipantesController`, y en el hub `RankingHub`. `/health` es anónimo
(health check). Lectura para cualquier rol autenticado — el ranking y el historial de partidas
jugadas son visibles para operador y participantes; sin permiso funcional específico. **Excepción
(SP-4d):** `HistorialController` restringe con `[Authorize(Roles = "Operador,Administrador")]` —
el historial de partida expone respuestas, pistas y ubicaciones de todos los participantes, por lo
que un `Participante` autenticado recibe `403`.
