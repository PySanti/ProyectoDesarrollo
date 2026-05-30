# Team Service

> **Regla de generación:** este contenido fue generado exclusivamente a partir de los archivos `diagrama de clases(2).md`, `enunciado-proyecto(1).md`, `historias de usuario(2).md`, `microservicios(2).md`, `modelo de dominio(2).md` y `srs(2).md`.
>
> **No se agregan microservicios, endpoints, colas, eventos, bases de datos, rutas HTTP ni contratos que no estén indicados explícitamente en esas fuentes.** Cuando una responsabilidad aparece en el SRS/modelo pero no está asignada a un microservicio en `microservicios(2).md`, queda marcada como **no asignada / pendiente de decisión**.


## Identificación

| Campo | Valor |
|---|---|
| Nombre | Team Service |
| Nombre en fuente | Microservicio de Gestión de Equipos |
| Contexto DDD | Team Context |
| Tipo de subdominio | Soporte |
| Historias asignadas | HU-03, HU-04, HU-05, HU-06, HU-07 |
| Persistencia indicada | Equipos y Miembros |

## Responsabilidad explícita

El Team Service es responsable de:

- creación de grupos;
- validación de códigos de invitación;
- límite de miembros;
- cambios de roles internos;
- liderazgo/invitado dentro del equipo.

El documento fuente indica explícitamente que este servicio:

```txt
No sabe nada de juegos ni de puntajes.
```

## Reglas de negocio relacionadas

| Regla | Contenido |
|---|---|
| RB-E01 | Los equipos son globales para toda la aplicación y se usan tanto en Trivia como en BDT. |
| RB-E02 | Todo jugador puede crear un equipo si no pertenece a otro. |
| RB-E03 | Todo jugador puede unirse a un equipo mediante código si no pertenece a otro. |
| RB-E04 | Cuando se crea un equipo, el sistema genera un código único de ingreso. |
| RB-E05 | El jugador que crea el equipo queda registrado automáticamente como líder. |
| RB-E06 | Un jugador solo puede pertenecer a un equipo a la vez. |
| RB-E07 | Un equipo puede tener máximo 5 jugadores. |
| RB-E08 | Los jugadores pueden salir de su equipo. |
| RB-E09 | Si un jugador no líder sale del equipo, simplemente deja de pertenecer al equipo. |
| RB-E10 | Si el líder desea salir y existen otros integrantes, debe transferir el liderazgo a otro jugador antes de salir. |
| RB-E11 | Si el líder desea salir y no existen otros integrantes, el equipo se elimina. |
| RB-E12 | El administrador puede crear, consultar, editar y desactivar equipos. |
| RB-E13 | Un equipo desactivado no puede inscribirse en nuevas partidas. |
| RB-E14 | El líder es el único autorizado para inscribir al equipo en partidas de equipo. |

## Modelo de dominio asociado

| Elemento | Tipo |
|---|---|
| `Equipo` | Agregado raíz |
| `Equipos.Participante` | Entidad hija |
| `EquipoId` | Value Object |
| `NombreEquipo` | Value Object |
| `CodigoAcceso` | Value Object |
| `EstadoEquipo` | Enum |

## Historias asignadas por `microservicios(2).md`

| HU | Descripción |
|---|---|
| HU-03 | Crear equipo. |
| HU-04 | Unirse a equipo usando código. |
| HU-05 | Eliminar equipo creado. |
| HU-06 | Transferir liderazgo antes de salir del equipo. |
| HU-07 | Salir del equipo. |

## No responsabilidades

Team Service no debe asumir ownership de:

- formularios de Trivia;
- partidas de Trivia;
- partidas BDT;
- validación de respuestas Trivia;
- validación de QR BDT;
- puntajes de partidas;
- ranking;
- historial de eventos de partida, salvo que una decisión posterior lo especifique.

## Dependencias conceptuales

| Dependencia | Motivo | Estado técnico |
|---|---|---|
| Identity / Keycloak | Los miembros/líderes son usuarios autenticados. | Mecanismo no especificado. |
| Trivia Game Service | Trivia necesita saber si un participante es líder para partidas por equipo. | Contrato no especificado. |
| BDT Game Service | BDT necesita saber si un participante es líder para partidas por equipo. | Contrato no especificado. |

## Pendientes antes de implementar

- Resolver si HU-08, que existe en SRS, pertenece también a Team Service. No aparece asignada en `microservicios(2).md`.
- Definir contrato para consultar liderazgo/equipo desde Trivia y BDT si esas HUs lo requieren.
- Definir contrato de notificación a integrantes en HU-05 si se implementa como parte de primera entrega.
