# HU-17 — Acceptance: Crear y publicar partida de Trivia

## Acceptance checklist

| CA | Descripción | Estado |
|----|-------------|--------|
| CA-01 | Crear partida individual exitosamente — 201 Created en Lobby | Hecho |
| CA-02 | Crear partida por equipos exitosamente — 201 Created en Lobby | Hecho |
| CA-03 | Rechazar si formulario no existe — 404 | Hecho |
| CA-04 | Rechazar si formulario incompleto — 400 | Hecho |
| CA-05 | Rechazar si modalidad inválida — 400 | Hecho |
| CA-06 | Rechazar si campos requeridos faltan — 400 | Hecho |
| CA-07 | Rechazar si no es Operador — 403 | Hecho |

## Business rules verified

| RB | Descripción | Verificado |
|----|-------------|------------|
| RB-T04 | Solo operador puede crear partidas | Hecho |
| RB-T05 | Partida debe tener formulario válido | Hecho |
| RB-T06 | Definir nombre, modalidad, formulario, mínimos, máximos y tiempo de inicio | Hecho |
| RB-T07 | Si individual, máximo = jugadores | Hecho |
| RB-T08 | Si equipo, máximo = equipos | Hecho |
| RB-T09 | Si equipo, definir mínimo/máximo de jugadores por equipo | Hecho |

## Test results

Las pruebas del handler y API se ejecutan como parte de la suite completa del backend de trivia. La rama `feature/trivia-backend-completo` reporta 325/326 tests exitosos.
