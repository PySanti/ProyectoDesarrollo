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
- Juego conocido sin marcadores → `200` con `entradas: []`.
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

- `404` `{ "message": "..." }`: juego desconocido, o el competidor no tiene marcador en el juego.

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
- Partida terminada sin marcadores → `200` con `entradas: []`.
- **Participación = tener ≥1 marcador**: competidores que nunca anotaron no aparecen (no hay evento de inscripción en el broker; best-effort ADR-0012).

## `GET /puntuaciones/equipos/{equipoId}/rendimiento`

Rendimiento histórico de un equipo (RF-44/HU-49): por cada partida por equipos **terminada**
donde el equipo tiene ≥1 marcador, su posición en el ranking consolidado y si la ganó
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
      "detalle": {}
    }
  ]
}
```

- Orden fijo `occurredAt ASC`. `total` = conteo con el filtro `tipo` aplicado (para paginar).
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
- **Participación:** marcador propio (`Individual`) o membresía resuelta del historial (cualquier
  `EventoHistorial` de la partida con `participanteId` + `equipoId` propios, excluyendo
  `ConvocatoriaCreada`) con **≥1 marcador del equipo** en la partida (sin marcador no hay posición
  calculable, misma regla de SP-4b). En ese caso la puntuación/posición mostrada es la del equipo.
- **Limitaciones documentadas:** un integrante de equipo que jamás autoró una acción de juego en la
  partida no la ve listada; las partidas `Canceladas` no aparecen (RB-30 — sus resultados parciales
  no cuentan como resultado final).

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
