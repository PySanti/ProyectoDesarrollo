# Partidas — Configuration HTTP Contract (SP-2)

Service: **Partidas** (`umbral_partidas`). Base path: `/partidas` (through the YARP gateway in production). Configuration only: create a partida header, add fully-formed games. No publish/lobby/runtime (SP-3), no scoring (SP-4).

Enums are serialized as their string name. `estado` is `null` until the partida is published (SP-3).

## Autorización (SP-5a)

JWT Keycloak wireado (authority/audiences/issuers por env, mismo patrón que Operaciones de
Sesión) + normalizador `KeycloakRoleClaims`. `FallbackPolicy = RequireAuthenticatedUser`
(fail-secure): cualquier endpoint sin policy explícita exige solo un JWT válido.

| Endpoint | Auth | Notas |
|---|---|---|
| `POST /partidas` | Policy `GestionarPartidas` | 401 sin token · 403 sin el permiso |
| `POST /partidas/{partidaId}/juegos/trivia` | Policy `GestionarPartidas` | 401 sin token · 403 sin el permiso |
| `POST /partidas/{partidaId}/juegos/bdt` | Policy `GestionarPartidas` | 401 sin token · 403 sin el permiso |
| `GET /partidas/{partidaId}` | Policy `GestionarPartidas` | 401 sin token · 403 sin el permiso — el handoff de config interno (SP-3a §12) reenvía el bearer del **operador/administrador** que publicó la partida (`SesionesController.Publicar`, que ya exige `GestionarPartidas`), no el del participante |
| `GET /partidas` | Policy `GestionarPartidas` | 401 sin token · 403 sin el permiso |
| `GET /health` | Anónimo | sin auth |

## POST /partidas
Create a partida header (no games yet).

Request:
```json
{
  "nombrePartida": "Copa UMBRAL",
  "modalidad": "Individual",
  "modoInicioPartida": "Manual",
  "tiempoInicio": null,
  "minimosParticipacion": 1,
  "maximosParticipacion": 10
}
```
- `modalidad`: `Individual` | `Equipo`
- `modoInicioPartida`: `Manual` | `Automatico` | `ManualYAutomatico`
- `tiempoInicio`: required iff `modoInicioPartida` is `Automatico` or `ManualYAutomatico`; must be null for `Manual`.
- `maximosParticipacion >= minimosParticipacion >= 1`.

Responses:
- `201 Created` → `{ "partidaId": "<guid>" }`, `Location: /partidas/{partidaId}`
- `400 Bad Request` → `{ "message": "..." }` or `ValidationProblemDetails` on invalid input.

## POST /partidas/{partidaId}/juegos/trivia
Add one Trivia game with its full question set.

Request:
```json
{
  "orden": 1,
  "preguntas": [
    {
      "texto": "Capital de Francia?",
      "opciones": [ { "texto": "Paris", "esCorrecta": true }, { "texto": "Londres", "esCorrecta": false } ],
      "puntaje": 10,
      "tiempoLimiteSegundos": 30
    }
  ]
}
```
- At least one question; each question: ≥2 options, exactly one `esCorrecta: true`, `puntaje > 0`, `tiempoLimiteSegundos > 0`.

Responses:
- `201 Created` → `{ "juegoId": "<guid>" }`, `Location: /partidas/{partidaId}`
- `400` invalid content · `404` partida not found · `409` duplicate `orden`/game in the partida.

## POST /partidas/{partidaId}/juegos/bdt
Add one Búsqueda del Tesoro game with its full stage set.

Request:
```json
{
  "orden": 2,
  "areaBusqueda": "Plaza central",
  "etapas": [
    { "orden": 1, "codigoQREsperado": "QR-TEXT", "puntaje": 50, "tiempoLimiteSegundos": 120 }
  ]
}
```
- Non-empty `areaBusqueda`; at least one stage with contiguous `orden` from 1; each stage: non-empty `codigoQREsperado`, `puntaje > 0`, `tiempoLimiteSegundos > 0`.

Responses:
- `201 Created` → `{ "juegoId": "<guid>" }` · `400` · `404` · `409`.

## GET /partidas/{partidaId}
Review a partida and its configured games (ordered).

Response `200 OK`:
```json
{
  "partidaId": "<guid>",
  "nombrePartida": "Copa UMBRAL",
  "modalidad": "Individual",
  "modoInicioPartida": "Manual",
  "tiempoInicio": null,
  "minimosParticipacion": 1,
  "maximosParticipacion": 10,
  "estado": null,
  "juegos": [
    { "juegoId": "<guid>", "orden": 1, "tipoJuego": "Trivia", "estado": "Pendiente",
      "trivia": { "preguntas": [ { "preguntaId": "<guid>", "texto": "...", "puntajeAsignado": 10, "tiempoLimiteSegundos": 30, "opciones": [ { "opcionId": "<guid>", "texto": "...", "esCorrecta": true } ] } ] },
      "bdt": null },
    { "juegoId": "<guid>", "orden": 2, "tipoJuego": "BusquedaDelTesoro", "estado": "Pendiente",
      "trivia": null,
      "bdt": { "areaBusqueda": "Plaza central", "etapas": [ { "etapaBDTId": "<guid>", "orden": 1, "codigoQREsperado": "QR-TEXT", "puntajeAsignado": 50, "tiempoLimiteSegundos": 120 } ] } }
  ]
}
```
- `404 Not Found` when the partida does not exist.

## GET /partidas
List partida summaries.

Response `200 OK`:
```json
[ { "partidaId": "<guid>", "nombrePartida": "Copa UMBRAL", "modalidad": "Individual", "modoInicioPartida": "Manual", "tiempoInicio": null, "minimosParticipacion": 1, "maximosParticipacion": 10, "estado": null, "cantidadJuegos": 2 } ]
```
