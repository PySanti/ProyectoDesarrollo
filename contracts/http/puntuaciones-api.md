# Puntuaciones HTTP Contract

## Status

Endpoints SP-4a registrados (2): ranking nativo por juego y marcador propio, servidos por las
proyecciones alimentadas por RabbitMQ (best-effort, ADR-0012). Consolidado/team-performance (SP-4b),
SignalR de ranking (SP-4c) y auditoría/historial (SP-4d) pendientes.

## Access Path

Requests enter through the YARP gateway (`/puntuaciones/*` → servicio Puntuaciones, reenvío puro).

## Endpoint Registry

| Capability | Method | Gateway path | Owning service | Status |
|---|---|---|---|---|
| Ranking nativo de un juego | GET | `/puntuaciones/partidas/{partidaId}/juegos/{juegoId}/ranking` | Puntuaciones | Registered (SP-4a) |
| Marcador propio en un juego | GET | `/puntuaciones/partidas/{partidaId}/juegos/{juegoId}/marcadores/{competidorId}` | Puntuaciones | Registered (SP-4a) |

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

## Autorización

La autenticación se exige en el gateway (policy `Default` sobre `/puntuaciones/*`). El servicio
valida el JWT cuando se presenta (configuración Keycloak condicional), pero estos endpoints de
lectura no llevan `[Authorize]` (paridad con los servicios hermanos; hardening del servicio →
SP-4c). Lectura para cualquier rol — el ranking es visible para operador y participantes; sin
permiso funcional específico.
