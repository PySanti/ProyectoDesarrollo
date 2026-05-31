# HU-09 — Ver partidas de Trivia publicadas

## Identificación

| Campo | Valor |
| --- | --- |
| HU ID | HU-09 |
| Nombre | Ver partidas de Trivia publicadas |
| Actor | Participante |
| Cliente objetivo | React Native mobile |
| Servicio dueño | Trivia Game Service |
| Estado SDD | Nueva |

## Referencias fuente

- `docs/01-project-source/srs.md` — HU-09, RF-05
- `docs/01-project-source/historias-de-usuario.md` — HU-09
- `docs/02-project-context/first-delivery-scope.md` — HU-09 activa
- `docs/03-microservices/service-ownership.md` — Trivia Game Service
- `docs/03-microservices/services/trivia-game-service.md`

## Historia de usuario

Como **Participante**, quiero **ver las partidas de trivia publicadas**, para **elegir a cuál unirme**.

## Objetivo de negocio

Permitir al participante visualizar un listado de partidas de Trivia que están en estado `Lobby` (publicadas), con información básica para decidir si inscribirse.

## Alcance

### Incluido

1. Endpoint `GET /api/trivia-games` que retorna lista de partidas en estado Lobby.
2. Accesible por rol **Participante** y **Operador**.
3. Retorna campos básicos: Id, Nombre, Modalidad, Estado, TiempoInicio, límites de participación.
4. Persistencia desde repositorio EF Core.

### Fuera de alcance

| Elemento | Motivo |
| --- | --- |
| Filtros por modalidad | HU-11 |
| Pantalla móvil "panel Trivia" | Frontend React Native congelado |
| Partidas de BDT | HU-10 |
| Inscripción o unión a partidas | HU-18, HU-19 |

## Precondiciones

1. Usuario autenticado vía Keycloak con rol **Participante** u **Operador**.
2. Trivia Game Service disponible.

## Postcondiciones

1. El sistema retorna lista de partidas en estado Lobby.
2. No hay mutación de estado.

## Reglas de negocio aplicables

| ID | Regla |
| --- | --- |
| RF-05 | El sistema debe mostrar a todos los participantes las partidas publicadas. |

## Criterios de aceptación

### CA-01 — Lista partidas publicadas

**Dado** un participante autenticado  
**Cuando** solicita el listado de partidas de Trivia  
**Entonces** recibe todas las partidas en estado `Lobby`.

### CA-02 — Lista vacía

**Dado** un participante autenticado y sin partidas publicadas  
**Cuando** solicita el listado  
**Entonces** recibe una lista vacía.

### CA-03 — Acceso no autorizado

**Dado** un usuario no autenticado  
**Cuando** solicita el listado  
**Entonces** recibe 401.

## Supuestos explícitos

1. Solo se listan partidas en `PartidaEstado.Lobby`.
2. El listado es de solo lectura (Query).

## Preguntas abiertas

Ninguna.
