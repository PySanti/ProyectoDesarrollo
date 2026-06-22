# HU-30 — Ver ranking durante Trivia

| Campo | Valor |
|---|---|
| HU ID | HU-30 |
| Nombre | Ver ranking durante Trivia |
| Actor | Operador |
| Cliente objetivo | React web |
| Owning service | Trivia Game Service |
| Supporting services | Identity Service (JWT) |

## Referencias

- `docs/01-project-source/srs.md` — HU-30, RF-22, RB-T30, RB-T31, RB-T35
- `docs/01-project-source/historias-de-usuario.md` — HU-30
- `docs/01-project-source/diagrama-de-clases.md` — RankingTrivia VO, CompetidorTrivia
- `docs/01-project-source/modelo-de-dominio.md` — CalculadorRankingTriviaService, RankingTriviaActualizado

## Alcance

### Incluido

- Endpoint `GET /api/trivia-games/{id}/ranking` para consultar el ranking en cualquier momento
- Hub SignalR `/hubs/trivia-ranking` que emite `RankingUpdated` cuando se responde una pregunta
- Ranking ordenado por puntaje acumulado descendente
- Desempate por menor tiempo acumulado de respuesta
- Tests de dominio, handler y API

### Excluido

- Frontend web o móvil
- Otras HU (cancelación, lobby, etc.)

## Reglas de negocio

- RB-T30: El ranking debe actualizarse en tiempo real
- RB-T31: El operador visualiza ranking y opción de cancelar
- RB-T35: Empate se desempata por menor tiempo acumulado de respuesta
- RB-T28: Puntaje solo se otorga si la respuesta es correcta
- RB-T36: En Trivia por equipos, el puntaje se asigna al equipo

## API / Events

### HTTP

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| GET | `/api/trivia-games/{id}/ranking` | Operador / Participante | Ranking actual de la partida |

### Eventos SignalR

| Evento | Payload | Trigger |
|---|---|---|
| `RankingUpdated` | `{ partidaId: guid }` | Después de registrar una respuesta de Trivia |

## Criterios de aceptación

| ID | Criterio |
|---|---|
| CA-01 | Ranking retorna lista ordenada por puntaje descendente |
| CA-02 | Empate se desempata por menor tiempo acumulado de respuesta |
| CA-03 | Participante sin respuestas aparece con 0 puntos al final |
| CA-04 | Endpoint retorna 404 si partida no existe |
| CA-05 | Endpoint retorna 401 si no autenticado |
| CA-06 | Hub SignalR emite `RankingUpdated` tras cada respuesta |
