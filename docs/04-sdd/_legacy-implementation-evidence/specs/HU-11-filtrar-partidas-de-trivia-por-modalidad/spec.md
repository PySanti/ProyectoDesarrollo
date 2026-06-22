# HU-11 — Filtrar partidas de Trivia por modalidad

## Identificación

| Campo | Valor |
| --- | --- |
| HU ID | HU-11 |
| Nombre | Filtrar partidas de Trivia por modalidad |
| Actor | Participante |
| Cliente objetivo | React Native mobile |
| Servicio dueño | Trivia Game Service |
| Estado SDD | Nueva |

## Referencias fuente

- `docs/01-project-source/historias-de-usuario.md` — HU-11
- `docs/01-project-source/srs.md` — RF-05
- `docs/02-project-context/first-delivery-scope.md` — HU-11 activa
- `docs/03-microservices/service-ownership.md` — Trivia Game Service

## Historia de usuario

Como **Participante**, quiero **filtrar las partidas de trivia publicadas por modalidad (individual o equipo)**, para **encontrar rápidamente las partidas que me interesan**.

## Alcance

### Incluido

1. Agregar query parameter opcional `?modalidad=Individual|Equipo` al endpoint `GET /api/trivia-games`.
2. Validar que el valor sea exactamente `Individual` o `Equipo` (case-insensitive).
3. Si no se provee filtro, retorna todas las partidas publicadas (backward compatible con HU-09).

### Fuera de alcance

| Elemento | Motivo |
| --- | --- |
| UI de filtros en móvil | Frontend React Native congelado |
| Filtros por BDT | HU-12 |
| Filtros por otros campos (fecha, nombre, etc.) | No está en el alcance |

## Precondiciones

1. Usuario autenticado.
2. Trivia Game Service disponible.

## Postcondiciones

1. El sistema retorna lista de partidas filtradas (o todas si no hay filtro).
2. No hay mutación de estado.

## Reglas de negocio aplicables

| ID | Regla |
| --- | --- |
| RF-05 | El sistema debe mostrar a todos los participantes las partidas publicadas, y cada panel debe permitir filtrar por modalidad individual o equipo. |
| HU-11 | Filtrar trivias del listado entre individuales y de equipo. |

## Criterios de aceptación

### CA-01 — Filtro Individual
**Dado** un participante autenticado y partidas de ambas modalidades
**Cuando** filtra por `modalidad=Individual`
**Entonces** recibe solo las partidas con modalidad Individual.

### CA-02 — Filtro Equipo
**Dado** un participante autenticado y partidas de ambas modalidades
**Cuando** filtra por `modalidad=Equipo`
**Entonces** recibe solo las partidas con modalidad Equipo.

### CA-03 — Sin filtro
**Dado** un participante autenticado
**Cuando** no provee filtro de modalidad
**Entonces** recibe todas las partidas publicadas.

### CA-04 — Filtro sin resultados
**Dado** un participante autenticado y sin partidas de la modalidad solicitada
**Cuando** filtra por esa modalidad
**Entonces** recibe una lista vacía.

### CA-05 — Modalidad inválida
**Dado** un participante autenticado
**Cuando** filtra por un valor inválido
**Entonces** recibe 400 Bad Request.

## Supuestos explícitos

1. El filtro se aplica en el handler (capa Application), no en el repositorio.
2. El valor del query parameter es case-insensitive.
3. HU-11 modifica el mismo endpoint creado en HU-09.

## Preguntas abiertas

Ninguna.
