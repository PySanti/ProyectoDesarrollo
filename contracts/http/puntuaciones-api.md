# Puntuaciones HTTP Contract

## Status

Endpoints registrados (4): ranking nativo por juego + marcador propio (SP-4a) y ranking
consolidado de partida + rendimiento de equipo (SP-4b), servidos por las proyecciones
alimentadas por RabbitMQ (best-effort, ADR-0012). SignalR de ranking (SP-4c) y
auditoría/historial (SP-4d) pendientes.

## Access Path

Requests enter through the YARP gateway (`/puntuaciones/*` → servicio Puntuaciones, reenvío puro).

## Endpoint Registry

| Capability | Method | Gateway path | Owning service | Status |
|---|---|---|---|---|
| Ranking nativo de un juego | GET | `/puntuaciones/partidas/{partidaId}/juegos/{juegoId}/ranking` | Puntuaciones | Registered (SP-4a) |
| Marcador propio en un juego | GET | `/puntuaciones/partidas/{partidaId}/juegos/{juegoId}/marcadores/{competidorId}` | Puntuaciones | Registered (SP-4a) |
| Ranking consolidado de la partida | GET | `/puntuaciones/partidas/{partidaId}/ranking-consolidado` | Puntuaciones | Registered (SP-4b) |
| Rendimiento histórico de un equipo | GET | `/puntuaciones/equipos/{equipoId}/rendimiento` | Puntuaciones | Registered (SP-4b) |

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

## Autorización

La autenticación se exige en el gateway (policy `Default` sobre `/puntuaciones/*`). El servicio
valida el JWT cuando se presenta (configuración Keycloak condicional), pero estos endpoints de
lectura no llevan `[Authorize]` (paridad con los servicios hermanos; hardening del servicio →
SP-4c). Lectura para cualquier rol — el ranking es visible para operador y participantes; sin
permiso funcional específico.
