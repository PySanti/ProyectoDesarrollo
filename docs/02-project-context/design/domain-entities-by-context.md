# Domain Entities by Context

## Identity Service

Owns:

- Usuario
- Administrador
- Operador
- Participante
- Rol
- Permiso
- UsuarioRol

Does not own:

- Teams
- Sessions
- Scores
- Audit history

## Team Service

Owns:

- Equipo
- EquipoParticipante

Does not own:

- Trivia content
- Treasure Hunt missions
- Scores
- Audit history

## Trivia Service

Owns:

- Quiz
- Pregunta
- OpcionRespuesta
- PreguntaSesion
- RespuestaTrivia
- ResultadoPreguntaEquipo
- SesionTrivia

Does not own:

- Team master data
- Global ranking persistence
- Audit history
- Treasure Hunt missions

## Treasure Hunt Service

Owns:

- Mision
- Etapa
- Nodo
- Objetivo
- Pista
- PistaLiberada
- Evidencia
- ValidacionEvidencia
- ProgresoMisionEquipo
- SesionBusquedaTesoro

Does not own:

- Trivia content
- Global ranking persistence
- User roles
- Audit history

## Scoring Service

Owns:

- PuntajeEquipoSesion
- MovimientoPuntaje
- RankingSesion
- PosicionRanking
- Penalizacion

Does not own:

- Evidence validation
- Trivia answer correctness
- Team creation
- Session state transitions

## Audit Service

Owns:

- EventoSesion
- SessionEventLog
- SystemAuditLog

Does not own:

- Business decisions
- Score calculation
- State transition validation