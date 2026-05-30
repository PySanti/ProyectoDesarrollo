# Service Ownership

## Identity Service

Owns:

- Usuario
- KeycloakId
- RolUsuario
- EstadoUsuario
- local user references

Does not own:

- teams
- trivia sessions
- BDT sessions
- game scoring
- game history

## Team Service

Owns:

- Equipo
- Equipos.Participante
- CodigoAcceso
- liderazgo
- estado del equipo
- reglas de pertenencia a equipo

Does not own:

- formularios de Trivia
- partidas de Trivia
- partidas BDT
- validación de respuestas
- validación de QR
- ranking de partidas
- historial de partidas

## Trivia Game Service

Owns:

- FormularioTrivia
- Pregunta
- Opcion
- PuntajeAsignado
- TiempoLimite
- PartidaTrivia
- Trivias.Participante
- RespuestaTrivia
- inscripciones de Trivia
- convocatorias de Trivia
- puntaje de Trivia
- ranking de Trivia
- historial de Trivia
- eventos de Trivia
- actualizaciones en tiempo real de Trivia

Does not own:

- usuarios
- equipos como dato maestro
- partidas BDT
- QR BDT
- pistas BDT
- geolocalización BDT

## BDT Game Service

Owns:

- PartidaBDT
- EtapaBDT
- TesoroQR
- Pista
- AreaBusqueda
- UbicacionGeografica
- CodigoQREsperado
- PuntajeEtapa
- inscripciones BDT
- convocatorias BDT
- validación QR
- puntaje BDT
- ranking BDT
- historial BDT
- eventos BDT
- actualizaciones en tiempo real BDT

Does not own:

- usuarios
- equipos como dato maestro
- formularios de Trivia
- preguntas Trivia
- respuestas Trivia